

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactSpawner : MonoBehaviour
{
    // -- Singleton --
    public static ArtifactSpawner Instance { get; private set; }

    // -- Events --
    public static event System.Action<ArtifactInstance> OnArtifactSpawned;
    public static event System.Action<string> OnArtifactHidden;

    // -- Spawned artifact tracking --
    private Dictionary<string, GameObject> _spawnedArtifacts
        = new Dictionary<string, GameObject>();

    // -- Spawn settings --
    [Header("Spawn Settings")]
    [Tooltip("Auto-scale normalizes every prefab so its largest dimension equals this (meters). " +
             "Works regardless of the model's export units (cm, mm, etc.).")]
    public float targetModelSize = 1.6f;

    private Vector3 SPAWN_OFFSET = new Vector3(0f, 0.05f, 0f);

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

    // ============================================================
    //  Main spawn method
    // ============================================================

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
        GameObject spawnedObject = null;

        // -- Step 1: Load and instantiate 3D prefab (collectible only) --
        if (artifact.type == ArtifactType.Collectible
            && !string.IsNullOrEmpty(artifact.bundle_key))
        {
            if (OfflineBundleLoader.Instance != null)
            {
                var prefabTask = OfflineBundleLoader.Instance.LoadPrefabAsync(artifact.bundle_key);
                yield return new WaitUntil(() => prefabTask.IsCompleted);

                if (prefabTask.Result != null)
                {
                    spawnedObject = Instantiate(
                        prefabTask.Result,
                        anchorTransform.position + SPAWN_OFFSET,
                        anchorTransform.rotation,
                        anchorTransform
                    );

                    // Hide all renderers immediately so the model is invisible while
                    // we wait for GLTFast to upload mesh data to the GPU. Without this,
                    // complex models (e.g. mortar exported at wrong units = 50+ Unity meters)
                    // fill the entire screen for up to 3 seconds while auto-scale runs.
                    // GetComponentsInChildren(true) includes disabled components.
                    var _initialRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in _initialRenderers) r.enabled = false;

                    var instance = spawnedObject.GetComponent<ArtifactInstance>()
                                   ?? spawnedObject.AddComponent<ArtifactInstance>();
                    instance.Initialise(artifact, artifact.anchor_mode);

                    if (artifact.anchor_mode == AnchorMode.GPS)
                        spawnedObject.AddComponent<GPSArtifactInteraction>();

                    _spawnedArtifacts[artifact.id] = spawnedObject;
                    OnArtifactSpawned?.Invoke(instance);

                    // Show scroll immediately — user can read artifact info while the
                    // model finishes loading and scaling in the background.
                    TryShowScroll(artifact, anchorTransform);

                    // Auto-scale: measure the world-space bounding box and normalise
                    // so the largest dimension equals targetModelSize.
                    //
                    // WHY MeshFilter.sharedMesh.bounds instead of Renderer.bounds:
                    //   Renderer.bounds for a DISABLED renderer can return zero or stale
                    //   data in Unity/GLTFast — the GPU hasn't rendered it yet so the
                    //   cached AABB is wrong. sharedMesh.bounds is CPU mesh data that
                    //   is always valid the moment GLTFast writes it, regardless of the
                    //   renderer's enabled state. We scale it by lossyScale to get world
                    //   size, which correctly handles nested transform hierarchies.
                    {
                        float maxDim = 0f;
                        float _elapsed = 0f;
                        const float _maxWait = 5f;

                        while (_elapsed < _maxWait && spawnedObject != null)
                        {
                            yield return null;
                            _elapsed += Time.deltaTime;

                            var _renderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
                            foreach (var _r in _renderers)
                            {
                                // Prefer CPU mesh bounds (works even when renderer disabled).
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
                                    // Fallback: Renderer.bounds (only valid when enabled).
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
                                // Bounds never became valid — apply a safe fallback scale.
                                // 0.5f prevents a screen-filling giant while still showing something.
                                spawnedObject.transform.localScale = Vector3.one * 0.5f;
                                Debug.LogWarning($"[ArtifactSpawner] AutoScale {artifact.name}: " +
                                                 $"bounds unavailable after {_maxWait:F1}s — fallback 0.5");
                            }

                            // Reveal the model now that it is correctly scaled.
                            var _finalRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
                            foreach (var r in _finalRenderers) r.enabled = true;
                        }
                    }

                    Debug.Log($"[ArtifactSpawner] Spawned 3D artifact: {artifact.name}");
                }
                else
                {
                    Debug.LogWarning($"[ArtifactSpawner] Prefab not found for " +
                                     $"'{artifact.bundle_key}'. Scroll will show only.");
                    var fallback = new GameObject($"FallbackAnchor_{artifact.id}");
                    fallback.transform.SetParent(anchorTransform);
                    fallback.transform.localPosition = Vector3.zero;
                    _spawnedArtifacts[artifact.id] = fallback;
                    TryShowScroll(artifact, anchorTransform);
                }
            }
        }
        else if (artifact.type == ArtifactType.InfoOnly)
        {
            var anchor = new GameObject($"InfoAnchor_{artifact.id}");
            anchor.transform.SetParent(anchorTransform);
            anchor.transform.localPosition = Vector3.zero;
            _spawnedArtifacts[artifact.id] = anchor;
            Debug.Log($"[ArtifactSpawner] Info-only anchor: {artifact.name}");
            TryShowScroll(artifact, anchorTransform);
        }

        yield return null;
    }

    private void TryShowScroll(ArtifactData artifact, Transform anchorTransform)
    {
        // Scroll UI disabled — only 3D models appear in AR.
        // The info card was rendering as a large 3D world-space panel that
        // blocked the camera view. Re-enable when the scroll is redesigned
        // as a 2D screen-space HUD instead of a world-anchored object.
        // ScrollUIManager.Instance?.ShowScroll(artifact, anchorTransform);
    }
    
    // ============================================================
    //  Hide / Show / Despawn
    // ============================================================

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
                instance?.Show();
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

    // ============================================================
    //  Query methods
    // ============================================================

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
