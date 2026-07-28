using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// GPS is used only to lock the route origin. Exact unlock distances are measured in
// AR world meters so the per-segment thresholds are reliable without ARCore Geospatial.
[DefaultExecutionOrder(-60)]
public class OfflineGPSRouteManager : MonoBehaviour
{
    public static OfflineGPSRouteManager Instance { get; private set; }

    [Header("Route Progression")]
    public float routeCheckInterval = 0.15f;
    public float defaultSpawnDistanceFromPlayer = 1f;
    // ArtifactSpawner.SPAWN_OFFSET adds +0.05m on top of this. Net GPS spawn = camera Y + 0.05m.
    public float spawnHeightOffset = 0f;

    private readonly Dictionary<string, GameObject> _presentationAnchors = new Dictionary<string, GameObject>();

    private List<ArtifactData> _routeArtifacts = new List<ArtifactData>();
    private float _routeCheckTimer;
    private bool _routeLoaded;
    private bool _inventoryCleared;
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
            // While an artifact is active, the real walking target is the NEXT artifact after it.
            if (!string.IsNullOrEmpty(ActiveArtifactId))
            {
                var active = ManifestLoader.Instance?.GetArtifact(ActiveArtifactId);
                if (active != null)
                {
                    ArtifactData nextAfter = null;
                    if (_routeArtifacts != null)
                        foreach (var _a in _routeArtifacts)
                            if (_a.sequence_index > active.sequence_index
                                && !InventoryManager.Instance.IsCollected(_a.id)
                                && (nextAfter == null || _a.sequence_index < nextAfter.sequence_index))
                                nextAfter = _a;
                    if (nextAfter != null)
                        return $"({active.name} active) → {nextAfter.name}";
                }
            }
            var next = GetNextRouteArtifact();
            return next != null ? next.name : "None";
        }
    }

    public float CurrentTargetDistance
    {
        get
        {
            if (!string.IsNullOrEmpty(ActiveArtifactId))
            {
                var active = ManifestLoader.Instance?.GetArtifact(ActiveArtifactId);
                if (active != null)
                {
                    ArtifactData nextAfter = null;
                    if (_routeArtifacts != null)
                        foreach (var _a in _routeArtifacts)
                            if (_a.sequence_index > active.sequence_index
                                && !InventoryManager.Instance.IsCollected(_a.id)
                                && (nextAfter == null || _a.sequence_index < nextAfter.sequence_index))
                                nextAfter = _a;
                    if (nextAfter != null)
                        return nextAfter.distance_from_previous_meters;
                }
            }
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
        ArtifactSpawner.OnArtifactModelVisible += HandleArtifactModelVisible;
    }

    private void OnDisable()
    {
        InventoryManager.OnArtifactCollected -= HandleArtifactCollected;
        ManifestLoader.OnManifestLoaded -= ReloadRouteArtifacts;
        ArtifactSpawner.OnArtifactModelVisible -= HandleArtifactModelVisible;
    }

    private void Update()
    {
        _routeCheckTimer += Time.deltaTime;
        if (_routeCheckTimer < routeCheckInterval)
            return;

        _routeCheckTimer = 0f;

        // Sacred exclusion zone (e.g. the memorial building) — never spawn GPS artifacts
        // there, and hide anything already presented if the player walks into it.
        if (GeofenceGuard.Instance != null && GeofenceGuard.Instance.IsInsideExclusionZone)
        {
            if (!string.IsNullOrEmpty(ActiveArtifactId))
            {
                ArtifactSpawner.Instance?.Despawn(ActiveArtifactId);
                DestroyPresentationAnchor(ActiveArtifactId);
            }
            return;
        }

        if (!DependenciesReady())
            return;

        if (!_routeLoaded)
            ReloadRouteArtifacts();

        if (_routeArtifacts.Count == 0)
            return;

        // Deferred inventory clear: runs once per route-load cycle as soon as
        // InventoryManager is confirmed ready. Keeping this separate from
        // ReloadRouteArtifacts avoids the race where OnManifestLoaded fires before
        // InventoryManager.Awake completes, which would skip the clear and leave
        // stale collected-artifact entries from previous sessions — causing the
        // route to start from a mid-sequence artifact instead of seq=1.
        if (!_inventoryCleared)
        {
            var ids = new List<string>();
            foreach (var a in _routeArtifacts) ids.Add(a.id);
            InventoryManager.Instance.RemoveCollectedArtifacts(ids);
            _inventoryCleared = true;
            Debug.Log($"[OfflineGPSRouteManager] Inventory cleared ({ids.Count} GPS artifacts) — route starts from seq=1.");
        }

        if (!EnsureOriginCaptured())
            return;

        ReconcileProgressWithInventory();

        // Auto-reset when every route artifact has been seen so the tester loops back
        // to seq=1 without pressing RESET. has_origin is preserved so the GPS wait is
        // NOT triggered again — the route restarts immediately.
        if (string.IsNullOrEmpty(ActiveArtifactId) && IsRouteComplete())
        {
            Debug.Log("[OfflineGPSRouteManager] All route artifacts done — auto-resetting for next loop.");
            var ids = new List<string>();
            foreach (var a in _routeArtifacts) ids.Add(a.id);
            InventoryManager.Instance?.RemoveCollectedArtifacts(ids);

            var st = GPSRouteStateStore.Instance.State;
            st.active_artifact_id  = "";
            st.next_sequence_index = 1;
            GPSRouteStateStore.Instance.Save();

            _hasSegmentStart       = false;
            _currentSegmentDistance = 0f;
            _routeLoaded           = false;
            _inventoryCleared      = false;
            return;
        }

        if (!string.IsNullOrEmpty(ActiveArtifactId))
        {
            EnsureActiveArtifactPresented();

            // Keep tracking distance even while an artifact is active (not yet collected)
            // so the next artifact can unlock automatically. Prevents the route from
            // getting stuck when the player walks past without tapping Collect.
            var activeArt = ManifestLoader.Instance?.GetArtifact(ActiveArtifactId);
            if (activeArt != null)
            {
                // Min-sequence search so non-contiguous per-soldier routes work correctly.
                ArtifactData nextAfterActive = null;
                foreach (var _a in _routeArtifacts)
                {
                    if (_a.sequence_index > activeArt.sequence_index
                        && !InventoryManager.Instance.IsCollected(_a.id)
                        && (nextAfterActive == null || _a.sequence_index < nextAfterActive.sequence_index))
                        nextAfterActive = _a;
                }

                if (nextAfterActive != null)
                {
                    if (EnsureSegmentStart())
                    {
                        _currentSegmentDistance = GetDistanceFromSegmentStart();
                        if (_currentSegmentDistance >= nextAfterActive.distance_from_previous_meters)
                        {
                            ArtifactSpawner.Instance?.Despawn(ActiveArtifactId);
                            DestroyPresentationAnchor(ActiveArtifactId);
                            var st = GPSRouteStateStore.Instance.State;
                            st.active_artifact_id = "";
                            st.next_sequence_index = activeArt.sequence_index + 1;
                            GPSRouteStateStore.Instance.Save();
                            Debug.Log($"[OfflineGPSRouteManager] Auto-advanced past {activeArt.id} — unlocking {nextAfterActive.id}.");
                            UnlockArtifact(nextAfterActive);
                        }
                    }
                }
                else
                {
                    // Active artifact is the LAST in the route (no seq+1 artifact exists).
                    // Without this block the route gets permanently stuck: nextAfterActive
                    // is always null, so auto-advance never fires, next_sequence_index never
                    // advances past maxSeq, and IsRouteComplete never returns true.
                    if (EnsureSegmentStart())
                    {
                        _currentSegmentDistance = GetDistanceFromSegmentStart();
                        if (_currentSegmentDistance >= activeArt.distance_from_previous_meters)
                        {
                            ArtifactSpawner.Instance?.Despawn(ActiveArtifactId);
                            DestroyPresentationAnchor(ActiveArtifactId);
                            var st = GPSRouteStateStore.Instance.State;
                            st.active_artifact_id  = "";
                            st.next_sequence_index = activeArt.sequence_index + 1;
                            GPSRouteStateStore.Instance.Save();
                            Debug.Log($"[OfflineGPSRouteManager] Last artifact {activeArt.id} passed — route complete, resetting for next loop.");
                        }
                    }
                }
            }
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

    private bool IsRouteComplete()
    {
        if (_routeArtifacts == null || _routeArtifacts.Count == 0) return false;
        int maxSeq = 0;
        foreach (var a in _routeArtifacts)
            maxSeq = Mathf.Max(maxSeq, a.sequence_index);
        return maxSeq > 0 && GPSRouteStateStore.Instance.State.next_sequence_index > maxSeq;
    }

    private bool DependenciesReady()
    {
        return ManifestLoader.Instance != null
               && ManifestLoader.Instance.IsLoaded
               && InventoryManager.Instance != null
               && GPSRouteStateStore.Instance != null
               && LocationServiceManager.Instance != null
               && ArtifactSpawner.Instance != null
               && Camera.main != null
               && (GeofenceGuard.Instance == null || GeofenceGuard.Instance.IsInsideGeofence);
    }

    private void ReloadRouteArtifacts()
    {
        _routeArtifacts = ManifestLoader.Instance != null
            ? ManifestLoader.Instance.GetGPSRouteArtifacts()
            : new List<ArtifactData>();
        _routeLoaded = true;
        // _inventoryCleared stays false — the deferred clear in Update handles it
        // once InventoryManager is confirmed ready (avoids manifest/inventory awake race).
    }

    private bool EnsureOriginCaptured()
    {
        var state = GPSRouteStateStore.Instance.State;
        if (state.has_origin)
            return true;

        if (!LocationServiceManager.Instance.HasStableFix)
            return false;

        // When forceStableFix is on, HasStableFix returns true but TryGetFilteredLocation
        // may return (0,0) if no real GPS data arrived. That's fine — lat/lng are only
        // metadata here. All unlock distances use AR world-space (camera position deltas),
        // not haversine, so (0,0) origin never affects gameplay.
        LocationServiceManager.Instance.TryGetFilteredLocation(
            out double lat,
            out double lng,
            out float accuracy);

        state.origin_lat = lat;
        state.origin_lng = lng;
        state.origin_accuracy_m = accuracy;
        state.initialized_at = DateTime.UtcNow.ToString("o");
        state.has_origin = true;
        state.next_sequence_index = Mathf.Max(1, state.next_sequence_index);
        GPSRouteStateStore.Instance.Save();

        SetSegmentStartFromCamera();

        bool forced = LocationServiceManager.Instance.forceStableFix;
        Debug.Log($"[OfflineGPSRouteManager] Route origin captured at {lat:F6}, {lng:F6} " +
                  $"(+/-{accuracy:F1}m){(forced ? " [FORCED — debug mode]" : "")}.");
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

        // Forward pass: advance past any sequences that were explicitly collected.
        // Min-sequence search handles non-contiguous per-soldier routes (with gaps).
        while (true)
        {
            ArtifactData next = null;
            foreach (var a in _routeArtifacts)
            {
                if (a.sequence_index >= state.next_sequence_index
                    && (next == null || a.sequence_index < next.sequence_index))
                    next = a;
            }
            if (next == null || !InventoryManager.Instance.IsCollected(next.id))
                break;

            state.next_sequence_index = next.sequence_index + 1;
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

        // Guard against corrupted persisted state: if a predecessor is uncollected AND not
        // yet passed (seq >= next_sequence_index), this artifact was promoted prematurely.
        // An auto-advanced predecessor has seq < next_sequence_index and must NOT block.
        int currentNextSeq = GPSRouteStateStore.Instance.State.next_sequence_index;
        foreach (var a in _routeArtifacts)
        {
            if (a.sequence_index < activeArtifact.sequence_index
                && !InventoryManager.Instance.IsCollected(a.id)
                && a.sequence_index >= currentNextSeq)
            {
                ArtifactSpawner.Instance?.Despawn(activeArtifact.id);
                DestroyPresentationAnchor(activeArtifact.id);
                var st = GPSRouteStateStore.Instance.State;
                st.active_artifact_id = "";
                st.next_sequence_index = a.sequence_index;
                GPSRouteStateStore.Instance.Save();
                Debug.Log($"[OfflineGPSRouteManager] Stale state: prerequisite {a.id} " +
                          $"(seq={a.sequence_index}) not yet reached (next_seq={currentNextSeq}). " +
                          $"Resetting from {activeArtifact.id}.");
                return;
            }
        }

        if (!ArtifactSpawner.Instance.IsSpawned(activeArtifact.id))
            PresentArtifact(activeArtifact);
    }

    private ArtifactData GetNextRouteArtifact()
    {
        if (_routeArtifacts == null || _routeArtifacts.Count == 0)
            return null;

        // Find the nearest uncollected artifact at or after next_sequence_index.
        // Exact-match fails when the active soldier's route has non-contiguous sequences.
        int nextSequence = NextSequenceIndex;
        ArtifactData best = null;
        foreach (var a in _routeArtifacts)
        {
            if (a.sequence_index >= nextSequence && !InventoryManager.Instance.IsCollected(a.id))
            {
                if (best == null || a.sequence_index < best.sequence_index)
                    best = a;
            }
        }
        return best;
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

        // Wait until ARCore is actively tracking before spawning. During SessionInitializing
        // the camera sits at world origin (0,0,0) and the camera feed is not yet live;
        // spawning here places the artifact at the scene origin rather than in front of
        // the player. Retry every routeCheckInterval until tracking is confirmed.
        if (ARSession.state < ARSessionState.SessionTracking)
        {
            Debug.Log($"[OfflineGPSRouteManager] AR not tracking yet (state={ARSession.state}) — deferring spawn of {artifact.id}.");
            return;
        }

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

        // Per-artifact height override: negative = below camera (more natural for
        // ground-level objects like a bolo knife).
        float heightOffset = spawnHeightOffset + artifact.spawn_height_offset_meters;

        anchorObject.transform.position =
            new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + heightOffset,
                Camera.main.transform.position.z)
            + flatForward * spawnDistance;

        anchorObject.transform.rotation = Quaternion.Euler(
            0f,
            Camera.main.transform.eulerAngles.y + 180f,
            0f);

        // Parent anchor to the AR scene root (XROrigin) via Camera.main's hierarchy root.
        // When ARCore relocalization corrects the coordinate frame it moves XROrigin so all
        // children move with it — the anchor stays locked in physical space instead of
        // drifting as tracking improves. worldPositionStays=true preserves the world
        // position set above. Skip if Camera.main is itself a scene root.
        var arSceneRoot = Camera.main.transform.root;
        if (arSceneRoot != Camera.main.transform)
            anchorObject.transform.SetParent(arSceneRoot, worldPositionStays: true);

        ArtifactSpawner.Instance.Spawn(artifact, anchorObject.transform);
    }

    private void HandleArtifactModelVisible(string artifactId)
    {
        // Only reset the segment start for the currently active artifact. The model was
        // hidden during loading/auto-scale (up to 5s). Without this reset, auto-advance
        // fires WHILE the model is still invisible because _currentSegmentDistance
        // accumulated during that hidden window.
        if (artifactId != ActiveArtifactId) return;

        _hasSegmentStart = false;
        _currentSegmentDistance = 0f;
        Debug.Log($"[OfflineGPSRouteManager] Model visible for {artifactId} — segment start reset.");
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

    public void ResetForTesting()
    {
        if (GPSRouteStateStore.Instance != null)
        {
            var state = GPSRouteStateStore.Instance.State;
            state.has_origin = false;
            state.next_sequence_index = 1;
            state.active_artifact_id = "";
            state.origin_lat = 0;
            state.origin_lng = 0;
            state.origin_accuracy_m = 0;
            state.initialized_at = "";
            GPSRouteStateStore.Instance.Save();
        }

        // Despawn ALL tracked GPS artifacts so ArtifactSpawner._spawnedArtifacts is fully
        // cleared. Without this, stale dictionary entries from a previous session cause
        // IsSpawned to return true for artifacts that should spawn fresh, silently
        // preventing later sequence artifacts from ever appearing.
        foreach (var id in new List<string>(_presentationAnchors.Keys))
            ArtifactSpawner.Instance?.Despawn(id);

        if (!string.IsNullOrEmpty(ActiveArtifactId))
            ArtifactSpawner.Instance?.Despawn(ActiveArtifactId);

        foreach (var anchor in _presentationAnchors.Values)
            if (anchor != null) Destroy(anchor);
        _presentationAnchors.Clear();

        _hasSegmentStart       = false;
        _currentSegmentDistance = 0f;
        _routeLoaded           = false;
        _inventoryCleared      = false;

        Debug.Log("[OfflineGPSRouteManager] Reset for testing — route starts from seq=1.");
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
