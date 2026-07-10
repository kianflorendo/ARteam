using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactActionPanel : MonoBehaviour
{
    private static readonly Color C_COLLECT_ACTIVE = new Color(0.290f, 0.486f, 0.349f);
    private static readonly Color C_COLLECT_DONE   = new Color(0.550f, 0.550f, 0.550f);

    private GameObject      _buttonRow;
    private Button          _collectButton;
    private Button          _showInfoButton;
    private TextMeshProUGUI _collectBtnText;
    private TextMeshProUGUI _showInfoBtnText;
    private Image           _collectBtnImage;

    private ArtifactData _currentArtifact;
    private ARDebugPanel _infoPanel;
    private bool         _infoVisible;

    private void Start()
    {
        _buttonRow     = transform.Find("ButtonRow")?.gameObject;
        var collectGo  = transform.Find("ButtonRow/CollectButton");
        var infoGo     = transform.Find("ButtonRow/ShowInfoButton");

        _collectButton   = collectGo?.GetComponent<Button>();
        _showInfoButton  = infoGo?.GetComponent<Button>();
        _collectBtnText  = collectGo?.GetComponentInChildren<TextMeshProUGUI>();
        _showInfoBtnText = infoGo?.GetComponentInChildren<TextMeshProUGUI>();
        _collectBtnImage = collectGo?.GetComponent<Image>();

        // Pass `true` so we find the info panel even when it's inactive.
        _infoPanel = FindObjectOfType<ARDebugPanel>(true);

        _collectButton?.onClick.AddListener(OnCollectPressed);
        _showInfoButton?.onClick.AddListener(OnShowInfoPressed);

        if (_buttonRow != null) _buttonRow.SetActive(false);

        ArtifactSpawner.OnArtifactSpawned    += HandleArtifactSpawned;
        ArtifactSpawner.OnArtifactHidden     += HandleArtifactHidden;
        InventoryManager.OnArtifactCollected += HandleInventoryCollected;
    }

    private void OnDestroy()
    {
        ArtifactSpawner.OnArtifactSpawned    -= HandleArtifactSpawned;
        ArtifactSpawner.OnArtifactHidden     -= HandleArtifactHidden;
        InventoryManager.OnArtifactCollected -= HandleInventoryCollected;
    }

    private void HandleArtifactSpawned(ArtifactInstance instance)
    {
        if (instance == null || instance.ArtifactData == null) return;
        ShowPanel(instance.ArtifactData);

        // Image-anchored artifacts: show info immediately on detection/re-detection.
        // GPS artifacts require an explicit SHOW INFO press (player is walking, info is optional).
        if (instance.ArtifactData.anchor_mode == AnchorMode.Image)
        {
            _infoVisible = true;
            _infoPanel?.ShowInfo(instance.ArtifactData);
            if (_showInfoBtnText != null) _showInfoBtnText.text = "HIDE INFO";
        }
    }

    private void HandleArtifactHidden(string artifactId)
    {
        if (_currentArtifact == null || _currentArtifact.id != artifactId) return;
        ResetPanel();
    }

    private void HandleInventoryCollected(string artifactId)
    {
        if (_currentArtifact == null || _currentArtifact.id != artifactId) return;
        SetCollectButtonState(collected: true);
    }

    private void OnCollectPressed()
    {
        if (_currentArtifact == null) return;
        CollectionController.Instance?.CollectArtifact(_currentArtifact);
    }

    private void OnShowInfoPressed()
    {
        if (_currentArtifact == null) return;

        _infoVisible = !_infoVisible;

        if (_infoVisible)
        {
            _infoPanel?.ShowInfo(_currentArtifact);
            if (_showInfoBtnText != null) _showInfoBtnText.text = "HIDE INFO";
        }
        else
        {
            _infoPanel?.Hide();
            if (_showInfoBtnText != null) _showInfoBtnText.text = "SHOW INFO";
        }
    }

    private void ShowPanel(ArtifactData artifact)
    {
        _currentArtifact = artifact;
        _infoVisible     = false;

        if (_showInfoBtnText != null) _showInfoBtnText.text = "SHOW INFO";

        CollectionController.Instance?.SetCurrentArtifact(artifact);

        bool isCollectible    = artifact.type == ArtifactType.Collectible;
        bool alreadyCollected = InventoryManager.Instance != null
                                && InventoryManager.Instance.IsCollected(artifact.id);

        if (_collectButton != null)
            _collectButton.gameObject.SetActive(isCollectible);

        if (isCollectible)
            SetCollectButtonState(collected: alreadyCollected);

        if (_buttonRow != null) _buttonRow.SetActive(true);
    }

    private void ResetPanel()
    {
        _currentArtifact = null;
        _infoVisible     = false;

        _infoPanel?.Hide();
        if (_showInfoBtnText != null) _showInfoBtnText.text = "SHOW INFO";

        if (_buttonRow != null) _buttonRow.SetActive(false);
    }

    private void SetCollectButtonState(bool collected)
    {
        if (_collectBtnImage != null)
            _collectBtnImage.color = collected ? C_COLLECT_DONE : C_COLLECT_ACTIVE;

        if (_collectBtnText != null)
            _collectBtnText.text = collected ? "COLLECTED" : "COLLECT";

        if (_collectButton != null)
            _collectButton.interactable = !collected;
    }
}
