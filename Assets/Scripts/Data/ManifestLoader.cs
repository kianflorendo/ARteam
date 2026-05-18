// ============================================================
// ManifestLoader.cs
// Location: Assets/Scripts/Data/ManifestLoader.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Reads manifest.json on app start.
// Priority: persistentDataPath (LFS updated) over StreamingAssets (bundled).
// Caches all data in memory after first parse.
// All other scripts read data through this singleton.
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ManifestLoader : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static ManifestLoader Instance { get; private set; }

    // ── Events ───────────────────────────────────────────────
    public static event Action OnManifestLoaded;

    // ── Private state ────────────────────────────────────────
    private ManifestData _manifest;
    private bool _isLoaded = false;

    // ── File paths ───────────────────────────────────────────
    private const string MANIFEST_FILENAME = "manifest.json";

    // ─────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(LoadManifestAsync());
    }

    // ─────────────────────────────────────────────────────────
    //  Load — prefer LFS updated, fall back to bundled.
    //  Uses UnityWebRequest for StreamingAssets so it works
    //  correctly on Android (jar:file:// scheme inside APK).
    // ─────────────────────────────────────────────────────────

    private IEnumerator LoadManifestAsync()
    {
        // Always load the bundled StreamingAssets manifest so we can compare versions.
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

        // Check whether a cached (LFS-downloaded) manifest exists and compare versions.
        // Use whichever has the HIGHER version number.
        // This ensures a new APK build (with a bumped version) always replaces a stale
        // device-side cache, while still allowing live LFS updates to override the APK.
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
                    // LFS copy is strictly newer — use it (live update scenario).
                    json   = lfsJson;
                    source = $"LFS/persistentDataPath (v{lfsVer} > bundled v{streamVer})";
                }
                else
                {
                    // Bundled version is same or newer — use StreamingAssets.
                    // Delete the stale cached file so it doesn't win on the next launch.
                    File.Delete(lfsPath);
                    source = $"StreamingAssets (v{streamVer} >= cached v{lfsVer}; cache cleared)";
                }
            }
            catch
            {
                // Corrupt cache — fall back to bundled.
                File.Delete(lfsPath);
            }
        }
        else if (File.Exists(lfsPath))
        {
            // StreamingAssets failed to load — use cached as last resort.
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

    // Compares semantic version strings ("1.2.3"). Returns >0 if a > b, 0 if equal, <0 if a < b.
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

    // ─────────────────────────────────────────────────────────
    //  Public lookup methods
    // ─────────────────────────────────────────────────────────

    /// Returns the full ArtifactData for a given artifact id (e.g. "A-001")
    public ArtifactData GetArtifact(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.artifacts?.Find(a => a.id == id);
    }

    /// Returns the ArtifactData whose marker_name matches the given name.
    /// Used by ImageAnchorManager when ARTrackedImage is detected.
    public ArtifactData GetArtifactByMarker(string markerName)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.artifacts?.Find(a => a.marker_name == markerName);
    }

    /// Returns all artifacts with anchor_mode == "gps".
    /// Used by offline GPS route progression.
    public List<ArtifactData> GetGPSArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }
        return _manifest.artifacts?.FindAll(a => a.anchor_mode == AnchorMode.GPS)
               ?? new List<ArtifactData>();
    }

    /// Returns GPS artifacts that participate in the offline distance-chain route,
    /// filtered by the currently active soldier, sorted by sequence_index ascending.
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
                && (string.IsNullOrEmpty(a.soldier_id) || a.soldier_id == activeSoldier))
            .OrderBy(a => a.sequence_index)
            .ToList();
    }

    /// Returns all artifacts with anchor_mode == "image".
    public List<ArtifactData> GetImageArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }
        return _manifest.artifacts?.FindAll(a => a.anchor_mode == AnchorMode.Image)
               ?? new List<ArtifactData>();
    }

    /// Returns SoldierData for a given soldier id (e.g. "S-001")
    public SoldierData GetSoldier(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.soldiers?.Find(s => s.id == id);
    }

    /// Returns DivisionData for a given division id (e.g. "D-21")
    public DivisionData GetDivision(string id)
    {
        if (!_isLoaded) { LogNotLoaded(); return null; }
        return _manifest.divisions?.Find(d => d.id == id);
    }

    /// Returns all soldiers
    public List<SoldierData> GetAllSoldiers()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<SoldierData>(); }
        return _manifest.soldiers ?? new List<SoldierData>();
    }

    /// Returns all divisions
    public List<DivisionData> GetAllDivisions()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<DivisionData>(); }
        return _manifest.divisions ?? new List<DivisionData>();
    }

    /// Returns current manifest version string
    public string GetVersion()
    {
        return _isLoaded ? _manifest.version : "unknown";
    }

    /// Returns true if manifest has been loaded and parsed successfully
    public bool IsLoaded => _isLoaded;

    // ─────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────

    private void LogNotLoaded()
    {
        Debug.LogWarning("[ManifestLoader] Manifest not loaded yet. " +
                         "Make sure ManifestLoader runs before other scripts.");
    }
}
