// ============================================================
// ARCameraDisplay.cs
// Location: Assets/Scripts/AR/ARCameraDisplay.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Shows the AR camera feed using CPU image conversion while
// ARSession.state < SessionTracking. ARCameraBackground cannot
// receive GPU frames on some Android devices during initialization,
// so this bypasses the URP rendering pipeline entirely by reading
// CPU-side camera images from ARCameraManager and uploading them
// to a RawImage in a ScreenSpaceOverlay canvas behind the main UI.
//
// Once SessionTracking is established, this hides itself and
// ARCameraBackground takes over for proper AR compositing.
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
    private int _frameSkip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARCameraDisplay>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARCameraDisplay", typeof(ARCameraDisplay)));
    }

    private void Start()
    {
        // Build a full-screen canvas at sortingOrder -100 so it sits behind
        // the main UI canvas (sortingOrder 100). The transparent CameraScreen
        // area in the main canvas lets this camera image show through.
        var canvasGo = new GameObject("[AR Camera Fallback Canvas]");
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        var rawGo = new GameObject("CameraRaw");
        rawGo.transform.SetParent(canvasGo.transform, false);

        _displayImage = rawGo.AddComponent<RawImage>();
        _displayImage.color = Color.white;

        var rect = rawGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Start hidden — Update() will show it once session begins initializing.
        _displayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Subscribe to ARCameraManager when it becomes available
        if (_cameraManager == null)
        {
            _cameraManager = FindAnyObjectByType<ARCameraManager>();
            if (_cameraManager != null)
            {
                _cameraManager.frameReceived += OnCameraFrameReceived;
                Debug.Log("[ARCameraDisplay] Subscribed to ARCameraManager.frameReceived.");
            }
        }

        // Show during initialization, hide once tracking is established
        bool needsShow = ARSession.state >= ARSessionState.SessionInitializing
                         && ARSession.state < ARSessionState.SessionTracking;

        if (needsShow == _isShowing) return;

        _isShowing = needsShow;

        if (_displayImage != null)
            _displayImage.gameObject.SetActive(_isShowing);

        Debug.Log(_isShowing
            ? "[ARCameraDisplay] Showing CPU camera fallback."
            : "[ARCameraDisplay] SessionTracking reached — hiding CPU fallback.");
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isShowing || _cameraManager == null) return;

        // Convert every 2nd frame to reduce CPU load
        _frameSkip++;
        if (_frameSkip % 2 != 0) return;

        if (!_cameraManager.TryAcquireLatestCpuImage(out var image))
            return;

        using (image)
        {
            try
            {
                // Downsample to 1/4 resolution for performance.
                // Camera is typically 1080×1920 → outputs at ~270×480.
                int w = Mathf.Max(1, image.width / 4);
                int h = Mathf.Max(1, image.height / 4);

                // Re-create texture only when dimensions change.
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
                    // MirrorY is the standard transform for Android back cameras.
                    transformation = XRCpuImage.Transformation.MirrorY,
                };

                // Write directly into the Texture2D's native memory — no extra allocation.
                var rawData = _cameraTexture.GetRawTextureData<byte>();
                image.Convert(convParams, rawData);
                _cameraTexture.Apply();
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
