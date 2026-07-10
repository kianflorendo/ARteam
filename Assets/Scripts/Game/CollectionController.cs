using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionController : MonoBehaviour
{
    public static CollectionController Instance { get; private set; }

    public static event System.Action<ArtifactData> OnArtifactCollected;
    public static event System.Action<string> OnCollectFailed;

    [Header("Collect Animation")]
    public float collectAnimDuration = 0.8f;
    public RectTransform hudTargetIcon;

    [Header("Toast")]
    public TextMeshProUGUI toastText;
    public float toastDuration = 2f;

    private ArtifactData _currentArtifact;
    private bool _isCollecting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentArtifact(ArtifactData artifact)
    {
        _currentArtifact = artifact;
    }

    public void OnCollectPressed()
    {
        if (_currentArtifact == null)
        {
            Debug.LogWarning("[CollectionController] OnCollectPressed with no artifact set.");
            return;
        }

        if (_isCollecting)
        {
            Debug.Log("[CollectionController] Already collecting. Ignoring tap.");
            return;
        }

        if (InventoryManager.Instance != null
            && InventoryManager.Instance.IsCollected(_currentArtifact.id))
        {
            Debug.Log($"[CollectionController] {_currentArtifact.id} already collected.");
            OnCollectFailed?.Invoke(_currentArtifact.id);
            return;
        }

        StartCoroutine(CollectSequence(_currentArtifact));
    }

    public void CollectArtifact(ArtifactData artifact)
    {
        if (artifact == null) return;
        _currentArtifact = artifact;
        OnCollectPressed();
    }

    private IEnumerator CollectSequence(ArtifactData artifact)
    {
        _isCollecting = true;
        Debug.Log($"[CollectionController] Collecting: {artifact.name}");

        AudioManager.Instance?.PlayCollectSFX();

        yield return StartCoroutine(PlayCollectAnimation(artifact));

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.CollectArtifact(artifact.id);

        if (AutoMatcher.Instance != null)
            AutoMatcher.Instance.Match(artifact);

        ShowToast($"{artifact.name} collected!");

        OnArtifactCollected?.Invoke(artifact);

        Debug.Log($"[CollectionController] Collect sequence complete: {artifact.name}");
        _isCollecting = false;
    }

    private IEnumerator PlayCollectAnimation(ArtifactData artifact)
    {
        yield return new WaitForSeconds(collectAnimDuration);
    }

    private void ShowToast(string message)
    {
        Debug.Log($"[CollectionController] Toast: {message}");

        if (toastText != null)
        {
            StopCoroutine(nameof(HideToastCoroutine));
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            StartCoroutine(HideToastCoroutine());
        }
    }

    private IEnumerator HideToastCoroutine()
    {
        yield return new WaitForSeconds(toastDuration);
        if (toastText != null)
            toastText.gameObject.SetActive(false);
    }

    public bool IsCollecting => _isCollecting;
}
