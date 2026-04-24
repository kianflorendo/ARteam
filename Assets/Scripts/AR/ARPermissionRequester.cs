using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Requests camera and location permissions before enabling the AR session.
/// The AR session must stay disabled until camera permission is confirmed.
/// </summary>
public class ARPermissionRequester : MonoBehaviour
{
    private ARSession _arSession;
    private bool _sessionEnableRequested;
    private GameObject _overlay;
    private TextMeshProUGUI _label;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _arSession = FindAnyObjectByType<ARSession>();
        if (_arSession != null)
            _arSession.enabled = false;
#endif
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += OnGranted;
        callbacks.PermissionDenied += OnDenied;
        callbacks.PermissionDeniedAndDontAskAgain += OnPermanentDenied;

        Permission.RequestUserPermissions(
            new[] { Permission.Camera, Permission.FineLocation },
            callbacks);

        Debug.Log("[ARPermissionRequester] Requesting Camera + Location permissions.");
        StartCoroutine(FallbackPermissionCheck());
#endif
    }

#if UNITY_ANDROID
    private void OnGranted(string permission)
    {
        Debug.Log($"[ARPermissionRequester] Granted: {permission}");
        if (permission == Permission.Camera)
            StartCoroutine(EnableARSession());
    }

    private void OnDenied(string permission)
    {
        Debug.LogWarning($"[ARPermissionRequester] Denied: {permission}");

        if (permission == Permission.Camera)
        {
            ShowOverlay(
                "Camera permission is required for AR.\n\n" +
                "Close the app, go to:\n" +
                "Settings -> Apps -> MT. Samat AR\n" +
                "-> Permissions -> Allow Camera\n\n" +
                "Then reopen the app.");
        }
        else if (permission == Permission.FineLocation)
        {
            ShowOverlay(
                "Location permission is required for the offline GPS route.\n\n" +
                "Close the app, go to:\n" +
                "Settings -> Apps -> MT. Samat AR\n" +
                "-> Permissions -> Allow Location\n\n" +
                "Then reopen the app.");
        }
    }

    private void OnPermanentDenied(string permission)
    {
        Debug.LogWarning($"[ARPermissionRequester] Permanently denied: {permission}");

        if (permission == Permission.Camera)
        {
            ShowOverlay(
                "Camera permission permanently denied.\n\n" +
                "Please open:\n" +
                "Settings -> Apps -> MT. Samat AR\n" +
                "-> Permissions -> Allow Camera\n\n" +
                "Then reopen the app.");
        }
        else if (permission == Permission.FineLocation)
        {
            ShowOverlay(
                "Location permission permanently denied.\n\n" +
                "Please open:\n" +
                "Settings -> Apps -> MT. Samat AR\n" +
                "-> Permissions -> Allow Location\n\n" +
                "Then reopen the app.");
        }
    }
#endif

    private IEnumerator FallbackPermissionCheck()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (_sessionEnableRequested)
            yield break;

#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("[ARPermissionRequester] Fallback: camera granted, enabling session.");
            StartCoroutine(EnableARSession());
        }
        else
        {
            Debug.LogWarning("[ARPermissionRequester] Fallback: camera not granted after 3 seconds.");
        }
#endif
    }

    private IEnumerator EnableARSession()
    {
        if (_sessionEnableRequested)
            yield break;

        _sessionEnableRequested = true;
        yield return null;

        if (_arSession != null)
        {
            _arSession.enabled = true;
            Debug.Log("[ARPermissionRequester] ARSession enabled.");
        }
        else
        {
            Debug.LogError("[ARPermissionRequester] ARSession reference is null.");
        }
    }

    private void ShowOverlay(string message)
    {
        if (_overlay != null)
            return;

        _overlay = new GameObject("PermissionOverlay");

        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        _overlay.AddComponent<UnityEngine.UI.CanvasScaler>();
        _overlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var bg = new GameObject("BG");
        bg.transform.SetParent(_overlay.transform, false);

        var img = bg.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.9f);

        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(_overlay.transform, false);
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.text = message;
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontSize = 30;
        _label.color = Color.white;
        _label.enableWordWrapping = true;

        var rect = _label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.3f);
        rect.anchorMax = new Vector2(0.92f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
