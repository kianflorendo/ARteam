using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ManifestLoader : MonoBehaviour
{
    public static ManifestLoader Instance { get; private set; }
    public static event Action OnManifestLoaded;

    private ManifestData _manifest;
    private bool _isLoaded = false;

    private const string MANIFEST_FILENAME = "manifest.json";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(LoadManifestAsync());
    }

    private IEnumerator LoadManifestAsync()
    {
        // On Android, StreamingAssets lives inside the APK and must be read via
        // UnityWebRequest (direct File.ReadAllText doesn't work in jar:file:// paths).
#if UNITY_EDITOR
        string streamingUri = "file://" + Path.Combine(
            Application.streamingAssetsPath, MANIFEST_FILENAME).Replace("\\", "/");
#else
        string streamingUri = Path.Combine(Application.streamingAssetsPath, MANIFEST_FILENAME);
#endif
        string streamingJson = null;
        using (var req = UnityWebRequest.Get(streamingUri))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                streamingJson = req.downloadHandler.text;
            else
                Debug.LogError($"[ManifestLoader] Failed to read StreamingAssets: {req.error}");
        }

        // Use whichever has the HIGHER version number so a new APK build (with a bumped version)
        // always replaces a stale device-side cache, while still allowing live LFS updates to override the APK.
        string lfsPath = Path.Combine(Application.persistentDataPath, MANIFEST_FILENAME);
        string json   = streamingJson;
        string source = "StreamingAssets";

        if (File.Exists(lfsPath) && !string.IsNullOrEmpty(streamingJson))
        {
            try
            {
                string lfsJson     = File.ReadAllText(lfsPath);
                var    lfsMeta     = JsonUtility.FromJson<ManifestData>(lfsJson);
                var    streamMeta  = JsonUtility.FromJson<ManifestData>(streamingJson);
                string lfsVer      = lfsMeta?.version    ?? "0.0.0";
                string streamVer   = streamMeta?.version ?? "0.0.0";

                if (CompareVersions(lfsVer, streamVer) > 0)
                {
                    json   = lfsJson;
                    source = $"LFS/persistentDataPath (v{lfsVer} > bundled v{streamVer})";
                }
                else
                {
                    File.Delete(lfsPath);
                    source = $"StreamingAssets (v{streamVer} >= cached v{lfsVer}; cache cleared)";
                }
            }
            catch
            {
                File.Delete(lfsPath);
            }
        }
        else if (File.Exists(lfsPath))
        {
            json   = File.ReadAllText(lfsPath);
            source = "LFS/persistentDataPath (StreamingAssets unavailable)";
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ManifestLoader] manifest.json not found in any path.");
            yield break;
        }

        try
        {
            _manifest = JsonUtility.FromJson<ManifestData>(json);
            _isLoaded = true;
            Debug.Log($"[ManifestLoader] Loaded v{_manifest.version} from {source}. " +
                      $"Artifacts: {_manifest.artifacts?.Count ?? 0}, " +
                      $"Soldiers: {_manifest.soldiers?.Count ?? 0}, " +
                      $"Divisions: {_manifest.divisions?.Count ?? 0}");
            OnManifestLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ManifestLoader] Failed to parse manifest.json: {e.Message}");
        }
    }

    private static int CompareVersions(string a, string b)
    {
        int[] Pa = ParseVersion(a);
        int[] Pb = ParseVersion(b);
        for (int i = 0; i < 3; i++)
        {
            int diff = Pa[i] - Pb[i];
            if (diff != 0) return diff;
        }
        return 0;
    }

    private static int[] ParseVersion(string v)
    {
        int[] result = { 0, 0, 0 };
        if (string.IsNullOrEmpty(v)) return result;
        string[] parts = v.Split('.');
        for (int i = 0; i < Mathf.Min(parts.Length, 3); i++)
            int.TryParse(parts[i], out result[i]);
        return result;
    }

    public ArtifactData GetArtifact(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.artifacts?.Find(a => a.id == id);
    }

    public ArtifactData GetArtifactByMarker(string markerName)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.artifacts?.Find(a => a.marker_name == markerName);
    }

    public List<ArtifactData> GetGPSArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }
        return _manifest.artifacts?.FindAll(a => a.anchor_mode == AnchorMode.GPS)
               ?? new List<ArtifactData>();
    }

    public List<ArtifactData> GetGPSRouteArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }

        string activeSoldier = ActiveSoldierManager.Instance?.ActiveSoldierId ?? "S-001";

        return (_manifest.artifacts ?? new List<ArtifactData>())
            .Where(a =>
                a.anchor_mode == AnchorMode.GPS
                && a.sequence_index > 0
                && (string.IsNullOrEmpty(a.gps_progression_mode)
                    || a.gps_progression_mode == GPSProgressionMode.DistanceChain)
                && (string.IsNullOrEmpty(a.soldier_id)
                    || a.soldier_id == activeSoldier
                    || (a.shared_soldier_ids != null && a.shared_soldier_ids.Contains(activeSoldier))))
            .OrderBy(a => a.sequence_index)
            .ToList();
    }

    public List<ArtifactData> GetImageArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }
        return _manifest.artifacts?.FindAll(a => a.anchor_mode == AnchorMode.Image)
               ?? new List<ArtifactData>();
    }

    public SoldierData GetSoldier(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.soldiers?.Find(s => s.id == id);
    }

    public DivisionData GetDivision(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.divisions?.Find(d => d.id == id);
    }

    public List<SoldierData> GetAllSoldiers()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<SoldierData>(); }
        return _manifest.soldiers ?? new List<SoldierData>();
    }

    public List<DivisionData> GetAllDivisions()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<DivisionData>(); }
        return _manifest.divisions ?? new List<DivisionData>();
    }

    public string GetVersion()
    {
        return _isLoaded ? _manifest.version : "unknown";
    }

    public bool IsLoaded => _isLoaded;

    private void LogNotLoaded()
    {
        Debug.LogWarning("[ManifestLoader] Manifest not loaded yet. " +
                         "Make sure ManifestLoader runs before other scripts.");
    }
}
