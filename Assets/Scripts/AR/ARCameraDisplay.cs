// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// CPU-path camera display that shows the real environment on ALL
// Android devices, including those where ARCameraBackground's GPU
// OES blit renders a white/grey rectangle (known AR Foundation + URP
// issue on certain Xiaomi/Redmi GPU drivers — ARCameraBackground
// can be enabled AND rendering while still outputting white garbage;
// "enabled == true" does NOT mean "outputting real camera feed").
//
// This class always shows the CPU-decoded camera image. It replaces
// whatever ARCameraBackground draws with real camera pixels.
//
// Canvas mode transitions:
//   ScreenSpaceOverlay  (SessionInitializing)
//     — renders on top of everything; no Camera reference needed.
//     — covers the black AR-init clear color while ARCore starts up.
//     — no GPS artifacts exist yet, so depth-sorting is irrelevant.
//
//   ScreenSpaceCamera at planeDistance=15 m  (SessionTracking)
//     — depth-tested inside the 3D scene.
//     — GPS artifacts spawned at ~1 m are CLOSER → appear IN FRONT
//       of this 15 m canvas (correct depth ordering).
//     — covers the white GPU-OES texture from ARCameraBackground.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DefaultExecutionOrder(-110)]
public class ARCameraDisplay : MonoBehaviour
{
    // Canvas depth in ScreenSpaceCamera mode. Must be inside the AR
    // camera frustum [nearClip≈0.1, farClip=20]. GPS artifacts spawn
    // at ~1 m, so any value >1 m keeps them depth-sorted in front.
    private const float PLANE_DISTANCE = 15f;

    private ARCameraManager _cameraManager;
    private Canvas          _canvas;
    private RawImage        _displayImage;
    private Texture2D       _cameraTexture;
    private bool            _isShowing;
    private bool            _hasTexture;   // true once first CPU frame decoded and shown
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
        _canvas.sortingOrder = -100; // behind main UI (sortingOrder 100)

        var rawGo = new GameObject("CameraRaw");
        rawGo.transform.SetParent(canvasGo.transform, false);

        _displayImage       = rawGo.AddComponent<RawImage>();
        _displayImage.color = Color.clear; // transparent until first frame decoded

        var rect = rawGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _displayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ── 1. Maintain ARCameraManager subscription (cached after first find) ──
        if (_cameraManager == null)
        {
            var mgr = FindAnyObjectByType<ARCameraManager>();
            if (mgr != null)
            {
                _cameraManager = mgr;
                _cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        // ── 2. Switch canvas mode at the Initializing → Tracking boundary ───
        //
        // ScreenSpaceOverlay: in front of 3D scene, no Camera ref needed.
        //   Correct at Initializing — no GPS artifacts yet to depth-sort against.
        //
        // ScreenSpaceCamera at 15 m: depth-tested in 3D scene.
        //   GPS artifacts at ~1 m are closer → depth-test puts them IN FRONT.
        //   Covers the white GPU-OES texture from ARCameraBackground.
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
                if (!_hasTexture) TryDecodeCpuImage();
            }
            else
            {
                _hasTexture         = false;
                _displayImage.color = Color.clear;
            }

            Debug.Log(_isShowing
                ? "[ARCameraDisplay] Showing CPU camera display."
                : "[ARCameraDisplay] Hidden — session pre-initialising.");
        }

        // ── 4. Decode CPU camera frames ────────────────────────────────────
        // Aggressive every frame until first texture; throttled every other frame after.
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
                // Downsample to 1/4 resolution for performance.
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

                if (!_hasTexture && _displayImage != null)
                {
                    _hasTexture          = true;
                    _displayImage.color  = Color.white; // reveal the real camera feed
                    Debug.Log("[ARCameraDisplay] Real environment visible — CPU feed active.");
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
