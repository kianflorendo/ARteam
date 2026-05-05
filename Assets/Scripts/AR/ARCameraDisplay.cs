// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// CPU camera display for all Android devices.
//
// Why CPU instead of GPU (ARCameraBackground)?
//   On this device's GPU driver, ARCameraBackground.enabled = true
//   but it renders a solid white OES texture instead of the real
//   camera feed. Its blit also conflicts with the ScreenSpaceCamera
//   canvas in Unity 6 URP — even when the canvas is at 15 m depth,
//   ARCameraBackground's late-pipeline blit overwrites background
//   pixels with white whenever a 3D artifact is in the scene.
//   Fix: disable ARCameraBackground entirely when CPU feed is active.
//   ARCameraManager.frameReceived fires independently — AR tracking
//   is unaffected by ARCameraBackground.enabled state.
//
// Orientation:
//   Android delivers CPU images in landscape orientation regardless
//   of how the phone is held. The RawImage is rotated -90° (CW) so
//   the landscape texture fills the portrait screen correctly.
//   XRCpuImage.Transformation.MirrorY corrects the OpenGL bottom-up
//   vertical flip before the rotation is applied.
//
// Canvas modes:
//   ScreenSpaceOverlay  (SessionInitializing)
//     No Camera ref needed. Renders on top of the black clear color.
//     No GPS artifacts exist yet — no depth ordering needed.
//
//   ScreenSpaceCamera at 15 m  (SessionTracking)
//     Depth-tested in the 3D scene. GPS artifacts at ~1 m are closer
//     → depth test puts them IN FRONT of this 15 m canvas.
//     ARCameraBackground is disabled so no competing OES blit exists.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DefaultExecutionOrder(-110)]
public class ARCameraDisplay : MonoBehaviour
{
    private const float PLANE_DISTANCE = 15f;

    private const float NO_FRAME_RETRY_SECONDS = 5f;

    private ARCameraManager    _cameraManager;
    private ARCameraBackground _arBackground;
    private Canvas             _canvas;
    private RawImage           _displayImage;
    private Texture2D          _cameraTexture;
    private bool               _isShowing;
    private bool               _hasTexture;
    private int                _frameSkip;
    private bool               _inSceneMode;
    private bool               _hasAppEverPaused;
    private float              _noFrameTimer;

