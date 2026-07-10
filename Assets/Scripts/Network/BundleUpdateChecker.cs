using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class BundleUpdateChecker : MonoBehaviour
{
    public static BundleUpdateChecker Instance { get; private set; }

    public static event Action<string> OnUpdateAvailable;
    public static event Action OnUpdateComplete;
    public static event Action<string> OnUpdateFailed;

    [Header("LFS Configuration")]
    [Tooltip("Set to your GitHub LFS raw URL before deploying")]
    public string lfsBaseUrl = "https://media.githubusercontent.com/media/NoContextOrg/anino-assets/main/";

    private const string MANIFEST_LFS_PATH = "manifest/manifest.json";

    private bool _isChecking = false;
    private const float CHECK_TIMEOUT_SECONDS = 10f;

    private string LocalManifestPath =>
        Path.Combine(Application.persistentDataPath, "manifest.json");

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

    public IEnumerator CheckForUpdates()
    {
        if (_isChecking) yield break;
        _isChecking = true;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[BundleUpdateChecker] No internet. Using bundled assets.");
            _isChecking = false;
            yield break;
        }

        Debug.Log("[BundleUpdateChecker] Internet available. Checking for updates...");

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

        string remoteVersion = ParseVersion(remoteJson);
        if (string.IsNullOrEmpty(remoteVersion))
        {
            Debug.LogWarning("[BundleUpdateChecker] Could not parse remote manifest version.");
            _isChecking = false;
            yield break;
        }

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

        Debug.Log($"[BundleUpdateChecker] New content available: v{remoteVersion}. " +
                  "Downloading silently in background...");
        OnUpdateAvailable?.Invoke(remoteVersion);

        yield return SaveUpdatedManifest(remoteJson, remoteVersion);

        _isChecking = false;
    }

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
