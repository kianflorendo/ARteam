using System.Collections;
using TMPro;
using UnityEngine;

public class CollectNotificationBanner : MonoBehaviour
{
    private TextMeshProUGUI _collectedLabel;
    private TextMeshProUGUI _remainingLabel;

    private RectTransform _rt;
    private Coroutine     _activeRoutine;

    // ScanHeader is 65px; banner is 60px tall.
    // SHOWN_Y centres the banner below the ScanHeader with 5px gap (65 + 5 + 25 half-height).
    private const float SHOWN_Y   = -95f;
    private const float HIDDEN_Y  =  70f;
    private const float SLIDE_SPD = 700f;
    private const float HOLD_SEC  = 2.5f;

    private void Start()
    {
        _rt             = GetComponent<RectTransform>();
        _collectedLabel = transform.Find("BannerContent/CollectedLabel")
                                   ?.GetComponent<TextMeshProUGUI>();
        _remainingLabel = transform.Find("BannerContent/RemainingLabel")
                                   ?.GetComponent<TextMeshProUGUI>();

        if (_rt != null) _rt.anchoredPosition = new Vector2(0f, HIDDEN_Y);

        CollectionController.OnArtifactCollected += HandleArtifactCollected;
    }

    private void OnDestroy()
    {
        CollectionController.OnArtifactCollected -= HandleArtifactCollected;
    }

    private void HandleArtifactCollected(ArtifactData artifact)
    {
        if (artifact == null) return;

        if (!gameObject.activeInHierarchy) return;

        int remaining = GetRemainingCount();

        if (_collectedLabel != null)
            _collectedLabel.text = $"{artifact.name} collected!";

        if (_remainingLabel != null)
            _remainingLabel.text = remaining > 0
                ? $"{remaining} {(remaining == 1 ? "artifact" : "artifacts")} remaining"
                : "Route complete!";

        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(ShowAndDismiss());
    }

    private IEnumerator ShowAndDismiss()
    {
        yield return StartCoroutine(SlideTo(SHOWN_Y));
        yield return new WaitForSeconds(HOLD_SEC);
        yield return StartCoroutine(SlideTo(HIDDEN_Y));
        _activeRoutine = null;
    }

    private IEnumerator SlideTo(float targetY)
    {
        if (_rt == null) yield break;

        while (Mathf.Abs(_rt.anchoredPosition.y - targetY) > 0.5f)
        {
            float newY = Mathf.MoveTowards(
                _rt.anchoredPosition.y, targetY, SLIDE_SPD * Time.deltaTime);
            _rt.anchoredPosition = new Vector2(0f, newY);
            yield return null;
        }

        _rt.anchoredPosition = new Vector2(0f, targetY);
    }

    private static int GetRemainingCount()
    {
        if (ManifestLoader.Instance == null || InventoryManager.Instance == null)
            return 0;

        int remaining = 0;
        foreach (var a in ManifestLoader.Instance.GetGPSRouteArtifacts())
        {
            if (a.type == ArtifactType.Collectible
                && !InventoryManager.Instance.IsCollected(a.id))
                remaining++;
        }
        return remaining;
    }
}
