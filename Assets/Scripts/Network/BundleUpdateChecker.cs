// ============================================================
// BundleUpdateChecker.cs
// Location: Assets/Scripts/Network/BundleUpdateChecker.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// On app launch with internet:
//   1. Fetch manifest.json from LFS
//   2. Compare version with local manifest version
//   3. If newer: download updated manifest silently
//   4. User is never blocked -- plays with existing content
//      while update runs in background
// ============================================================

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class BundleUpdateChecker : MonoBehaviour
{
    // -- Singleton --
    public static BundleUpdateChecker Instance { get; private set; }

    // -- Events --
    public static event Action<string> OnUpdateAvailable;
    public static event Action OnUpdateComplete;
    public static event Action<string> OnUpdateFailed;

    // -- LFS Configuration --
    [Header("LFS Configuration")]
    [Tooltip("Set to your GitHub LFS raw URL before deploying")]
    public string lfsBaseUrl = "https://media.githubusercontent.com/media/NoContextOrg/anino-assets/main/";

    private const string MANIFEST_LFS_PATH = "manifest/manifest.json";

    // -- State --
    private bool _isChecking = false;
    private const float CHECK_TIMEOUT_SECONDS = 10f;

    // -- File paths --
    private string LocalManifestPath =>
        Path.Combine(Application.persistentDataPath, "manifest.json");

    // ============================================================
    //  Unity lifecycle
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(CheckForUpdates());
    }

    // ============================================================
    //  Main update check flow
    // ============================================================

    public IEnumerator CheckForUpdates()
    {
        if (_isChecking) yield break;
        _isChecking = true;

        // Step 1 -- Check internet connectivity
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[BundleUpdateChecker] No internet. Using bundled assets.");
            _isChecking = false;
            yield break;
        }

        Debug.Log("[BundleUpdateChecker] Internet available. Checking for updates...");

        // Step 2 -- Fetch remote manifest.json from LFS
        string remoteManifestUrl = lfsBaseUrl + MANIFEST_LFS_PATH;
        string remoteJson = null;

        using (var request = UnityWebRequest.Get(remoteManifestUrl))
        {
            request.timeout = (int)CHECK_TIMEOUT_SECONDS;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[BundleUpdateChecker] Could not reach LFS: {request.error}. " +
                          "Using bundled assets.");
                _isChecking = false;
                yield break;
            }

            remoteJson = request.downloadHandler.text;
        }

        if (string.IsNullOrEmpty(remoteJson))
        {
            Debug.LogWarning("[BundleUpdateChecker] Remote manifest is empty.");
            _isChecking = false;
            yield break;
        }

        // Step 3 -- Parse remote version
        string remoteVersion = ParseVersion(remoteJson);
        if (string.IsNullOrEmpty(remoteVersion))
        {
            Debug.LogWarning("[BundleUpdateChecker] Could not parse remote manifest version.");
            _isChecking = false;
            yield break;
        }

        // Step 4 -- Compare with local version
        string localVersion = ManifestLoader.Instance != null
            ? ManifestLoader.Instance.GetVersion()
            : "0.0.0";

        Debug.Log($"[BundleUpdateChecker] Local: v{localVersion} | Remote: v{remoteVersion}");

        if (localVersion == remoteVersion)
        {
            Debug.Log("[BundleUpdateChecker] Already up to date. No download needed.");
            _isChecking = false;
            yield break;
        }

        // Step 5 -- Newer version found -- save updated manifest
        Debug.Log($"[BundleUpdateChecker] New content available: v{remoteVersion}. " +
                  "Downloading silently in background...");
        OnUpdateAvailable?.Invoke(remoteVersion);

        yield return SaveUpdatedManifest(remoteJson, remoteVersion);

        _isChecking = false;
    }

    // ============================================================
    //  Save updated manifest
    // ============================================================

    private IEnumerator SaveUpdatedManifest(string json, string version)
    {
        try
        {
            File.WriteAllText(LocalManifestPath, json);
            Debug.Log($"[BundleUpdateChecker] Manifest v{version} saved. " +
                      "Active on next app launch.");
            OnUpdateComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BundleUpdateChecker] Failed to save manifest: {e.Message}");
            OnUpdateFailed?.Invoke(e.Message);
        }

        yield return null;
    }

    // ============================================================
    //  Helpers
    // ============================================================

    private string ParseVersion(string json)
    {
        try
        {
            const string versionKey = "\"version\":";
            int keyIndex = json.IndexOf(versionKey, StringComparison.Ordinal);
            if (keyIndex < 0) return null;

            int start = json.IndexOf('"', keyIndex + versionKey.Length) + 1;
            int end = json.IndexOf('"', start);
            if (start < 0 || end < 0) return null;

            return json.Substring(start, end - start);
        }
        catch
        {
            return null;
        }
    }

    public bool IsChecking => _isChecking;
}