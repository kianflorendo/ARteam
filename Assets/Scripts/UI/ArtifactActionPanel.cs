// ============================================================
// ArtifactActionPanel.cs
// Location: Assets/Scripts/UI/ArtifactActionPanel.cs
// Mt. Samat AR — bottom action panel shown when an artifact is active.
//
// Shows two buttons:
//   COLLECT   — collects the artifact (hidden for info_only type)
//   SHOW INFO — toggles the ARDebugPanel info card
//
// NavigationManager controls the outer panel visibility (SetActive).
// This script controls the inner ButtonRow based on artifact state.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactActionPanel : MonoBehaviour
{
    // ── Colors ───────────────────────────────────────────────
    private static readonly Color C_COLLECT_ACTIVE = new Color(0.290f, 0.486f, 0.349f); // #4a7c59 Terra green
    private static readonly Color C_COLLECT_DONE   = new Color(0.550f, 0.550f, 0.550f); // grey when collected

    // ── Child refs (resolved at Start via transform.Find) ────
    private GameObject      _buttonRow;
    private Button          _collectButton;
    private Button          _showInfoButton;
    private TextMeshProUGUI _collectBtnText;
    private TextMeshProUGUI _showInfoBtnText;
    private Image           _collectBtnImage;

    // ── State ────────────────────────────────────────────────
    private ArtifactData _currentArtifact;
    private ARDebugPanel _infoPanel;
    private bool         _infoVisible;

    // ─────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Resolve child references by name — hierarchy built by UIHierarchySetup
        _buttonRow     = transform.Find("ButtonRow")?.gameObject;
        var collectGo  = transform.Find("ButtonRow/CollectButton");
        var infoGo     = transform.Find("ButtonRow/ShowInfoButton");

        _collectButton   = collectGo?.GetComponent<Button>();
        _showInfoButton  = infoGo?.GetComponent<Button>();
        _collectBtnText  = collectGo?.GetComponentInChildren<TextMeshProUGUI>();
        _showInfoBtnText = infoGo?.GetComponentInChildren<TextMeshProUGUI>();
        _collectBtnImage = collectGo?.GetComponent<Image>();

        // Find the info panel in the scene — it may be inactive at this point
        _infoPanel = FindObjectOfType<ARDebugPanel>(true);

        // Wire button clicks
        _collectButton?.onClick.AddListener(OnCollectPressed);
        _showInfoButton?.onClick.AddListener(OnShowInfoPressed);

        // Buttons hidden by default — only shown when an artifact is active
        if (_buttonRow != null) _buttonRow.SetActive(false);

        // Subscribe to artifact and inventory events
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

    // ─────────────────────────────────────────────────────────
    //  Event handlers
    // ─────────────────────────────────────────────────────────

    private void HandleArtifactSpawned(ArtifactInstance instance)
    {
        if (instance == null || instance.ArtifactData == null) return;
        ShowPanel(instance.ArtifactData);
    }

    private void HandleArtifactHidden(string artifactId)
    {
        if (_currentArtifact == null || _currentArtifact.id != artifactId) return;
        ResetPanel();
    }

    private void HandleInventoryCollected(string artifactId)
    {
        // Inventory confirmed the artifact collected — grey out the button
        if (_currentArtifact == null || _currentArtifact.id != artifactId) return;
        SetCollectButtonState(collected: true);
    }

    // ─────────────────────────────────────────────────────────
    //  Button callbacks
    // ─────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────
    //  Panel state management
    // ─────────────────────────────────────────────────────────

    private void ShowPanel(ArtifactData artifact)
    {
        _currentArtifact = artifact;
        _infoVisible     = false;

        // Reset Show Info button label
        if (_showInfoBtnText != null) _showInfoBtnText.text = "SHOW INFO";

        // Tell CollectionController which artifact is now active
        CollectionController.Instance?.SetCurrentArtifact(artifact);

        bool isCollectible    = artifact.type == ArtifactType.Collectible;
        bool alreadyCollected = InventoryManager.Instance != null
                                && InventoryManager.Instance.IsCollected(artifact.id);

        // Collect button only shown for collectible artifacts
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

        // Close info panel if it was open
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
