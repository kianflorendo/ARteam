using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARDebugPanel : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI debugText;
    public ARTrackedImageManager imageManager;
    private ARSession _arSession;

    private float _updateInterval = 0.5f;
    private float _timer;
    private int _spawnedCount;

    private void Start()
    {
        if (debugText == null)
        {
            var textTransform = transform.Find("DebugInfo");
            if (textTransform != null)
                debugText = textTransform.GetComponent<TextMeshProUGUI>();
        }

        if (imageManager == null)
            imageManager = FindAnyObjectByType<ARTrackedImageManager>();

        _arSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
        UpdateDebugText();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _updateInterval)
            return;

        _timer = 0f;
        UpdateDebugText();
    }

    public void SetSpawnedCount(int count)
    {
        _spawnedCount = count;
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("=== AR DEBUG INFO ===");

        if (_arSession == null)
            _arSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);

        if (_arSession != null)
            sb.AppendLine($"Session: {ARSession.state} | Enabled:{_arSession.enabled}");
        else
            sb.AppendLine("Session: NOT FOUND");

#if UNITY_ANDROID
        bool camPerm = UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            UnityEngine.Android.Permission.Camera);
        bool gpsPerm = UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            UnityEngine.Android.Permission.FineLocation);
        sb.AppendLine($"CamPerm: {(camPerm ? "GRANTED" : "DENIED")}");
        sb.AppendLine($"GpsPerm: {(gpsPerm ? "GRANTED" : "DENIED")}");
#endif

        if (LocationServiceManager.Instance != null)
        {
            sb.AppendLine($"GPS: {LocationServiceManager.Instance.GetStatusString()}");

            if (LocationServiceManager.Instance.TryGetFilteredLocation(
                    out double lat,
                    out double lng,
                    out float acc))
            {
                sb.AppendLine($"Fix: {lat:F6}, {lng:F6} (+/-{acc:F1}m)");
            }
        }
        else
        {
            sb.AppendLine("GPS: manager not found");
        }

        if (OfflineGPSRouteManager.Instance != null)
        {
            sb.AppendLine($"Route Origin: {(OfflineGPSRouteManager.Instance.HasOrigin ? "SET" : "WAITING")}");
            sb.AppendLine($"Next Seq: {OfflineGPSRouteManager.Instance.NextSequenceIndex}");
            sb.AppendLine($"Target: {OfflineGPSRouteManager.Instance.CurrentTargetName}");
            sb.AppendLine($"Segment: {OfflineGPSRouteManager.Instance.CurrentSegmentDistance:F2} / {OfflineGPSRouteManager.Instance.CurrentTargetDistance:F2}m");
            sb.AppendLine($"Active GPS Artifact: {OfflineGPSRouteManager.Instance.ActiveArtifactId}");
        }
        else
        {
            sb.AppendLine("Route: manager not found");
        }

        if (imageManager != null)
        {
            int tracked = 0;
            foreach (var trackedImage in imageManager.trackables)
            {
                if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                    tracked++;
            }
            sb.AppendLine($"Images Tracked: {tracked}");
        }
        else
        {
            sb.AppendLine("ImageManager: not found");
        }

        int spawned = ArtifactSpawner.Instance?.GetSpawnedCount() ?? _spawnedCount;
        sb.AppendLine($"Spawned Objects: {spawned}");

        bool manifestOk = ManifestLoader.Instance != null && ManifestLoader.Instance.IsLoaded;
        sb.AppendLine($"Manifest: {(manifestOk ? "Loaded" : "Loading...")}");

        debugText.text = sb.ToString();
    }
}