    // ── Singleton bootstrap ──────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraDisplay>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraDisplay", typeof(ARCameraDisplay)));
    }

    // ── Lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        var canvasGo = new GameObject("[AR Camera Fallback Canvas]");
        DontDestroyOnLoad(canvasGo);

        _canvas              = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -100;

        var rawGo = new GameObject("CameraRaw");
        rawGo.transform.SetParent(canvasGo.transform, false);

        _displayImage       = rawGo.AddComponent<RawImage>();
        _displayImage.color = Color.clear;

        // ── Rotation correction for Android portrait ──────────────────────
        // Camera delivers landscape images on Android. Rotate the RawImage
        // -90° (CW) so the landscape texture fills the portrait screen.
        // sizeDelta is (Screen.height, Screen.width) — swapped — so that
        // after the -90° rotation the image visually spans (width, height).
        var rt = rawGo.GetComponent<RectTransform>();
        rt.localRotation  = Quaternion.Euler(0f, 0f, -90f);
        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta      = new Vector2(Screen.height, Screen.width);

        _displayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ── 1. Cache ARCameraManager (find once) ──────────────────────────
        if (_cameraManager == null)
        {
            var mgr = FindAnyObjectByType<ARCameraManager>();
            if (mgr != null)
            {
                _cameraManager = mgr;
                _cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        // ── 2. Suppress ARCameraBackground once the CPU feed is active ────
        // Only suppress AFTER the first CPU frame is decoded (_hasTexture).
        // Before that, ARCameraBackground must stay enabled — on this device
        // its OnEnable() is what triggers ARCameraManager to open the camera
        // hardware and start delivering frames. Suppressing too early means
        // TryAcquireLatestCpuImage() never gets a frame → black screen.
        // Once _hasTexture is true the camera is definitely open and we can
        // safely disable ARCameraBackground to stop the white OES blit.
        if (_isShowing && _hasTexture) SuppressARCameraBackground();

        // ── 3. Canvas mode: ScreenSpaceOverlay → ScreenSpaceCamera ────────
        bool shouldBeSceneMode = ARSession.state >= ARSessionState.SessionTracking;

        if (shouldBeSceneMode && !_inSceneMode)
        {
            var main = Camera.main;
            if (main != null)
            {
                _canvas.worldCamera   = main;
                _canvas.renderMode    = RenderMode.ScreenSpaceCamera;
                _canvas.planeDistance = PLANE_DISTANCE;
                _inSceneMode          = true;
                Debug.Log("[ARCameraDisplay] Switched to ScreenSpaceCamera (tracking).");
            }
        }
        else if (!shouldBeSceneMode && _inSceneMode)
        {
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _inSceneMode       = false;
            Debug.Log("[ARCameraDisplay] Reverted to ScreenSpaceOverlay (tracking lost).");
        }

        // ── 4. Show / hide ────────────────────────────────────────────────
        bool needsShow = ARSession.state >= ARSessionState.SessionInitializing;

        if (_displayImage != null && needsShow != _isShowing)
        {
            _isShowing = needsShow;
            _displayImage.gameObject.SetActive(_isShowing);

            if (_isShowing)
            {
                if (!_hasTexture) TryDecodeCpuImage();
            }
            else
            {
                // Session ended — let ARCameraBackground manage itself again.
                RestoreARCameraBackground();
                _hasTexture         = false;
                _displayImage.color = Color.clear;
            }

            Debug.Log(_isShowing
                ? "[ARCameraDisplay] Showing CPU camera display."
                : "[ARCameraDisplay] Hidden — session pre-initialising.");
        }

        // ── 5. Decode CPU frames ──────────────────────────────────────────
        // Aggressive every frame until first texture; every other frame after.
        if (_isShowing && _cameraManager != null)
        {
            if (!_hasTexture)
            {
                TryDecodeCpuImage();
            }
            else
            {
                _frameSkip++;
                if (_frameSkip % 2 == 0)
                    TryDecodeCpuImage();
            }
        }

        // ── 6. No-frame timeout self-heal ─────────────────────────────────
        // If we have been showing for NO_FRAME_RETRY_SECONDS without a decoded
        // frame, the camera hardware may be stuck. Force a disable→enable cycle
        // on ARCameraBackground to reopen it (same mechanism as initial setup).
        if (_isShowing && !_hasTexture)
        {
            _noFrameTimer += Time.unscaledDeltaTime;
            if (_noFrameTimer >= NO_FRAME_RETRY_SECONDS)
            {
                _noFrameTimer = 0f;
                RetryOpenCamera();
            }
        }
        else
        {
            _noFrameTimer = 0f;
        }
    }

    private void RetryOpenCamera()
    {
        if (_arBackground == null)
            _arBackground = Camera.main?.GetComponent<ARCameraBackground>();
        if (_arBackground == null) return;

        _arBackground.enabled = false;
        _arBackground.enabled = true;
        Debug.Log("[ARCameraDisplay] No frame after timeout — re-triggered camera background toggle.");
    }

    // ── ARCameraBackground suppression ───────────────────────────────────

    private void SuppressARCameraBackground()
    {
        if (_arBackground == null)
            _arBackground = Camera.main?.GetComponent<ARCameraBackground>();

        if (_arBackground != null && _arBackground.enabled)
        {
            _arBackground.enabled = false;
            // No log — this runs every frame.
        }
    }

    private void RestoreARCameraBackground()
    {
        if (_arBackground != null && !_arBackground.enabled)
        {
            _arBackground.enabled = true;
            Debug.Log("[ARCameraDisplay] Restored ARCameraBackground (session ended).");
        }
    }

    // ── Camera frame handling ────────────────────────────────────────────

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isShowing || _cameraManager == null) return;
        TryDecodeCpuImage();
    }

    private void TryDecodeCpuImage()
    {
        if (_cameraManager == null) return;
        if (!_cameraManager.TryAcquireLatestCpuImage(out var image)) return;

        using (image)
        {
            try
            {
                // 1/2 resolution — good quality with acceptable CPU cost.
                // (1/4 was fast but too blurry at 3× upscale on portrait screen)
                int w = Mathf.Max(1, image.width  / 2);
                int h = Mathf.Max(1, image.height / 2);

                if (_cameraTexture == null
                    || _cameraTexture.width  != w
                    || _cameraTexture.height != h)
                {
                    if (_cameraTexture != null) Destroy(_cameraTexture);
                    _cameraTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    if (_displayImage != null)
                        _displayImage.texture = _cameraTexture;
                }

                var convParams = new XRCpuImage.ConversionParams
                {
                    inputRect        = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(w, h),
                    outputFormat     = TextureFormat.RGBA32,
                    // MirrorY: converts Android's OpenGL bottom-up convention to
                    // Unity's top-down convention. The RawImage rotation (-90°)
                    // handles the portrait/landscape orientation correction.
                    transformation   = XRCpuImage.Transformation.MirrorY,
                };

                var rawData = _cameraTexture.GetRawTextureData<byte>();
                image.Convert(convParams, rawData);
                _cameraTexture.Apply();

                if (!_hasTexture && _displayImage != null)
                {
                    _hasTexture          = true;
                    _displayImage.color  = Color.white;
                    Debug.Log("[ARCameraDisplay] Real environment visible.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ARCameraDisplay] Frame decode failed: {e.Message}");
            }
        }
    }

    // ── App lifecycle ────────────────────────────────────────────────────

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _hasAppEverPaused = true;
            return;
        }

        // On Android, OnApplicationPause(false) fires on INITIAL LAUNCH (first
        // Activity focus gain) as well as on genuine app resume. Resetting state
        // on initial launch causes a spurious ARCameraBackground double-toggle
        // that can permanently stall camera hardware on some devices.
        // Only process the resume path if the app has genuinely been paused before.
        if (!_hasAppEverPaused) return;

        _hasTexture   = false;
        _arBackground = null;
        _noFrameTimer = 0f;
        Debug.Log("[ARCameraDisplay] App resumed — camera pipeline reset.");
    }

    // ── Cleanup ──────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnCameraFrameReceived;

        if (_cameraTexture != null)
            Destroy(_cameraTexture);
    }
}
