// ============================================================
// SoldierScreenController.cs
// Location: Assets/Scripts/UI/SoldierScreenController.cs
// Mt. Samat AR — Soldier tab: artifact checklist per soldier.
//
// Figma: carousel (prev/next) + progress bar + ITEMS TO FIND
// list with per-artifact collected checkmarks + Scan Next Item.
// All static references are [SerializeField] — wire in Prefab Editor.
// Artifact rows are created dynamically at runtime.
// ============================================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoldierScreenController : MonoBehaviour
{
    // ── Soldier ID cycle order ────────────────────────────
    private static readonly string[] SOLDIER_IDS = { "S-001", "S-002", "S-003" };

    // ── Serialized references (wire in Prefab Editor) ────
    [Header("Carousel")]
    [SerializeField] private Image  _centerAvatar;
    [SerializeField] private Button _prevBtn;
    [SerializeField] private Button _nextBtn;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI _soldierNameLabel;
    [SerializeField] private TextMeshProUGUI _progressCountLabel;
    [SerializeField] private Image           _progressBarFill;

    [Header("Artifact List")]
    [SerializeField] private Transform _artifactListContainer;
    [SerializeField] private Button    _scanNextItemBtn;

    // ── Runtime state ────────────────────────────────────
    private int                _currentIndex;
    private readonly List<GameObject> _rows = new List<GameObject>();

    // ── Colours (match UIHierarchySetup palette) ─────────
    private static readonly Color COL_BLACK   = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color COL_WHITE   = Color.white;
    private static readonly Color COL_GRAY_D9 = new Color(0.851f, 0.851f, 0.851f);
    private static readonly Color COL_GRAY_E8 = new Color(0.910f, 0.910f, 0.910f);
    private static readonly Color COL_GRAY_88 = new Color(0.533f, 0.533f, 0.533f);

    // ─────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────

    private void Awake()
    {
        _prevBtn?.onClick.AddListener(OnPrev);
        _nextBtn?.onClick.AddListener(OnNext);
        _scanNextItemBtn?.onClick.AddListener(OnScanNextItem);
    }

    private void OnEnable()
    {
        SyncIndex();
        RefreshDisplay();
    }

    // ─────────────────────────────────────────────────────
    //  Refresh
    // ─────────────────────────────────────────────────────

    private void SyncIndex()
    {
        string active = ActiveSoldierManager.Instance?.ActiveSoldierId ?? "S-001";
        for (int i = 0; i < SOLDIER_IDS.Length; i++)
        {
            if (SOLDIER_IDS[i] == active) { _currentIndex = i; return; }
        }
        _currentIndex = 0;
    }

    private void RefreshDisplay()
    {
        string      soldierId  = SOLDIER_IDS[_currentIndex];
        SoldierData data       = ManifestLoader.Instance?.GetSoldier(soldierId);
        bool        unlocked   = InventoryManager.Instance?.IsSoldierUnlocked(soldierId) ?? false;

        // Soldier name
        if (_soldierNameLabel != null)
        {
            string nat = data?.nationality ?? "Filipino";
            _soldierNameLabel.text = nat.ToUpper() + " SOLDIER";
        }

        // Center avatar: black when unlocked, gray when locked
        if (_centerAvatar != null)
            _centerAvatar.color = unlocked ? COL_BLACK : COL_GRAY_D9;

        // Progress bar and count
        RefreshProgress(data, unlocked);

        // Artifact rows
        RebuildArtifactList(data, unlocked);
    }

    private void RefreshProgress(SoldierData data, bool unlocked)
    {
        if (!unlocked || data == null)
        {
            if (_progressCountLabel != null) _progressCountLabel.text = "—";
            if (_progressBarFill    != null) _progressBarFill.fillAmount = 0f;
            return;
        }

        int total = data.required_artifacts?.Count ?? 0;
        int found = 0;
        if (InventoryManager.Instance != null && data.required_artifacts != null)
            foreach (var id in data.required_artifacts)
                if (InventoryManager.Instance.IsCollected(id)) found++;

        if (_progressCountLabel != null) _progressCountLabel.text = $"{found}/{total}";
        if (_progressBarFill    != null)
            _progressBarFill.fillAmount = total > 0 ? (float)found / total : 0f;
    }

    // ─────────────────────────────────────────────────────
    //  Dynamic row builder
    // ─────────────────────────────────────────────────────

    private void RebuildArtifactList(SoldierData data, bool unlocked)
    {
        if (_artifactListContainer == null) return;

        ClearRows();

        if (!unlocked || data == null || data.required_artifacts == null)
        {
            SpawnLockedOverlay();
            return;
        }

        bool first = true;
        foreach (var artifactId in data.required_artifacts)
        {
            if (!first) SpawnDivider();
            first = false;

            bool collected = InventoryManager.Instance?.IsCollected(artifactId) ?? false;
            SpawnArtifactRow(artifactId, collected);
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row);
        _rows.Clear();
    }

    private void SpawnArtifactRow(string artifactId, bool collected)
    {
        var row = new GameObject($"Row_{artifactId}", typeof(RectTransform));
        row.transform.SetParent(_artifactListContainer, false);

        row.AddComponent<LayoutElement>().preferredHeight = 54;
        row.AddComponent<Image>().color = Color.clear;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlHeight    = false;
        hlg.childControlWidth     = false;
        hlg.childForceExpandWidth = false;
        hlg.padding               = new RectOffset(20, 18, 0, 0);
        hlg.spacing               = 10;

        // Artifact image square (placeholder)
        var img = new GameObject("ArtImg", typeof(RectTransform));
        img.transform.SetParent(row.transform, false);
        var imgLE = img.AddComponent<LayoutElement>();
        imgLE.preferredWidth = 40; imgLE.preferredHeight = 40;
        img.AddComponent<Image>().color = COL_GRAY_E8;

        // Name bar flex container
        var flex = new GameObject("Flex", typeof(RectTransform));
        flex.transform.SetParent(row.transform, false);
        var flexLE = flex.AddComponent<LayoutElement>();
        flexLE.flexibleWidth = 1; flexLE.preferredHeight = 40;

        var bar = new GameObject("NameBar", typeof(RectTransform));
        bar.transform.SetParent(flex.transform, false);
        var barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0.5f);
        barRT.anchorMax = new Vector2(1, 0.5f);
        barRT.pivot     = new Vector2(0.5f, 0.5f);
        barRT.sizeDelta = new Vector2(0, 15);
        bar.AddComponent<Image>().color = COL_GRAY_E8;

        // Check circle
        var circle = new GameObject("CheckCircle", typeof(RectTransform));
        circle.transform.SetParent(row.transform, false);
        var circleLE = circle.AddComponent<LayoutElement>();
        circleLE.preferredWidth = 26; circleLE.preferredHeight = 26;
        circle.AddComponent<Image>().color = collected ? COL_BLACK : COL_GRAY_E8;

        if (collected)
        {
            var checkGo = new GameObject("Check", typeof(RectTransform));
            checkGo.transform.SetParent(circle.transform, false);
            var cRT = checkGo.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one; cRT.sizeDelta = Vector2.zero;
            var ct = checkGo.AddComponent<TextMeshProUGUI>();
            ct.text = "✓"; ct.fontSize = 14; ct.color = COL_WHITE;
            ct.alignment = TextAlignmentOptions.Center;
        }

        _rows.Add(row);
    }

    private void SpawnDivider()
    {
        var div = new GameObject("Divider", typeof(RectTransform));
        div.transform.SetParent(_artifactListContainer, false);
        div.AddComponent<LayoutElement>().preferredHeight = 1;
        div.AddComponent<Image>().color = COL_GRAY_E8;
        _rows.Add(div);
    }

    private void SpawnLockedOverlay()
    {
        var locked = new GameObject("LockedOverlay", typeof(RectTransform));
        locked.transform.SetParent(_artifactListContainer, false);
        locked.AddComponent<LayoutElement>().preferredHeight = 120;
        locked.AddComponent<Image>().color = COL_GRAY_E8;

        var lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(locked.transform, false);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one; lRT.sizeDelta = Vector2.zero;
        var lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = "Locked"; lt.fontSize = 16; lt.fontStyle = FontStyles.Bold;
        lt.color = COL_GRAY_88; lt.alignment = TextAlignmentOptions.Center;

        _rows.Add(locked);
    }

    // ─────────────────────────────────────────────────────
    //  Button handlers
    // ─────────────────────────────────────────────────────

    private void OnPrev()
    {
        _currentIndex = (_currentIndex - 1 + SOLDIER_IDS.Length) % SOLDIER_IDS.Length;
        ActiveSoldierManager.Instance?.SetActiveSoldier(SOLDIER_IDS[_currentIndex]);
        RefreshDisplay();
        AudioManager.Instance?.PlayUITapSFX();
    }

    private void OnNext()
    {
        _currentIndex = (_currentIndex + 1) % SOLDIER_IDS.Length;
        ActiveSoldierManager.Instance?.SetActiveSoldier(SOLDIER_IDS[_currentIndex]);
        RefreshDisplay();
        AudioManager.Instance?.PlayUITapSFX();
    }

    private void OnScanNextItem()
    {
        NavigationManager.Instance?.ShowScreen("ARScan");
        AudioManager.Instance?.PlayUITapSFX();
    }
}
