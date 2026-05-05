// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt - Terra App
//
// CPU-path camera display that shows the real environment on Android
// devices where ARCameraBackground's GPU OES blit renders white garbage.
// This class replaces whatever ARCameraBackground draws with real camera
// pixels from the CPU image path.
//
// Canvas modes:
//   ScreenSpaceOverlay  (SessionInitializing)
//     Renders on top of everything; no Camera reference needed.
//     Covers the black AR-init clear color while ARCore starts up.
//
//   ScreenSpaceCamera at 15 m  (SessionTracking)
//     Depth-tested in the 3D scene. GPS artifacts at ~1 m stay in
//     front of this 15 m canvas.
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

        IsCameraManFound = _cameraManager != null;
        IsBgEnabled = false;
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

        if (_displayImage != null && shouldShow != _isShowing)
        {
            _isShowing = shouldShow;
            _displayImage.gameObject.SetActive(_isShowing);

            if (_isShowing && !_hasTexture)
                TryDecodeCpuImage();

            if (!_isShowing)
            {
                _hasTexture = false;
                _displayImage.color = Color.clear;
            }

            Debug.Log(_isShowing
                ? "[ARCameraDisplay] Showing CPU camera display."
                : "[ARCameraDisplay] Hidden - session pre-initialising.");
        }

        IsShowingFeed = _hasTexture;

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
                    Debug.Log("[ARCameraDisplay] Real environment visible — CPU feed active.");
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
