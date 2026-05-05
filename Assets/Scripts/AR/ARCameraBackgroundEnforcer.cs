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
    private int  _resetCount;        // number of stuck-resets attempted this session
    private const int MAX_RESETS = 3; // give up after 3 × 30 s = 90 s of stuck time
    private bool _hasAppEverPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraBackgroundEnforcer>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraBackgroundEnforcer",
            typeof(ARCameraBackgroundEnforcer)));
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _hasAppEverPaused = true;
            return;
        }

        // Guard: OnApplicationPause(false) fires on initial Android launch (first
        // Activity focus gain). Resetting _applied then causes a double-toggle of
        // ARCameraBackground which can stall camera hardware on certain devices.
        // Only process genuine app resumes (preceded by a real pause).
        if (!_hasAppEverPaused) return;

        _applied    = false;
        _stuckTimer = 0f;
        _resetCount = 0;
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
            // If the CPU camera feed is already showing the real environment,
            // the session is not truly stuck — ARCore is running and scanning.
            // Skip the reset so we don't interrupt an actively working camera.
            if (ARCameraDisplay.IsShowingFeed)
            {
                _stuckTimer = 0f;
                return;
            }

            _stuckTimer += Time.unscaledDeltaTime;

            // Allow up to MAX_RESETS attempts, one every 30 s.
            // 15 s was too aggressive — ARCore on Xiaomi/Redmi devices needs up to
            // 20-25 s to scan enough feature points to reach SessionTracking outdoors.
            // Resetting at 15 s created an infinite Initializing → reset → Initializing
            // loop that permanently prevented SessionTracking.
            if (_stuckTimer > 30f && _resetCount < MAX_RESETS)
            {
                _stuckTimer = 0f;
                _resetCount++;
                _applied = false;
                Debug.Log($"[ARCameraBackgroundEnforcer] Session stuck (no feed) — reset #{_resetCount}/{MAX_RESETS}.");
                StartCoroutine(ResetARSession());
            }
        }
        else if (ARSession.state == ARSessionState.SessionTracking)
        {
            _stuckTimer  = 0f;
            _resetCount  = 0;
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

        // Step 6 — force re-subscription once per setup cycle.
        // The disable→enable cycle on ARCameraBackground does two things:
        //   1. Restores the frameReceived subscription lost after ARSession cycles.
        //   2. Triggers ARCameraBackground.OnEnable() at SessionInitializing, which
        //      on certain Android devices is what causes ARCameraManager to actually
        //      open the camera hardware. Skipping this toggle prevents the camera
        //      from ever starting on those devices.
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

        // Step 7 removed — ARCameraDisplay owns the background enabled state.
        // When the CPU feed is active, ARCameraDisplay disables ARCameraBackground
        // every frame to prevent the white OES blit from overriding the CPU feed.
        // A keep-alive here would fight that and re-introduce the white background.
    }
}
