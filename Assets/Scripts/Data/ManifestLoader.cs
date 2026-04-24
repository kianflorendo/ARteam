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
        string json = null;
        string source = "";

        // Path 1: persistentDataPath — downloaded via LFS update.
        // Regular filesystem path — synchronous read is fine.
        string lfsPath = Path.Combine(Application.persistentDataPath, MANIFEST_FILENAME);
        if (File.Exists(lfsPath))
        {
            json = File.ReadAllText(lfsPath);
            source = "LFS (persistentDataPath)";
        }
        else
        {
            // Path 2: StreamingAssets — bundled inside APK.
            // On Android, Application.streamingAssetsPath returns the jar:file:// URI
            // that UnityWebRequest understands. Direct File.ReadAllText does NOT
            // work inside an APK — this was the original bug that broke all Android builds.
#if UNITY_EDITOR
            string streamingUri = "file://" + Path.Combine(
                Application.streamingAssetsPath, MANIFEST_FILENAME).Replace("\\", "/");
#else
            string streamingUri = Path.Combine(
                Application.streamingAssetsPath, MANIFEST_FILENAME);
#endif
            using (var request = UnityWebRequest.Get(streamingUri))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    json = request.downloadHandler.text;
                    source = "StreamingAssets";
                }
                else
                {
                    Debug.LogError($"[ManifestLoader] Failed to read StreamingAssets: " +
                                   $"{request.error}. Make sure manifest.json is in " +
                                   "Assets/StreamingAssets/.");
                }
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ManifestLoader] manifest.json not found in either path. " +
                           "Make sure Assets/StreamingAssets/manifest.json exists.");
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
    /// sorted by sequence_index ascending.
    public List<ArtifactData> GetGPSRouteArtifacts()
    {
        if (!_isLoaded) { LogNotLoaded(); return new List<ArtifactData>(); }

        return (_manifest.artifacts ?? new List<ArtifactData>())
            .Where(a =>
                a.anchor_mode == AnchorMode.GPS
                && a.sequence_index > 0
                && (string.IsNullOrEmpty(a.gps_progression_mode)
                    || a.gps_progression_mode == GPSProgressionMode.DistanceChain))
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
