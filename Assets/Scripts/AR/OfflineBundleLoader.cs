// ============================================================
// OfflineBundleLoader.cs
// Location: Assets/Scripts/AR/OfflineBundleLoader.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Loads 3D prefabs and assets from Addressables bundles.
// Works for both:
//   - Local bundles bundled inside APK (StreamingAssets)
//   - Updated bundles downloaded from LFS (persistentDataPath)
// Addressables automatically picks the correct source.
//
// Called by ArtifactSpawner.cs in Phase 4.
// ============================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class OfflineBundleLoader : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static OfflineBundleLoader Instance { get; private set; }

    // ── Handle cache — tracks loaded assets to avoid reloading
    private Dictionary<string, AsyncOperationHandle> _loadedHandles
        = new Dictionary<string, AsyncOperationHandle>();

    // ── Unload timer — assets unused for this long get unloaded
    private const float UNLOAD_TIMEOUT_SECONDS = 60f;

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
    }

    private void OnDestroy()
    {
        // Release all loaded asset handles on destroy
        foreach (var handle in _loadedHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        _loadedHandles.Clear();
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    /// Loads a GameObject prefab by its Addressable key.
    /// Returns null if the key is not found or bundle is missing.
    /// Key matches bundle_key in manifest.json (e.g. "artifacts/bolo_knife")
    public async Task<GameObject> LoadPrefabAsync(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning("[OfflineBundleLoader] Empty addressable key passed. Returning null.");
            return null;
        }

        // Return cached handle if already loaded
        if (_loadedHandles.TryGetValue(addressableKey, out var existingHandle))
        {
            if (existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return existingHandle.Result as GameObject;
            }
            else
            {
                // Stale handle — remove and reload
                _loadedHandles.Remove(addressableKey);
                if (existingHandle.IsValid())
                    Addressables.Release(existingHandle);
            }
        }

        // Load from Addressables (local bundle or LFS updated bundle)
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

    /// Loads any Unity asset (Texture2D, AudioClip, Sprite etc.) by key
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

    /// Unloads a specific bundle from memory by its addressable key.
    /// Call this after an artifact is no longer visible to free memory.
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

    /// Checks if an asset is currently loaded in memory
    public bool IsLoaded(string addressableKey)
    {
        return _loadedHandles.ContainsKey(addressableKey)
               && _loadedHandles[addressableKey].IsValid()
               && _loadedHandles[addressableKey].Status == AsyncOperationStatus.Succeeded;
    }

    /// Returns how many assets are currently loaded in memory
    public int GetLoadedCount() => _loadedHandles.Count;
}