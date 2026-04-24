using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Ensures the AR camera renders the device camera feed.
///
/// Root causes addressed:
/// 1. transparent backgroundColor (alpha=0) composited over Android's white window = white screen.
///    Fix: always use opaque black as the camera clear color.
/// 2. ARCameraBackground set up BEFORE ARSession is re-enabled by ARPermissionRequester.
///    The component loses its frame subscription during the disable/re-enable cycle.
///    Fix: force a disable→enable cycle on ARCameraBackground once the session is tracking.
/// </summary>
[DefaultExecutionOrder(-120)]
public class ARCameraBackgroundEnforcer : MonoBehaviour
{
    private bool _applied = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraBackgroundEnforcer>() != null)
            return;

        var go = new GameObject("[AUTO] ARCameraBackgroundEnforcer");
        go.AddComponent<ARCameraBackgroundEnforcer>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        ARSession.stateChanged += OnARSessionStateChanged;
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnARSessionStateChanged;
    }

    private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        if (args.state >= ARSessionState.SessionInitializing && !_applied)
            StartCoroutine(ApplyAfterDelay());
    }

    private void Update()
    {
        // Fallback: catch the case where stateChanged fired before we subscribed.
        if (!_applied && ARSession.state >= ARSessionState.SessionInitializing)
            StartCoroutine(ApplyAfterDelay());
    }

    private IEnumerator ApplyAfterDelay()
    {
        // Wait two frames for AR subsystems to fully start after the session enables.
        yield return null;
        yield return null;
        ApplyFix();
    }

    private void ApplyFix()
    {
        if (_applied) return;

        var cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
        Camera targetCamera = cameraManager != null
            ? cameraManager.GetComponent<Camera>()
            : Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("[ARCameraBackgroundEnforcer] Camera not found — will retry.");
            return;
        }

        // Ensure ARCameraBackground is present.
        var background = targetCamera.GetComponent<ARCameraBackground>();
        if (background == null)
            background = targetCamera.gameObject.AddComponent<ARCameraBackground>();

        // Force a disable→enable cycle so ARCameraBackground re-subscribes to
        // ARCameraManager.frameReceived after the ARSession was enabled.
        background.enabled = false;
        background.enabled = true;

        // CRITICAL: use opaque black (alpha=1), NOT transparent (alpha=0).
        // Transparent pixels on Android composite against the white system window,
        // producing a white background whenever ARCameraBackground isn't rendering.
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;

        _applied = true;

        if (cameraManager != null)
            Debug.Log($"[ARCameraBackgroundEnforcer] Applied on '{targetCamera.name}'. " +
                      $"Session: {ARSession.state}");
        else
            Debug.LogWarning("[ARCameraBackgroundEnforcer] ARCameraManager not found — " +
                             "background may not render.");
    }
}
