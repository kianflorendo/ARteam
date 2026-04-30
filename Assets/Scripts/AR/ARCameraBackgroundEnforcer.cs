using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Ensures the AR camera background renders the device camera feed.
///
/// Polling-based (Update) instead of event-driven coroutines to avoid
/// timing races on Android where stateChanged events can fire before
/// the camera pipeline is ready.
///
/// Root causes addressed:
/// 1. alpha=0 backgroundColor composites as white on Android — always use Color.black.
/// 2. ARCameraBackground loses frameReceived subscription after ARSession reset or
///    app resume — force a disable→enable cycle to re-subscribe.
/// 3. Silent disable during session interruptions — keep-alive re-enables each frame.
/// </summary>
[DefaultExecutionOrder(-120)]
public class ARCameraBackgroundEnforcer : MonoBehaviour
{
    private ARCameraBackground _background;
    private Camera _arCamera;
    private bool _appliedForCurrentSetup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraBackgroundEnforcer>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraBackgroundEnforcer",
            typeof(ARCameraBackgroundEnforcer)));
    }

    private void OnApplicationPause(bool paused)
    {
        // On Android, foregrounding after background resets the camera pipeline.
        // Clear the applied flag so the next Update forces a re-subscription toggle.
        if (!paused)
            _appliedForCurrentSetup = false;
    }

    private void Update()
    {
        EnsureBackground();
    }

    private void EnsureBackground()
    {
        // Step 1 — find the AR camera.
        if (_arCamera == null)
        {
            var manager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            _arCamera = manager != null ? manager.GetComponent<Camera>() : Camera.main;
            if (_arCamera == null)
                return;

            _appliedForCurrentSetup = false;
        }

        // Step 2 — always keep clear color opaque black.
        // alpha=0 (Color.clear) composites as white on Android system window.
        _arCamera.clearFlags = CameraClearFlags.SolidColor;
        _arCamera.backgroundColor = Color.black;

        // Step 3 — ensure ARCameraBackground component exists.
        if (_background == null)
        {
            _background = _arCamera.GetComponent<ARCameraBackground>();
            if (_background == null)
                _background = _arCamera.gameObject.AddComponent<ARCameraBackground>();

            _appliedForCurrentSetup = false;
        }

        // Step 4 — force re-subscription once per setup (or after resume/re-find).
        // The disable→enable cycle forces ARCameraBackground.OnEnable() which
        // re-subscribes to ARCameraManager.frameReceived.
        // Wait until ARCore is at least initializing before toggling.
        if (!_appliedForCurrentSetup)
        {
            if (ARSession.state < ARSessionState.SessionInitializing)
                return;

            _appliedForCurrentSetup = true;
            _background.enabled = false;
            _background.enabled = true;
            Debug.Log($"[ARCameraBackgroundEnforcer] Applied. " +
                      $"Camera={_arCamera.name} Session={ARSession.state}");
            return;
        }

        // Step 5 — keep-alive: re-enable if silently disabled during session.
        if (!_background.enabled)
        {
            _background.enabled = true;
            Debug.Log("[ARCameraBackgroundEnforcer] Keep-alive: re-enabled background.");
        }
    }
}
