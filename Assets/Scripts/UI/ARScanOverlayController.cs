// ============================================================
// ARScanOverlayController.cs
// Location: Assets/Scripts/UI/ARScanOverlayController.cs
// Mt. Samat AR — AR Scan screen overlay.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARScanOverlayController : MonoBehaviour
{
    [Header("Scan Header")]
    [SerializeField] private TextMeshProUGUI _targetLabel;
    [SerializeField] private Image           _scanDot;

    private Coroutine _pulseCoroutine;

    private void OnEnable()
    {
        RefreshTarget();
        ManifestLoader.OnManifestLoaded += RefreshTarget;
        if (_scanDot != null && _pulseCoroutine == null)
            _pulseCoroutine = StartCoroutine(PulseDot());
    }

    private void OnDisable()
    {
        ManifestLoader.OnManifestLoaded -= RefreshTarget;
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
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
