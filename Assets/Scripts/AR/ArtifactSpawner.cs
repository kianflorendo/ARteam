using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactSpawner : MonoBehaviour
{
    public static ArtifactSpawner Instance { get; private set; }

    public static event System.Action<ArtifactInstance> OnArtifactSpawned;
    public static event System.Action<string> OnArtifactHidden;
    // Fired when a GPS artifact's 3D model becomes visible after auto-scale.
    // OfflineGPSRouteManager resets the auto-advance segment start on this event so
    // the player always gets the full walk distance to interact with the visible model.
    public static event System.Action<string> OnArtifactModelVisible;

    private Dictionary<string, GameObject> _spawnedArtifacts
        = new Dictionary<string, GameObject>();

    [Header("Spawn Settings")]
    [Tooltip("Auto-scale normalizes every prefab so its largest dimension equals this (meters). " +
             "Works regardless of the model's export units (cm, mm, etc.).")]
    public float targetModelSize = 1.6f;

    private Vector3 SPAWN_OFFSET = new Vector3(0f, 0.05f, 0f);

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

    public void Spawn(ArtifactData artifact, Transform anchorTransform)
    {
        if (artifact == null)
        {
            Debug.LogWarning("[ArtifactSpawner] Spawn called with null artifact.");
            return;
        }

        if (IsSpawned(artifact.id))
        {
            Debug.Log($"[ArtifactSpawner] {artifact.id} already spawned. Skipping.");
            return;
        }

        StartCoroutine(SpawnCoroutine(artifact, anchorTransform));
    }

    private IEnumerator SpawnCoroutine(ArtifactData artifact, Transform anchorTransform)
    {
        if (artifact.type == ArtifactType.Collectible
            && !string.IsNullOrEmpty(artifact.bundle_key))
        {
            // Try to load the 3D prefab. If the loader is missing or the key is not
            // in the catalog, fall through to the collect-only fallback so the player
            // can still tap COLLECT even without a visible model.
            GameObject loadedPrefab = null;

            if (OfflineBundleLoader.Instance != null)
            {
                var prefabTask = OfflineBundleLoader.Instance.LoadPrefabAsync(artifact.bundle_key);
                yield return new WaitUntil(() => prefabTask.IsCompleted);
                loadedPrefab = prefabTask.Result;
            }
            else
            {
                Debug.LogWarning($"[ArtifactSpawner] OfflineBundleLoader not found — " +
                                 $"falling back to collect-only for {artifact.id}.");
            }

            if (loadedPrefab != null)
            {
                var spawnedObject = Instantiate(
                    loadedPrefab,
                    anchorTransform.position + SPAWN_OFFSET,
                    anchorTransform.rotation,
                    anchorTransform
                );

                // Hide all renderers immediately so the model is invisible while we wait
                // for GLTFast to upload mesh data to the GPU. Without this, models with
                // wrong export units (e.g. mortar exported at 50+ Unity meters) fill the
                // entire screen for up to 3 seconds while auto-scale runs.
                var _initialRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
                foreach (var r in _initialRenderers) r.enabled = false;

                var instance = spawnedObject.GetComponent<ArtifactInstance>()
                               ?? spawnedObject.AddComponent<ArtifactInstance>();
                instance.Initialise(artifact, artifact.anchor_mode);

                if (artifact.anchor_mode == AnchorMode.GPS)
                    spawnedObject.AddComponent<GPSArtifactInteraction>();

                _spawnedArtifacts[artifact.id] = spawnedObject;
                OnArtifactSpawned?.Invoke(instance);

                TryShowScroll(artifact, anchorTransform);

                // Auto-scale: measure the world-space bounding box and normalise so the
                // largest dimension equals targetModelSize.
                //
                // WHY MeshFilter.sharedMesh.bounds instead of Renderer.bounds:
                //   Renderer.bounds for a DISABLED renderer can return zero or stale data
                //   in Unity/GLTFast — the GPU hasn't rendered it yet so the cached AABB
                //   is wrong. sharedMesh.bounds is CPU mesh data that is always valid the
                //   moment GLTFast writes it. We scale by lossyScale to get world size,
                //   correctly handling nested transform hierarchies.
                float maxDim = 0f;
                float _elapsed = 0f;
                const float _maxWait = 5f;
                // If no renderers exist after 0.5s the prefab is empty/broken.
                // Stop waiting early so auto-advance can fire in time.
                const float _noRendererTimeout = 0.5f;

                while (_elapsed < _maxWait && spawnedObject != null)
                {
                    yield return null;
                    _elapsed += Time.deltaTime;

                    var _renderers = spawnedObject.GetComponentsInChildren<Renderer>(true);

                    if (_renderers.Length == 0 && _elapsed >= _noRendererTimeout)
                        break;

                    foreach (var _r in _renderers)
                    {
                        var _mf = _r.GetComponent<MeshFilter>();
                        if (_mf?.sharedMesh != null)
                        {
                            Vector3 _ext = _mf.sharedMesh.bounds.extents;
                            Vector3 _ls  = _r.transform.lossyScale;
                            float   _d   = Mathf.Max(
                                Mathf.Abs(_ext.x * _ls.x),
                                Mathf.Abs(_ext.y * _ls.y),
                                Mathf.Abs(_ext.z * _ls.z)) * 2f;
                            if (_d > maxDim) maxDim = _d;
                        }
                        else if (_r.enabled)
                        {
                            float _d = Mathf.Max(_r.bounds.size.x,
                                                 _r.bounds.size.y,
                                                 _r.bounds.size.z);
                            if (_d > maxDim) maxDim = _d;
                        }
                    }
                    if (maxDim > 0.001f) break;
                }

                if (spawnedObject != null)
                {
                    if (maxDim > 0.001f)
                    {
                        float _s = spawnedObject.transform.localScale.x;
                        spawnedObject.transform.localScale =
                            Vector3.one * _s * (targetModelSize / maxDim);
                        Debug.Log($"[ArtifactSpawner] AutoScale {artifact.name}: " +
                                  $"{maxDim:F2}m → {targetModelSize}m ({_elapsed:F2}s)");
                    }
                    else
                    {
                        // Bounds never became valid — prefab likely has no mesh.
                        // Apply fallback scale; auto-advance will skip this artifact
                        // after the player walks the required distance.
                        spawnedObject.transform.localScale = Vector3.one * 0.5f;
                        Debug.LogWarning($"[ArtifactSpawner] AutoScale {artifact.name}: " +
                                         $"no renderers/bounds after {_elapsed:F2}s — " +
                                         $"check prefab in Unity Editor. Fallback 0.5.");
                    }

                    var _finalRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in _finalRenderers) r.enabled = true;

                    // Notify route manager the model is now visible so it can reset
                    // the auto-advance segment start. This guarantees the player gets
                    // a full walk distance WITH the model visible, not during the
                    // hidden loading/scaling window.
                    OnArtifactModelVisible?.Invoke(artifact.id);
                }

                Debug.Log($"[ArtifactSpawner] Spawned 3D artifact: {artifact.name}");
            }
            else
            {
                // Prefab unavailable (Addressables not built, key missing, or
                // OfflineBundleLoader absent). Register a lightweight anchor so
                // IsSpawned() returns true and the collect panel still appears.
                Debug.LogWarning($"[ArtifactSpawner] No prefab for '{artifact.bundle_key}' — " +
                                 $"collect panel only for {artifact.id}.");

                var fallback = new GameObject($"FallbackAnchor_{artifact.id}");
                fallback.transform.SetParent(anchorTransform);
                fallback.transform.localPosition = Vector3.zero;
                _spawnedArtifacts[artifact.id] = fallback;

                var fallbackInstance = fallback.AddComponent<ArtifactInstance>();
                fallbackInstance.Initialise(artifact, artifact.anchor_mode);
                if (artifact.anchor_mode == AnchorMode.GPS)
                    fallback.AddComponent<GPSArtifactInteraction>();

                OnArtifactSpawned?.Invoke(fallbackInstance);
                // Fire model-visible so OfflineGPSRouteManager resets the segment
                // start — otherwise the next artifact's walk window starts from when
                // the invisible anchor registered, not from when it became interactive.
                OnArtifactModelVisible?.Invoke(artifact.id);
                TryShowScroll(artifact, anchorTransform);
            }
        }
        else if (artifact.type == ArtifactType.InfoOnly)
        {
            var anchor = new GameObject($"InfoAnchor_{artifact.id}");
            anchor.transform.SetParent(anchorTransform);
            anchor.transform.localPosition = Vector3.zero;
            _spawnedArtifacts[artifact.id] = anchor;

            var infoInstance = anchor.AddComponent<ArtifactInstance>();
            infoInstance.Initialise(artifact, artifact.anchor_mode);
            OnArtifactSpawned?.Invoke(infoInstance);

            Debug.Log($"[ArtifactSpawner] Info-only anchor: {artifact.name}");
        }

        yield return null;
    }

    private void TryShowScroll(ArtifactData artifact, Transform anchorTransform)
    {
        // Scroll UI disabled — only 3D models appear in AR. The info card was rendering
        // as a large 3D world-space panel that blocked the camera view. Re-enable when
        // the scroll is redesigned as a 2D screen-space HUD instead of a world-anchored object.
    }

    public void Hide(string artifactId)
    {
        if (_spawnedArtifacts.TryGetValue(artifactId, out var obj))
        {
            if (obj != null)
            {
                var instance = obj.GetComponent<ArtifactInstance>();
                instance?.Hide();
            }
            OnArtifactHidden?.Invoke(artifactId);
            Debug.Log($"[ArtifactSpawner] Hidden: {artifactId}");
        }
    }

    public void Show(string artifactId)
    {
        if (_spawnedArtifacts.TryGetValue(artifactId, out var obj))
        {
            if (obj != null)
            {
                var instance = obj.GetComponent<ArtifactInstance>();
                if (instance != null)
                {
                    // Only re-fire OnArtifactSpawned if the artifact was previously hidden.
                    // ARCore fires trackablesChanged every frame while tracking — re-firing
                    // unconditionally would reset and reflash the UI panel continuously.
                    bool wasHidden = !instance.IsVisible;
                    instance.Show();
                    if (wasHidden && instance.ArtifactData?.anchor_mode == AnchorMode.Image)
                        OnArtifactSpawned?.Invoke(instance);
                }
                else
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    public void Despawn(string artifactId)
    {
        if (_spawnedArtifacts.TryGetValue(artifactId, out var obj))
        {
            if (obj != null) Destroy(obj);
            _spawnedArtifacts.Remove(artifactId);
            Debug.Log($"[ArtifactSpawner] Despawned: {artifactId}");
        }
    }

    public bool IsSpawned(string artifactId)
        => _spawnedArtifacts.ContainsKey(artifactId);

    public GameObject GetSpawnedObject(string artifactId)
        => _spawnedArtifacts.TryGetValue(artifactId, out var obj) ? obj : null;

    public int GetSpawnedCount() => _spawnedArtifacts.Count;

    private void OnDestroy()
    {
        foreach (var obj in _spawnedArtifacts.Values)
            if (obj != null) Destroy(obj);
        _spawnedArtifacts.Clear();
    }
}
