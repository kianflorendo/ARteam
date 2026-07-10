using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class OfflineBundleLoader : MonoBehaviour
{
    public static OfflineBundleLoader Instance { get; private set; }

    private Dictionary<string, AsyncOperationHandle> _loadedHandles
        = new Dictionary<string, AsyncOperationHandle>();

    private const float UNLOAD_TIMEOUT_SECONDS = 60f;

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

    private void OnDestroy()
    {
        foreach (var handle in _loadedHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        _loadedHandles.Clear();
    }

    public async Task<GameObject> LoadPrefabAsync(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning("[OfflineBundleLoader] Empty addressable key passed. Returning null.");
            return null;
        }

        if (_loadedHandles.TryGetValue(addressableKey, out var existingHandle))
        {
            if (existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return existingHandle.Result as GameObject;
            }
            else
            {
                _loadedHandles.Remove(addressableKey);
                if (existingHandle.IsValid())
                    Addressables.Release(existingHandle);
            }
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _loadedHandles[addressableKey] = handle;
            Debug.Log($"[OfflineBundleLoader] Loaded prefab: {addressableKey}");
            return handle.Result;
        }
        else
        {
            Debug.LogError($"[OfflineBundleLoader] Failed to load prefab: '{addressableKey}'. " +
                           "Make sure this asset is marked Addressable with the correct key " +
                           "and the Addressables bundle has been built.");
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }
    }

    public async Task<T> LoadAssetAsync<T>(string addressableKey) where T : Object
    {
        if (string.IsNullOrEmpty(addressableKey))
            return null;

        var handle = Addressables.LoadAssetAsync<T>(addressableKey);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _loadedHandles[addressableKey] = handle;
            return handle.Result;
        }
        else
        {
            Debug.LogError($"[OfflineBundleLoader] Failed to load asset: '{addressableKey}'.");
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }
    }

    public void UnloadBundle(string addressableKey)
    {
        if (_loadedHandles.TryGetValue(addressableKey, out var handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
                Debug.Log($"[OfflineBundleLoader] Unloaded: {addressableKey}");
            }
            _loadedHandles.Remove(addressableKey);
        }
    }

    public bool IsLoaded(string addressableKey)
    {
        return _loadedHandles.ContainsKey(addressableKey)
               && _loadedHandles[addressableKey].IsValid()
               && _loadedHandles[addressableKey].Status == AsyncOperationStatus.Succeeded;
    }

    public int GetLoadedCount() => _loadedHandles.Count;
}
