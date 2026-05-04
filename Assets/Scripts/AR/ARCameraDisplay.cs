// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// CPU-path camera fallback. Activates ONLY when the GPU path
// (ARCameraBackground) is not handling the display.
//
// With URP IntermediateTextureMode=Auto (correct setting),
// ARCameraBackground renders the OES camera feed directly and
// this canvas stays Color.clear (transparent) — zero overhead.
//
// With IntermediateTextureMode=Always (broken setting), the GPU
// OES blit fails. In that case this class decodes CPU images and
// shows them via ScreenSpaceCamera at 15 m so that GPS artifacts
// spawned at ~1 m remain visually in front of the background.
//
// Canvas mode transitions:
//   ScreenSpaceOverlay  (SessionInitializing)
//     — renders on top of the black AR-init clear color without
//       needing a Camera reference.
//   ScreenSpaceCamera at planeDistance=15 m  (SessionTracking)
//     — depth-tested so GPS artifacts (~1 m) stay in front.
//     — only matters when CPU feed is showing (Color.white).
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

    private ARCameraManager _cameraManager;
    private Canvas          _canvas;
    private RawImage        _displayImage;
    private Texture2D       _cameraTexture;
    private bool            _isShowing;
    private bool            _hasCheckedGpu;   // have we determined if GPU path is active?
    private bool            _gpuPathActive;   // true = ARCameraBackground is handling display
    private int             _frameSkip;
    private bool            _inSceneMode;

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

        var rect = rawGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _displayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ── 1. Maintain ARCameraManager subscription (cached) ─────────────
        // FindAnyObjectByType only runs until the manager is found once.
        if (_cameraManager == null)
        {
            var mgr = FindAnyObjectByType<ARCameraManager>();
            if (mgr != null)
            {
                _cameraManager = mgr;
                _cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        // ── 2. Switch canvas mode at the Initializing→Tracking boundary ───
        //
        // ScreenSpaceOverlay  → always works, no Camera ref needed
        // ScreenSpaceCamera   → depth-tested; GPS artifacts at ~1 m appear
        //                       in front of this 15 m background canvas.
        //                       Only matters when CPU feed is showing (Color.white).
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

        // ── 3. Show / hide based on session state ─────────────────────────
        bool needsShow = ARSession.state >= ARSessionState.SessionInitializing;

        if (_displayImage != null && needsShow != _isShowing)
        {
            _isShowing = needsShow;
            _displayImage.gameObject.SetActive(_isShowing);

            if (_isShowing)
            {
                if (!_hasCheckedGpu)
                    TryDecodeCpuImage();
            }
            else
            {
                _hasCheckedGpu      = false;
                _gpuPathActive      = false;
                _displayImage.color = Color.clear;
            }

            Debug.Log(_isShowing
                ? "[ARCameraDisplay] Showing (may be transparent if GPU path active)."
                : "[ARCameraDisplay] Hidden — session pre-initialising.");
        }

        // ── 4. Decode CPU camera frames ────────────────────────────────────
        // Aggressive decode every frame until we know whether GPU path is active.
        // After that, only keep decoding if GPU path is NOT active (CPU fallback needed).
        if (_isShowing && _cameraManager != null)
        {
            if (!_hasCheckedGpu)
            {
                TryDecodeCpuImage();
            }
            else if (!_gpuPathActive)
            {
                _frameSkip++;
                if (_frameSkip % 2 == 0)
                    TryDecodeCpuImage();
            }
            // If _gpuPathActive: no decoding needed — GPU path handles the display.
        }
    }

    // ── Camera frame handling ────────────────────────────────────────────

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isShowing || _cameraManager == null) return;
        if (!_hasCheckedGpu) TryDecodeCpuImage();
    }

    private void TryDecodeCpuImage()
    {
        if (_cameraManager == null) return;
        if (!_cameraManager.TryAcquireLatestCpuImage(out var image)) return;

        using (image)
        {
            try
            {
                int w = Mathf.Max(1, image.width  / 4);
                int h = Mathf.Max(1, image.height / 4);

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
                    transformation   = XRCpuImage.Transformation.MirrorY,
                };

                var rawData = _cameraTexture.GetRawTextureData<byte>();
                image.Convert(convParams, rawData);
                _cameraTexture.Apply();

                if (!_hasCheckedGpu && _displayImage != null)
                {
                    _hasCheckedGpu = true;

                    // With IntermediateTextureMode=Auto (correct URP setting),
                    // ARCameraBackground handles the GPU OES feed. Stay transparent
                    // so the GPU feed shows through — no need for the CPU path.
                    // Only activate CPU feed when GPU path is confirmed inactive.
                    var bg = Camera.main?.GetComponent<ARCameraBackground>();
                    _gpuPathActive = bg != null && bg.enabled;

                    if (_gpuPathActive)
                    {
                        Debug.Log("[ARCameraDisplay] GPU path active (ARCameraBackground enabled) — CPU canvas stays transparent.");
                    }
                    else
                    {
                        _displayImage.color = Color.white;
                        Debug.Log("[ARCameraDisplay] GPU path inactive — CPU fallback activated.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ARCameraDisplay] Frame conversion failed: {e.Message}");
            }
        }
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
