

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
    public float targetModelSize = 1.2f;

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

                    // Wait up to 5 frames for Renderer.bounds to be initialized.
                    // Bounds are zero in the same frame as Instantiate() for complex
                    // GLTFast-loaded meshes; reading them before they're ready causes
                    // the fallback path (localScale = 0.5f) which still leaves large
                    // models (e.g. mortar at 30 m) filling the screen.
                    {
                        float maxDim = 0f;
                        for (int _attempt = 0; _attempt < 5; _attempt++)
                        {
                            yield return null;
                            if (spawnedObject == null) break;
                            var _renderers = spawnedObject.GetComponentsInChildren<Renderer>();
                            if (_renderers.Length > 0)
                            {
                                Bounds _b = _renderers[0].bounds;
                                for (int _i = 1; _i < _renderers.Length; _i++)
                                    _b.Encapsulate(_renderers[_i].bounds);
                                maxDim = Mathf.Max(_b.size.x, _b.size.y, _b.size.z);
                                if (maxDim > 0.001f) break;
                            }
                        }
                        if (spawnedObject != null)
                        {
                            if (maxDim > 0.001f)
                            {
                                float _s = spawnedObject.transform.localScale.x;
                                spawnedObject.transform.localScale =
                                    Vector3.one * _s * (targetModelSize / maxDim);
                                Debug.Log($"[ArtifactSpawner] AutoScale {artifact.name}: " +
                                          $"{maxDim:F2}m → {targetModelSize}m");
                            }
                            else
                            {
                                Debug.LogWarning($"[ArtifactSpawner] AutoScale {artifact.name}: " +
                                                 $"bounds unavailable after 5 frames.");
                            }
                        }
                    }

                    var instance = spawnedObject.GetComponent<ArtifactInstance>()
                                   ?? spawnedObject.AddComponent<ArtifactInstance>();
                    instance.Initialise(artifact, artifact.anchor_mode);

                    _spawnedArtifacts[artifact.id] = spawnedObject;
                    OnArtifactSpawned?.Invoke(instance);
                    Debug.Log($"[ArtifactSpawner] Spawned 3D artifact: {artifact.name}");
                }
                else
                {
                    Debug.LogWarning($"[ArtifactSpawner] Prefab not found for " +
                                     $"'{artifact.bundle_key}'. Scroll will show only.");
                    // Still register so the geofence doesn't retry every frame
                    // and the scroll (its only visible element) remains shown.
                    var fallback = new GameObject($"FallbackAnchor_{artifact.id}");
                    fallback.transform.SetParent(anchorTransform);
                    fallback.transform.localPosition = Vector3.zero;
                    _spawnedArtifacts[artifact.id] = fallback;
                }
            }
        }
        else if (artifact.type == ArtifactType.InfoOnly)
        {
            // Info-only: create empty anchor object, scroll will attach to it
            var anchor = new GameObject($"InfoAnchor_{artifact.id}");
            anchor.transform.SetParent(anchorTransform);
            anchor.transform.localPosition = Vector3.zero;
            _spawnedArtifacts[artifact.id] = anchor;
            Debug.Log($"[ArtifactSpawner] Info-only anchor: {artifact.name}");
        }

        // -- Step 2: Show scroll UI --
        TryShowScroll(artifact, anchorTransform);

        yield return null;
    }

    private void TryShowScroll(ArtifactData artifact, Transform anchorTransform)
    {
        ScrollUIManager.Instance?.ShowScroll(artifact, anchorTransform);
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
