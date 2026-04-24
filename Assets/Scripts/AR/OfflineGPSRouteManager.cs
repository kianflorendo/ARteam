using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offline route controller for GPS artifacts.
/// GPS is used to lock the player's first route origin.
/// The exact unlock distances are measured in AR world meters so the
/// 1m / 5m / 10m thresholds are reliable without ARCore Geospatial.
/// </summary>
[DefaultExecutionOrder(-60)]
public class OfflineGPSRouteManager : MonoBehaviour
{
    public static OfflineGPSRouteManager Instance { get; private set; }

    [Header("Route Progression")]
    public float routeCheckInterval = 0.15f;
    public float defaultSpawnDistanceFromPlayer = 1f;
    public float spawnHeightOffset = -0.3f;

    private readonly Dictionary<string, GameObject> _presentationAnchors = new Dictionary<string, GameObject>();

    private List<ArtifactData> _routeArtifacts = new List<ArtifactData>();
    private float _routeCheckTimer;
    private bool _routeLoaded;
    private bool _hasSegmentStart;
    private Vector3 _segmentStartPosition;
    private float _currentSegmentDistance;
    public bool HasOrigin =>
        GPSRouteStateStore.Instance != null && GPSRouteStateStore.Instance.State.has_origin;

    public int NextSequenceIndex =>
        GPSRouteStateStore.Instance != null ? GPSRouteStateStore.Instance.State.next_sequence_index : 1;

    public string ActiveArtifactId =>
        GPSRouteStateStore.Instance != null ? GPSRouteStateStore.Instance.State.active_artifact_id : "";

    public float CurrentSegmentDistance => _currentSegmentDistance;

    public string CurrentTargetName
    {
        get
        {
            var next = GetNextRouteArtifact();
            return next != null ? next.name : "None";
        }
    }

