using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Ensures the AR camera background renders the device camera feed.
///
/// Root causes addressed:
/// 1. alpha=0 backgroundColor composites as white on Android - use Color.black.
/// 2. ARCameraBackground loses frameReceived subscription after ARSession
///    disable/enable cycles - force a disable->enable cycle to re-subscribe.
/// 3. ARSession can get stuck at SessionInitializing outdoors - auto-reset
///    after 15 seconds to restart the AR initialization sequence.
/// 4. ARCameraManager can be unexpectedly disabled - ensure it stays enabled.
/// </summary>
[DefaultExecutionOrder(-120)]
public class ARCameraBackgroundEnforcer : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private ARCameraBackground _background;
    private Camera _arCamera;

    private bool _applied;
    private float _stuckTimer;
    private int _resetCount;        // number of stuck-resets attempted this session
    private const int MAX_RESETS = 3; // give up after 3 x 30 s = 90 s of stuck time

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
            _resetCount = 0;
        }
    }

    private void Update()
    {
        MonitorSessionStuck();
        EnsureBackground();
    }

    private void MonitorSessionStuck()
    {
        if (ARSession.state == ARSessionState.SessionInitializing)
        {
            if (ARCameraDisplay.IsShowingFeed)
            {
                _stuckTimer = 0f;
                return;
            }

            _stuckTimer += Time.unscaledDeltaTime;

            // Allow up to MAX_RESETS attempts, one every 30 s.
            if (_stuckTimer > 30f && _resetCount < MAX_RESETS)
            {
                _stuckTimer = 0f;
                _resetCount++;
                _applied = false;
                Debug.Log($"[ARCameraBackgroundEnforcer] Session stuck (no feed) - reset #{_resetCount}/{MAX_RESETS}.");
                StartCoroutine(ResetARSession());
            }
        }
        else if (ARSession.state == ARSessionState.SessionTracking)
        {
            _stuckTimer = 0f;
            _resetCount = 0;
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

    private void EnsureBackground()
    {
        if (_cameraManager == null)
        {
            _cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            if (_cameraManager == null) return;
            _applied = false;
        }

        if (!_cameraManager.enabled)
        {
            _cameraManager.enabled = true;
            Debug.Log("[ARCameraBackgroundEnforcer] Enabled ARCameraManager.");
            _applied = false;
        }

        if (_arCamera == null)
        {
            _arCamera = _cameraManager.GetComponent<Camera>();
            if (_arCamera == null)
                _arCamera = Camera.main;
            if (_arCamera == null)
                return;
            _applied = false;
        }

        _arCamera.clearFlags = CameraClearFlags.SolidColor;
        _arCamera.backgroundColor = Color.black;

        if (_background == null)
        {
            _background = _arCamera.GetComponent<ARCameraBackground>();
            if (_background == null)
                _background = _arCamera.gameObject.AddComponent<ARCameraBackground>();
            _applied = false;
        }

        if (!_applied)
        {
            if (ARSession.state < ARSessionState.SessionInitializing)
                return;

            _applied = true;
            _background.enabled = false;
            _background.enabled = true;
            Debug.Log($"[ARCameraBackgroundEnforcer] Applied. Camera={_arCamera.name}");
            return;
        }

        // ARCameraDisplay owns the background enabled state after the first
        // CPU frame. Keeping it alive here would re-introduce the white OES bug.
    }
}
