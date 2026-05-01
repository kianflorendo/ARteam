// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Shows the AR camera feed using CPU image conversion while
// ARSession.state < SessionTracking. ARCameraBackground relies on
// URP's ARBackgroundRendererFeature (GPU path) which can silently
// fail on some Android device/driver combinations. This bypasses
// that pipeline entirely: it reads CPU-side camera images from
// ARCameraManager and uploads them to a RawImage in a
// ScreenSpaceOverlay canvas behind the main UI canvas.
//
// The RawImage starts fully transparent (Color.clear) and only
// becomes opaque once the first real camera frame is decoded, so
// the user never sees a solid white or black flash.
//
// Once SessionTracking is established, this hides itself and
// ARCameraBackground (GPU path) takes over for proper AR compositing.
// ============================================================

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[DefaultExecutionOrder(-110)]
public class ARCameraDisplay : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private RawImage _displayImage;
    private Texture2D _cameraTexture;
    private bool _isShowing;
    private bool _hasTexture;
    private int _frameSkip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraDisplay>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraDisplay", typeof(ARCameraDisplay)));
    }

    private void Start()
    {
        // Full-screen canvas at sortingOrder -100 so it sits behind the main UI
        // canvas (sortingOrder 100). The transparent CameraScreen area in the main
        // canvas lets this camera image show through.
        var canvasGo = new GameObject("[AR Camera Fallback Canvas]");
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        var rawGo = new GameObject("CameraRaw");
        rawGo.transform.SetParent(canvasGo.transform, false);

        _displayImage = rawGo.AddComponent<RawImage>();
        // Start fully transparent — only opaque once a real frame is decoded.
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
        // Subscribe / re-subscribe to ARCameraManager when it (re-)appears.
        // ARSession restarts can swap out the component reference.
        var mgr = FindAnyObjectByType<ARCameraManager>();
        if (mgr != _cameraManager)
        {
            if (_cameraManager != null)
                _cameraManager.frameReceived -= OnCameraFrameReceived;

            _cameraManager = mgr;

            if (_cameraManager != null)
            {
                _cameraManager.frameReceived += OnCameraFrameReceived;
                Debug.Log("[ARCameraDisplay] Subscribed to ARCameraManager.frameReceived.");
            }
        }

        // Show during initialization, hide once tracking is established.
        bool needsShow = ARSession.state >= ARSessionState.SessionInitializing
                         && ARSession.state < ARSessionState.SessionTracking;

        if (_displayImage != null && needsShow != _isShowing)
        {
            _isShowing = needsShow;
            _displayImage.gameObject.SetActive(_isShowing);

            if (!_isShowing)
                _hasTexture = false; // reset so next init cycle starts fresh

            Debug.Log(_isShowing
                ? "[ARCameraDisplay] Showing CPU camera fallback."
                : "[ARCameraDisplay] SessionTracking reached — hiding CPU fallback.");
        }

        // Polling fallback: attempt image capture every other Update() frame
        // in case frameReceived never fires on this device/session.
        if (_isShowing && _cameraManager != null)
        {
            _frameSkip++;
            if (_frameSkip % 2 == 0)
                TryDecodeCpuImage();
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
                // Downsample to 1/4 resolution for performance.
                int w = Mathf.Max(1, image.width / 4);
                int h = Mathf.Max(1, image.height / 4);

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
                    inputRect        = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(w, h),
                    outputFormat     = TextureFormat.RGBA32,
                    transformation   = XRCpuImage.Transformation.MirrorY,
                };

                var rawData = _cameraTexture.GetRawTextureData<byte>();
                image.Convert(convParams, rawData);
                _cameraTexture.Apply();

                // First successful decode: make the image visible.
                if (!_hasTexture && _displayImage != null)
                {
                    _hasTexture = true;
                    _displayImage.color = Color.white;
                    Debug.Log("[ARCameraDisplay] First camera frame decoded — display is now visible.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ARCameraDisplay] Frame conversion failed: {e.Message}");
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
