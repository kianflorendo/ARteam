using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class LFSDownloader : MonoBehaviour
{
    public static LFSDownloader Instance { get; private set; }

    public static event Action<float> OnDownloadProgress;
    public static event Action<string> OnBundleDownloaded;
    public static event Action OnAllDownloadsComplete;
    public static event Action<string> OnDownloadFailed;

    [Header("LFS Configuration")]
    [Tooltip("Must match BundleUpdateChecker.lfsBaseUrl")]
    public string lfsBaseUrl = "https://media.githubusercontent.com/media/your-org/mtsamatar-assets/main/";

    private const float DOWNLOAD_TIMEOUT = 60f;

    private string BundleStoragePath =>
        Path.Combine(Application.persistentDataPath, "bundles");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(BundleStoragePath))
            Directory.CreateDirectory(BundleStoragePath);
    }

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

    public bool IsBundleDownloaded(string bundleKey)
    {
        string fileName = GetFileNameFromKey(bundleKey);
        string localPath = Path.Combine(BundleStoragePath, fileName + ".bundle");
        return File.Exists(localPath);
    }

    public string GetLocalBundlePath(string bundleKey)
    {
        string fileName = GetFileNameFromKey(bundleKey);
        return Path.Combine(BundleStoragePath, fileName + ".bundle");
    }

    private string GetFileNameFromKey(string bundleKey)
    {
        return bundleKey.Replace("/", "_");
    }

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
