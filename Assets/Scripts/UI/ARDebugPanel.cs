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

        // Show AR guidance when session is stuck initializing
        if (ARSession.state == ARSessionState.SessionInitializing)
            sb.AppendLine(">> Point camera at ground + move slowly <<");

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

            // Show active seq / next seq in a readable way so the numbers make sense.
            // state.next_sequence_index equals the ACTIVE artifact's seq while one is
            // presented, which looks confusing ("Next Seq: 2" while seq-2 is already
            // showing). Show the true next-to-unlock value instead.
            var activeId = OfflineGPSRouteManager.Instance.ActiveArtifactId;
            if (!string.IsNullOrEmpty(activeId))
            {
                var activeArt = ManifestLoader.Instance?.GetArtifact(activeId);
                int activeSeq = activeArt?.sequence_index ?? OfflineGPSRouteManager.Instance.NextSequenceIndex;
                sb.AppendLine($"Active Seq: {activeSeq} | Unlock Next: {activeSeq + 1}");
            }
            else
            {
                sb.AppendLine($"Next Seq: {OfflineGPSRouteManager.Instance.NextSequenceIndex}");
            }

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

        // ── Camera feed diagnostics ──────────────────────────────────────
        sb.AppendLine($"CamFeed: {(ARCameraDisplay.IsShowingFeed ? "LIVE" : "WAITING")} | " +
                      $"CamMgr: {(ARCameraDisplay.IsCameraManFound ? "OK" : "NULL")} | " +
                      $"BgOn: {ARCameraDisplay.IsBgEnabled} | " +
                      $"Sub: {(ARCameraDisplay.IsSubsystemRunning ? "RUN" : "STOP")} | " +
                      $"Frames: {ARCameraDisplay.DecodeFrameCount}");

        debugText.text = sb.ToString();
    }

    // ── Debug reset button ────────────────────────────────────
    // Renders a one-tap button that resets GPS route state and
    // un-collects GPS test artifacts so the sequence starts from
    // Bolo Knife again. Useful when old save data persists after
    // a reinstall over an existing build.
    private void OnGUI()
    {
        float btnW = 220f;
        float btnH = 70f;
        float x = Screen.width - btnW - 10f;
        float y = Screen.height - btnH - 10f;

        GUI.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        if (GUI.Button(new Rect(x, y, btnW, btnH), "RESET GPS\nTEST DATA"))
        {
            ResetGPSTestData();
        }
        GUI.color = Color.white;
    }

    private void ResetGPSTestData()
    {
        Debug.Log("[ARDebugPanel] RESETTING GPS TEST DATA — route will restart from Bolo Knife.");

        // Remove GPS test artifact collected status from inventory
        InventoryManager.Instance?.RemoveCollectedArtifacts(
            new System.Collections.Generic.List<string>
            {
                "GPS-TEST-001",
                "GPS-TEST-002",
                "GPS-TEST-003",
                "GPS-TEST-004",
                "GPS-TEST-005",
                "GPS-TEST-006"
            });

        // Reset route state and in-memory tracking
        OfflineGPSRouteManager.Instance?.ResetForTesting();

        Debug.Log("[ARDebugPanel] Reset complete. Next: walk 1m to see Bolo Knife.");
    }
}
