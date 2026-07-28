using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARScanOverlayController : MonoBehaviour
{
    [Header("Scan Header")]
    [SerializeField] private TextMeshProUGUI _targetLabel;
    [SerializeField] private Image           _scanDot;

    [Header("Geofence Out-Of-Range Overlay")]
    [SerializeField] private GameObject _geofenceOutOfRangePanel;

    private Coroutine _pulseCoroutine;

    private void OnEnable()
    {
        RefreshTarget();
        ManifestLoader.OnManifestLoaded += RefreshTarget;
        GeofenceGuard.OnGeofenceStatusChanged += OnGeofenceChanged;

        // Set initial geofence overlay state.
        bool inside = GeofenceGuard.Instance == null || GeofenceGuard.Instance.IsInsideGeofence;
        SetGeofencePanel(!inside);

        if (_scanDot != null && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseDot());
    }

    private void OnDisable()
    {
        ManifestLoader.OnManifestLoaded -= RefreshTarget;
        GeofenceGuard.OnGeofenceStatusChanged -= OnGeofenceChanged;
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    private void OnGeofenceChanged(bool isInside) => SetGeofencePanel(!isInside);

    private void SetGeofencePanel(bool visible)
    {
        if (_geofenceOutOfRangePanel != null)
            _geofenceOutOfRangePanel.SetActive(visible);
    }

    private void RefreshTarget()
    {
        if (_targetLabel == null || ManifestLoader.Instance == null) return;

        var route = ManifestLoader.Instance.GetGPSRouteArtifacts();
        int total = route.Count;
        int seq   = 1;

        if (OfflineGPSRouteManager.Instance != null)
        {
            string activeId = OfflineGPSRouteManager.Instance.ActiveArtifactId;
            if (!string.IsNullOrEmpty(activeId))
            {
                var art = ManifestLoader.Instance.GetArtifact(activeId);
                if (art != null) seq = art.sequence_index;
            }
            else
            {
                seq = OfflineGPSRouteManager.Instance.NextSequenceIndex;
            }
        }

        _targetLabel.text = $"Target {seq} of {total}";
    }

    private IEnumerator PulseDot()
    {
        while (true)
        {
            SetDotAlpha(1f);
            yield return new WaitForSeconds(0.6f);
            SetDotAlpha(0.25f);
            yield return new WaitForSeconds(0.6f);
        }
    }

    private void SetDotAlpha(float a)
    {
        if (_scanDot == null) return;
        Color c = _scanDot.color;
        c.a = a;
        _scanDot.color = c;
    }
}
