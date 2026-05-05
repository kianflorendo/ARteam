// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt - Terra App
//
// GPU-first camera display.
//
// The real AR camera background is the primary background layer.
// The CPU RawImage is only a fallback if the AR camera background
// is actually unavailable on a device.
//
// Canvas modes:
//   ScreenSpaceOverlay  (SessionInitializing)
//     No camera reference needed. The fallback stays hidden unless
//     the AR camera background is unavailable.
//
//   ScreenSpaceCamera at 15 m  (SessionTracking)
//     Depth-tested in the 3D scene. GPS artifacts at ~1 m stay in
//     front of the background canvas.
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

    public static bool IsShowingFeed { get; private set; }
    public static bool IsCameraManFound { get; private set; }
    public static bool IsBgEnabled { get; private set; }
    public static int DecodeFrameCount { get; private set; }
    public static bool IsSubsystemRunning { get; private set; }

    private ARCameraManager _cameraManager;
    private ARCameraBackground _arBackground;
    private Canvas _canvas;
    private RawImage _displayImage;
    private Texture2D _cameraTexture;
    private bool _isShowing;
    private bool _hasTexture;
    private int _frameSkip;
    private bool _inSceneMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraDisplay>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraDisplay", typeof(ARCameraDisplay)));
    }

    private void Start()
    {
        var canvasGo = new GameObject("[AR Camera Fallback Canvas]");
        DontDestroyOnLoad(canvasGo);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -100;

        var rawGo = new GameObject("CameraRaw");
        rawGo.transform.SetParent(canvasGo.transform, false);

        _displayImage = rawGo.AddComponent<RawImage>();
        _displayImage.color = Color.clear;

        var rt = rawGo.GetComponent<RectTransform>();
        rt.localRotation = Quaternion.Euler(0f, 0f, -90f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(Screen.height, Screen.width);

        _displayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_cameraManager == null)
        {
            var mgr = FindAnyObjectByType<ARCameraManager>();
            if (mgr != null)
            {
                _cameraManager = mgr;
                _cameraManager.requestedFacingDirection = CameraFacingDirection.World;
                _cameraManager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
                _cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        if (_arBackground == null && Camera.main != null)
            _arBackground = Camera.main.GetComponent<ARCameraBackground>();

        IsCameraManFound = _cameraManager != null;
        IsBgEnabled = _arBackground != null && _arBackground.enabled;
        IsSubsystemRunning = _cameraManager != null
            && _cameraManager.subsystem != null
            && _cameraManager.subsystem.running;

        bool shouldBeSceneMode = ARSession.state >= ARSessionState.SessionTracking;

        if (shouldBeSceneMode && !_inSceneMode)
        {
            var main = Camera.main;
            if (main != null)
            {
                _canvas.worldCamera = main;
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.planeDistance = PLANE_DISTANCE;
                _inSceneMode = true;
                Debug.Log("[ARCameraDisplay] Switched to ScreenSpaceCamera (tracking).");
            }
        }
        else if (!shouldBeSceneMode && _inSceneMode)
        {
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _inSceneMode = false;
            Debug.Log("[ARCameraDisplay] Reverted to ScreenSpaceOverlay (tracking lost).");
        }

        bool shouldShow = ARSession.state >= ARSessionState.SessionInitializing;
        bool useCpuFallback = shouldShow && !IsBgEnabled;

        if (_displayImage != null && shouldShow != _isShowing)
        {
            _isShowing = shouldShow;
            _displayImage.gameObject.SetActive(useCpuFallback);

            if (useCpuFallback && !_hasTexture)
                TryDecodeCpuImage();

            if (!useCpuFallback)
            {
                _hasTexture = false;
                _displayImage.color = Color.clear;
            }

            Debug.Log(_isShowing
                ? (useCpuFallback
                    ? "[ARCameraDisplay] Showing CPU fallback display."
                    : "[ARCameraDisplay] Showing AR camera background.")
                : "[ARCameraDisplay] Hidden - session pre-initialising.");
        }

        useCpuFallback = _isShowing && !IsBgEnabled;
        if (_displayImage != null && _displayImage.gameObject.activeSelf != useCpuFallback)
            _displayImage.gameObject.SetActive(useCpuFallback);

        IsShowingFeed = _isShowing && (IsBgEnabled || (_hasTexture && useCpuFallback));

        if (_isShowing && _cameraManager != null && useCpuFallback)
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

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isShowing || _cameraManager == null || IsBgEnabled) return;
        TryDecodeCpuImage();
    }

    private void TryDecodeCpuImage()
    {
        if (_cameraManager == null) return;
        if (IsBgEnabled) return;
        if (!_cameraManager.TryAcquireLatestCpuImage(out var image)) return;

        using (image)
        {
            try
            {
                int w = Mathf.Max(1, image.width / 2);
                int h = Mathf.Max(1, image.height / 2);

                if (_cameraTexture == null
                    || _cameraTexture.width != w
                    || _cameraTexture.height != h)
                {
                    if (_cameraTexture != null) Destroy(_cameraTexture);
                    _cameraTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    if (_displayImage != null)
                        _displayImage.texture = _cameraTexture;
                }

                var convParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(w, h),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.MirrorY,
                };

                var rawData = _cameraTexture.GetRawTextureData<byte>();
                image.Convert(convParams, rawData);
                _cameraTexture.Apply();
                DecodeFrameCount++;

                if (!_hasTexture && _displayImage != null)
                {
                    _hasTexture = true;
                    _displayImage.color = Color.white;
                    Debug.Log("[ARCameraDisplay] CPU fallback visible.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ARCameraDisplay] Frame decode failed: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnCameraFrameReceived;

        if (_cameraTexture != null)
            Destroy(_cameraTexture);
    }
}
