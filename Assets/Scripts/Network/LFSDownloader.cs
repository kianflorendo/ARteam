// ============================================================
// LFSDownloader.cs
// Location: Assets/Scripts/Network/LFSDownloader.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Downloads updated asset bundles from Git LFS silently
// in the background. User is never blocked or interrupted.
// Downloaded bundles are stored in persistentDataPath/bundles/
// and will override the bundled APK versions on next launch.
//
// Called by BundleUpdateChecker when a newer manifest is found.
// ============================================================

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LFSDownloader : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static LFSDownloader Instance { get; private set; }

    // ── Events ───────────────────────────────────────────────
    public static event Action<float> OnDownloadProgress;   // 0.0 to 1.0
    public static event Action<string> OnBundleDownloaded;   // bundle key
    public static event Action OnAllDownloadsComplete;
    public static event Action<string> OnDownloadFailed;     // error message

    // ── Configuration ────────────────────────────────────────
    [Header("LFS Configuration")]
    [Tooltip("Must match BundleUpdateChecker.lfsBaseUrl")]
    public string lfsBaseUrl = "https://media.githubusercontent.com/media/your-org/mtsamatar-assets/main/";

    private const float DOWNLOAD_TIMEOUT = 60f;

    // ── Local bundle storage path ────────────────────────────
    private string BundleStoragePath =>
        Path.Combine(Application.persistentDataPath, "bundles");

    // ─────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure local bundle storage folder exists
        if (!Directory.Exists(BundleStoragePath))
            Directory.CreateDirectory(BundleStoragePath);
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    /// Downloads a single bundle from LFS by its bundle key.
    /// Bundle key matches bundle_key in manifest.json
    /// e.g. "artifacts/bolo_knife" → downloads bolo_knife.bundle
    public IEnumerator DownloadBundle(string bundleKey)
    {
        if (string.IsNullOrEmpty(bundleKey))
        {
            Debug.LogWarning("[LFSDownloader] Empty bundle key. Skipping.");
            yield break;
        }

        string fileName = GetFileNameFromKey(bundleKey);
        string remoteUrl = lfsBaseUrl + "bundles/" + bundleKey + ".bundle";
        string localPath = Path.Combine(BundleStoragePath, fileName + ".bundle");

        Debug.Log($"[LFSDownloader] Downloading: {bundleKey}");

        using (var request = UnityWebRequest.Get(remoteUrl))
        {
            request.timeout = (int)DOWNLOAD_TIMEOUT;
            var operation = request.SendWebRequest();

            // Report progress while downloading
            while (!operation.isDone)
            {
                OnDownloadProgress?.Invoke(request.downloadProgress);
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LFSDownloader] Failed to download '{bundleKey}': {request.error}");
                OnDownloadFailed?.Invoke($"Failed: {bundleKey} — {request.error}");
                yield break;
            }

            // Save bundle to local storage
            try
            {
                string dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(localPath, request.downloadHandler.data);
                Debug.Log($"[LFSDownloader] Saved: {localPath} " +
                          $"({request.downloadHandler.data.Length / 1024} KB)");
                OnBundleDownloaded?.Invoke(bundleKey);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LFSDownloader] Failed to save bundle '{bundleKey}': {e.Message}");
                OnDownloadFailed?.Invoke(e.Message);
            }
        }
    }

    /// Downloads a list of bundles sequentially in background.
    /// Reports overall progress via OnDownloadProgress event.
    public IEnumerator DownloadBundles(System.Collections.Generic.List<string> bundleKeys)
    {
        if (bundleKeys == null || bundleKeys.Count == 0)
        {
            Debug.Log("[LFSDownloader] No bundles to download.");
            OnAllDownloadsComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[LFSDownloader] Starting download of {bundleKeys.Count} bundle(s)...");

        int completed = 0;
        foreach (var key in bundleKeys)
        {
            yield return DownloadBundle(key);
            completed++;
            float overallProgress = (float)completed / bundleKeys.Count;
            OnDownloadProgress?.Invoke(overallProgress);
            Debug.Log($"[LFSDownloader] Progress: {completed}/{bundleKeys.Count}");
        }

        Debug.Log("[LFSDownloader] All downloads complete. New content active on next launch.");
        OnAllDownloadsComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    /// Checks if a bundle has already been downloaded locally
    public bool IsBundleDownloaded(string bundleKey)
    {
        string fileName = GetFileNameFromKey(bundleKey);
        string localPath = Path.Combine(BundleStoragePath, fileName + ".bundle");
        return File.Exists(localPath);
    }

    /// Returns the local file path for a downloaded bundle
    public string GetLocalBundlePath(string bundleKey)
    {
        string fileName = GetFileNameFromKey(bundleKey);
        return Path.Combine(BundleStoragePath, fileName + ".bundle");
    }

    /// Converts "artifacts/bolo_knife" → "artifacts_bolo_knife"
    private string GetFileNameFromKey(string bundleKey)
    {
        return bundleKey.Replace("/", "_");
    }

    /// Returns total size of all downloaded bundles in MB
    public float GetDownloadedSizeMB()
    {
        if (!Directory.Exists(BundleStoragePath)) return 0f;
        long totalBytes = 0;
        foreach (var file in Directory.GetFiles(BundleStoragePath, "*.bundle",
                                                 SearchOption.AllDirectories))
        {
            totalBytes += new FileInfo(file).Length;
        }
        return totalBytes / (1024f * 1024f);
    }
}