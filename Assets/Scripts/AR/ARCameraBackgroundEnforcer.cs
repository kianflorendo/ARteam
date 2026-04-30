using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Continuously ensures the AR camera renders the device camera feed.
///
/// Root causes addressed:
/// 1. transparent backgroundColor (alpha=0) on Android composites as white against the
///    system window background. Fix: always use opaque black as the camera clear color.
/// 2. ARCameraBackground loses its ARCameraManager.frameReceived subscription whenever
///    ARSession is disabled/re-enabled (e.g. by ARPermissionRequester, or app resume).
///    Fix: force a disable→enable cycle on ARCameraBackground on every session init/resume.
/// 3. Single-shot fixes break when the app backgrounds and foregrounds.
///    Fix: re-apply on every state transition to SessionInitializing or above, and add a
///    keep-alive check in Update to revive a silently-disabled ARCameraBackground.
/// </summary>
[DefaultExecutionOrder(-120)]
public class ARCameraBackgroundEnforcer : MonoBehaviour
{
    private Camera _camera;
    private ARCameraBackground _background;
    private bool _applyPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraBackgroundEnforcer>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraBackgroundEnforcer",
            typeof(ARCameraBackgroundEnforcer)));
    }

    private void OnEnable()  => ARSession.stateChanged += OnARSessionStateChanged;
    private void OnDisable() => ARSession.stateChanged -= OnARSessionStateChanged;

    private void Start()
    {
        // Bootstrap: session may already be past initialization when we first enable.
        if (ARSession.state >= ARSessionState.SessionInitializing)
            ScheduleApply();
    }

    private void OnApplicationPause(bool paused)
    {
        // On Android, coming back from background resets the camera pipeline.
        if (!paused)
            ScheduleApply();
    }

    private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        if (args.state >= ARSessionState.SessionInitializing)
            ScheduleApply();
    }

    private void Update()
    {
        // Keep-alive: if ARCameraBackground gets silently disabled while the session
        // is running (can happen after certain OS interruptions), immediately revive it.
        if (_background != null &&
            !_background.enabled &&
            ARSession.state >= ARSessionState.SessionTracking)
        {
            _background.enabled = true;
            Debug.Log("[ARCameraBackgroundEnforcer] Keep-alive: re-enabled ARCameraBackground.");
        }
    }

    private void ScheduleApply()
    {
        if (_applyPending) return;
        _applyPending = true;
        StartCoroutine(ApplyAfterDelay());
    }

    private IEnumerator ApplyAfterDelay()
    {
        // Wait two frames for AR subsystems to fully initialise after the session enables.
        yield return null;
        yield return null;
        _applyPending = false;
        Apply();
    }

    private void Apply()
    {
        var manager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
        _camera = manager != null ? manager.GetComponent<Camera>() : Camera.main;

        if (_camera == null)
        {
            Debug.LogWarning("[ARCameraBackgroundEnforcer] Camera not found — will retry on next state change.");
            return;
        }

        // Ensure ARCameraBackground is present.
        _background = _camera.GetComponent<ARCameraBackground>();
        if (_background == null)
            _background = _camera.gameObject.AddComponent<ARCameraBackground>();

        // Force a disable→enable cycle so ARCameraBackground re-subscribes to
        // ARCameraManager.frameReceived after any ARSession disable/enable cycle.
        _background.enabled = false;
        _background.enabled = true;

        // CRITICAL: use opaque black (alpha=1).
        // Transparent pixels (alpha=0) on Android composite against the white system
        // window, producing a white background whenever ARCameraBackground isn't rendering.
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;

        Debug.Log($"[ARCameraBackgroundEnforcer] Applied on '{_camera.name}'. Session={ARSession.state}");
    }
}
