using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Ensures the AR camera background renders the device camera feed.
///
/// Root causes addressed:
/// 1. alpha=0 backgroundColor composites as white on Android — use Color.black.
/// 2. ARCameraBackground loses frameReceived subscription after ARSession
///    disable/enable cycles — force a disable→enable cycle to re-subscribe.
/// 3. ARSession can get stuck at SessionInitializing outdoors — auto-reset
///    after 15 seconds to restart the AR initialization sequence.
/// 4. ARCameraManager can be unexpectedly disabled — ensure it stays enabled.
/// </summary>
[DefaultExecutionOrder(-120)]
public class ARCameraBackgroundEnforcer : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private ARCameraBackground _background;
    private Camera _arCamera;

    private bool _applied;
    private float _stuckTimer;
    private bool _resetAttempted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraBackgroundEnforcer>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraBackgroundEnforcer",
            typeof(ARCameraBackgroundEnforcer)));
    }

    private void OnApplicationPause(bool paused)
    {
        // On Android, coming back from background resets the camera pipeline.
        if (!paused)
        {
            _applied = false;
            _stuckTimer = 0f;
            _resetAttempted = false;
        }
    }

    private void Update()
    {
        MonitorSessionStuck();
        EnsureBackground();
    }

    // ── Session stuck detection ───────────────────────────────────────────
    // If the AR session stays at SessionInitializing for more than 15 seconds,
    // force a disable/enable cycle on ARSession. This restarts ARCore's
    // initialization sequence and often resolves the stuck state outdoors.

    private void MonitorSessionStuck()
    {
        if (ARSession.state == ARSessionState.SessionInitializing)
        {
            _stuckTimer += Time.unscaledDeltaTime;

            if (!_resetAttempted && _stuckTimer > 15f)
            {
                _resetAttempted = true;
                Debug.Log("[ARCameraBackgroundEnforcer] Session stuck at Initializing for 15s — forcing reset.");
                _applied = false;
                StartCoroutine(ResetARSession());
            }
        }
        else if (ARSession.state == ARSessionState.SessionTracking)
        {
            _stuckTimer = 0f;
            _resetAttempted = false;
        }
    }

    private IEnumerator ResetARSession()
    {
        var arSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
        if (arSession == null) yield break;

        arSession.enabled = false;
        yield return new WaitForSecondsRealtime(1f);
        arSession.enabled = true;
        Debug.Log("[ARCameraBackgroundEnforcer] ARSession re-enabled after stuck-state reset.");
    }

    // ── Camera background setup ───────────────────────────────────────────

    private void EnsureBackground()
    {
        // Step 1 — find ARCameraManager.
        if (_cameraManager == null)
        {
            _cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            if (_cameraManager == null) return;
            _applied = false;
        }

        // Step 2 — ensure ARCameraManager is enabled.
        if (!_cameraManager.enabled)
        {
            _cameraManager.enabled = true;
            Debug.Log("[ARCameraBackgroundEnforcer] Enabled ARCameraManager.");
            _applied = false;
        }

        // Step 3 — get the AR camera.
        if (_arCamera == null)
        {
            _arCamera = _cameraManager.GetComponent<Camera>();
            if (_arCamera == null)
                _arCamera = Camera.main;
            if (_arCamera == null)
                return;
            _applied = false;
        }

        // Step 4 — always use opaque black clear color.
        _arCamera.clearFlags = CameraClearFlags.SolidColor;
        _arCamera.backgroundColor = Color.black;

        // Step 5 — ensure ARCameraBackground exists on the AR camera.
        if (_background == null)
        {
            _background = _arCamera.GetComponent<ARCameraBackground>();
            if (_background == null)
                _background = _arCamera.gameObject.AddComponent<ARCameraBackground>();
            _applied = false;
        }

        // Step 6 — force re-subscription once per setup.
        // Wait until ARCore is at least initializing before toggling.
        if (!_applied)
        {
            if (ARSession.state < ARSessionState.SessionInitializing)
                return;

            _applied = true;
            _background.enabled = false;
            _background.enabled = true;
            Debug.Log($"[ARCameraBackgroundEnforcer] Applied. " +
                      $"Camera={_arCamera.name} Session={ARSession.state}");
            return;
        }

        // Step 7 — keep-alive: re-enable if silently disabled.
        if (!_background.enabled)
        {
            _background.enabled = true;
            Debug.Log("[ARCameraBackgroundEnforcer] Keep-alive: re-enabled background.");
        }
    }
}