    public float CurrentTargetDistance
    {
        get
        {
            var next = GetNextRouteArtifact();
            return next != null ? next.distance_from_previous_meters : 0f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;

        var go = new GameObject("[AUTO] OfflineGPSRouteManager");
        go.AddComponent<OfflineGPSRouteManager>();
    }

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

    private void OnEnable()
    {
        InventoryManager.OnArtifactCollected += HandleArtifactCollected;
        ManifestLoader.OnManifestLoaded += ReloadRouteArtifacts;
    }

    private void OnDisable()
    {
        InventoryManager.OnArtifactCollected -= HandleArtifactCollected;
        ManifestLoader.OnManifestLoaded -= ReloadRouteArtifacts;
    }

    private void Update()
    {
        _routeCheckTimer += Time.deltaTime;
        if (_routeCheckTimer < routeCheckInterval)
            return;

        _routeCheckTimer = 0f;

        if (!DependenciesReady())
            return;

        if (!_routeLoaded)
            ReloadRouteArtifacts();

        if (_routeArtifacts.Count == 0)
            return;

        if (!EnsureOriginCaptured())
            return;

        ReconcileProgressWithInventory();

        if (!string.IsNullOrEmpty(ActiveArtifactId))
        {
            EnsureActiveArtifactPresented();
            return;
        }

        var nextArtifact = GetNextRouteArtifact();
        if (nextArtifact == null)
            return;

        if (!EnsureSegmentStart())
            return;

        _currentSegmentDistance = GetDistanceFromSegmentStart();
        if (_currentSegmentDistance < nextArtifact.distance_from_previous_meters)
            return;

        UnlockArtifact(nextArtifact);
    }

    private bool DependenciesReady()
    {
        return ManifestLoader.Instance != null
               && ManifestLoader.Instance.IsLoaded
               && InventoryManager.Instance != null
               && GPSRouteStateStore.Instance != null
               && LocationServiceManager.Instance != null
               && ArtifactSpawner.Instance != null
               && Camera.main != null;
    }

    private void ReloadRouteArtifacts()
    {
        _routeArtifacts = ManifestLoader.Instance != null
            ? ManifestLoader.Instance.GetGPSRouteArtifacts()
            : new List<ArtifactData>();
        _routeLoaded = true;
    }

    private bool EnsureOriginCaptured()
    {
        var state = GPSRouteStateStore.Instance.State;
        if (state.has_origin)
            return true;

        if (!LocationServiceManager.Instance.HasStableFix)
            return false;

        if (!LocationServiceManager.Instance.TryGetFilteredLocation(
                out double lat,
                out double lng,
                out float accuracy))
        {
            return false;
        }

        state.origin_lat = lat;
        state.origin_lng = lng;
        state.origin_accuracy_m = accuracy;
        state.initialized_at = DateTime.UtcNow.ToString("o");
        state.has_origin = true;
        state.next_sequence_index = Mathf.Max(1, state.next_sequence_index);
        GPSRouteStateStore.Instance.Save();

        SetSegmentStartFromCamera();
        Debug.Log($"[OfflineGPSRouteManager] Route origin captured at {lat:F6}, {lng:F6} (+/-{accuracy:F1}m).");
        return true;
    }

    private void ReconcileProgressWithInventory()
    {
        var state = GPSRouteStateStore.Instance.State;
        bool dirty = false;

        if (!string.IsNullOrEmpty(state.active_artifact_id)
            && InventoryManager.Instance.IsCollected(state.active_artifact_id))
        {
            state.active_artifact_id = "";
            dirty = true;
        }

        while (true)
        {
            var next = _routeArtifacts.Find(a => a.sequence_index == state.next_sequence_index);
            if (next == null || !InventoryManager.Instance.IsCollected(next.id))
                break;

            state.next_sequence_index++;
            dirty = true;
        }

        if (dirty)
            GPSRouteStateStore.Instance.Save();
    }

    private void EnsureActiveArtifactPresented()
    {
        var activeArtifact = ManifestLoader.Instance.GetArtifact(ActiveArtifactId);
        if (activeArtifact == null || InventoryManager.Instance.IsCollected(activeArtifact.id))
        {
            GPSRouteStateStore.Instance.State.active_artifact_id = "";
            GPSRouteStateStore.Instance.Save();
            return;
        }

        if (!ArtifactSpawner.Instance.IsSpawned(activeArtifact.id))
            PresentArtifact(activeArtifact);
    }

    private ArtifactData GetNextRouteArtifact()
    {
        if (_routeArtifacts == null || _routeArtifacts.Count == 0)
            return null;

        int nextSequence = NextSequenceIndex;
        return _routeArtifacts.Find(a =>
            a.sequence_index == nextSequence
            && !InventoryManager.Instance.IsCollected(a.id));
    }

    private bool EnsureSegmentStart()
    {
        if (_hasSegmentStart)
            return true;

        return SetSegmentStartFromCamera();
    }

    private bool SetSegmentStartFromCamera()
    {
        if (Camera.main == null)
            return false;

        _segmentStartPosition = Flatten(Camera.main.transform.position);
        _currentSegmentDistance = 0f;
        _hasSegmentStart = true;
        return true;
    }

    private float GetDistanceFromSegmentStart()
    {
        if (Camera.main == null)
            return 0f;

        return Vector3.Distance(_segmentStartPosition, Flatten(Camera.main.transform.position));
    }

    private void UnlockArtifact(ArtifactData artifact)
    {
        GPSRouteStateStore.Instance.State.active_artifact_id = artifact.id;
        GPSRouteStateStore.Instance.Save();

        _hasSegmentStart = false;
        _currentSegmentDistance = artifact.distance_from_previous_meters;

        PresentArtifact(artifact);

        Debug.Log($"[OfflineGPSRouteManager] Unlocked GPS artifact {artifact.id} after {_currentSegmentDistance:F2}m.");
    }

    private void PresentArtifact(ArtifactData artifact)
    {
        if (Camera.main == null)
            return;

        if (_presentationAnchors.TryGetValue(artifact.id, out var existingAnchor)
            && existingAnchor != null)
        {
            return;
        }

        DestroyPresentationAnchor(artifact.id);

        var anchorObject = new GameObject($"GPSRouteAnchor_{artifact.id}");
        _presentationAnchors[artifact.id] = anchorObject;

        float spawnDistance = artifact.spawn_distance_from_player_meters > 0f
            ? artifact.spawn_distance_from_player_meters
            : defaultSpawnDistanceFromPlayer;

        if (!string.IsNullOrEmpty(artifact.spawn_presentation)
            && artifact.spawn_presentation != GPSSpawnPresentation.CameraForward)
        {
            Debug.Log($"[OfflineGPSRouteManager] Spawn presentation '{artifact.spawn_presentation}' is not configured in-scene yet. Falling back to camera_forward for {artifact.id}.");
        }

        Vector3 flatForward = Camera.main.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;
        else
            flatForward.Normalize();

        anchorObject.transform.position =
            new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + spawnHeightOffset,
                Camera.main.transform.position.z)
            + flatForward * spawnDistance;

        anchorObject.transform.rotation = Quaternion.Euler(
            0f,
            Camera.main.transform.eulerAngles.y + 180f,
            0f);

        ArtifactSpawner.Instance.Spawn(artifact, anchorObject.transform);
    }

    private void HandleArtifactCollected(string artifactId)
    {
        if (GPSRouteStateStore.Instance == null)
            return;

        var state = GPSRouteStateStore.Instance.State;
        if (state.active_artifact_id != artifactId)
            return;

        var artifact = ManifestLoader.Instance != null
            ? ManifestLoader.Instance.GetArtifact(artifactId)
            : null;

        if (artifact == null)
            return;

        ArtifactSpawner.Instance?.Despawn(artifactId);
        DestroyPresentationAnchor(artifactId);

        state.active_artifact_id = "";
        state.next_sequence_index = Mathf.Max(state.next_sequence_index, artifact.sequence_index + 1);
        GPSRouteStateStore.Instance.Save();

        SetSegmentStartFromCamera();

        Debug.Log($"[OfflineGPSRouteManager] Completed GPS artifact {artifact.id}. Next sequence: {state.next_sequence_index}");
    }

    private void DestroyPresentationAnchor(string artifactId)
    {
        if (_presentationAnchors.TryGetValue(artifactId, out var anchor) && anchor != null)
            Destroy(anchor);

        _presentationAnchors.Remove(artifactId);
    }

    private static Vector3 Flatten(Vector3 position)
    {
        return new Vector3(position.x, 0f, position.z);
    }

    private void OnDestroy()
    {
        foreach (var anchor in _presentationAnchors.Values)
        {
            if (anchor != null)
                Destroy(anchor);
        }
        _presentationAnchors.Clear();
    }
}
