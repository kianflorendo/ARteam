// ============================================================
// HomeScreenController.cs
// Location: Assets/Scripts/UI/HomeScreenController.cs
// Mt. Samat AR — Home Dashboard screen.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private TextMeshProUGUI _progressCountLabel;
    [SerializeField] private Image           _progressBarFill;

    [Header("Soldier Cards")]
    [SerializeField] private GameObject _filipinoCard;
    [SerializeField] private GameObject _japaneseCard;
    [SerializeField] private GameObject _americanCard;

    private Image        _filipinoCardBg;
    private Image        _japaneseCardBg;
    private Image        _americanCardBg;

    private CanvasGroup  _japaneseGroup;
    private CanvasGroup  _americanGroup;

    private static readonly Color ACTIVE_BORDER = new Color(0.102f, 0.102f, 0.102f, 1f);
    private static readonly Color LOCKED_BORDER = new Color(0.851f, 0.851f, 0.851f, 1f);

    private void Awake()
    {
        if (_filipinoCard != null)
            _filipinoCardBg = _filipinoCard.GetComponent<Image>();

        if (_japaneseCard != null)
        {
            _japaneseCardBg = _japaneseCard.GetComponent<Image>();
            _japaneseGroup  = _japaneseCard.GetComponent<CanvasGroup>();
            if (_japaneseGroup == null) _japaneseGroup = _japaneseCard.AddComponent<CanvasGroup>();
        }
        if (_americanCard != null)
        {
            _americanCardBg = _americanCard.GetComponent<Image>();
            _americanGroup  = _americanCard.GetComponent<CanvasGroup>();
            if (_americanGroup == null) _americanGroup = _americanCard.AddComponent<CanvasGroup>();
        }

        _filipinoCard?.GetComponent<Button>()?.onClick.AddListener(() => OnSelectSoldier("S-001"));
        _japaneseCard?.GetComponent<Button>()?.onClick.AddListener(() => OnSelectSoldier("S-002"));
        _americanCard?.GetComponent<Button>()?.onClick.AddListener(() => OnSelectSoldier("S-003"));
    }

    private void OnEnable()
    {
        RefreshProgress();
        RefreshSoldierCards();
    }

    // ── Progress bar ─────────────────────────────────────────

    private void RefreshProgress()
    {
        if (InventoryManager.Instance == null || ManifestLoader.Instance == null) return;

        var route     = ManifestLoader.Instance.GetGPSRouteArtifacts();
        int total     = route.Count;
        int collected = 0;
        foreach (var a in route)
            if (InventoryManager.Instance.IsCollected(a.id)) collected++;

        if (_progressCountLabel != null)
            _progressCountLabel.text = $"{collected}/{total}";
        if (_progressBarFill != null)
            _progressBarFill.fillAmount = total > 0 ? (float)collected / total : 0f;
    }

    // ── Soldier cards ────────────────────────────────────────

    private void RefreshSoldierCards()
    {
        if (ActiveSoldierManager.Instance == null || InventoryManager.Instance == null) return;

        string active   = ActiveSoldierManager.Instance.ActiveSoldierId;
        bool   jpUnlock = InventoryManager.Instance.IsSoldierUnlocked("S-002");
        bool   usUnlock = InventoryManager.Instance.IsSoldierUnlocked("S-003");

        ApplyCardState(_filipinoCardBg, null,           active == "S-001", true);
        ApplyCardState(_japaneseCardBg, _japaneseGroup, active == "S-002", jpUnlock);
        ApplyCardState(_americanCardBg, _americanGroup, active == "S-003", usUnlock);
    }

    private void ApplyCardState(Image bg, CanvasGroup group, bool isActive, bool isUnlocked)
    {
        if (bg    != null) bg.color    = isActive ? ACTIVE_BORDER : LOCKED_BORDER;
        if (group != null) group.alpha = isUnlocked ? 1f : 0.5f;
    }

    public void OnSelectSoldier(string soldierId)
    {
        if (InventoryManager.Instance == null) return;
        if (!InventoryManager.Instance.IsSoldierUnlocked(soldierId))
        {
            Debug.Log($"[HomeScreenController] Soldier {soldierId} is locked.");
            return;
        }

        ActiveSoldierManager.Instance?.SetActiveSoldier(soldierId);
        OfflineGPSRouteManager.Instance?.ResetForTesting();
        RefreshSoldierCards();
        NavigationManager.Instance?.ShowScreen("ARScan");
    }
}
