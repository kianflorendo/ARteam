using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Divisions List Screen - matches Terra Figma design: divisions_inventory_6_item_nav
/// Shows:
/// - Title: "Philippine Army Divisions"
/// - Description text
/// - "INVENTORY HIGHLIGHTS" section showing X/6 progress
/// - Division cards with emblem, name, motto, progress, COMPLETED badge
/// </summary>
public class DivisionsListScreen : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText;           // "Philippine Army Divisions"
    public TextMeshProUGUI descriptionText;     // Browse the courageous divisions...

    [Header("Inventory Highlights")]
    public TextMeshProUGUI highlightsLabel;     // "INVENTORY HIGHLIGHTS"
    public TextMeshProUGUI highlightsProgress;  // "2/6"

    [Header("Division Cards")]
    public Transform divisionsContainer;        // Parent for division cards
    public GameObject divisionCardPrefab;       // Prefab for each division card

    // ───────────────────────────────────────────────────────────────────
    // Lifecycle
    // ───────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        PopulateScreen();
    }

    // ───────────────────────────────────────────────────────────────────
    // Screen Population
    // ───────────────────────────────────────────────────────────────────

    public void PopulateScreen()
    {
        if (ManifestLoader.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogWarning("[DivisionsListScreen] Managers not ready");
            return;
        }

        // Header
        if (titleText != null)
            titleText.text = "Philippine Army Divisions";

        if (descriptionText != null)
            descriptionText.text = "Browse the courageous divisions that fought at Bataan. Collect artifacts associated with each battalion to complete your digital archive.";

        // Calculate highlights
        var allDivisions = ManifestLoader.Instance.GetAllDivisions();

        int completedCount = 0;
        int totalCount = allDivisions.Count;

        foreach (var division in allDivisions)
        {
            var progress = InventoryManager.Instance.GetDivisionProgress(division.id);
            if (progress.completed)
                completedCount++;
        }

        if (highlightsLabel != null)
            highlightsLabel.text = "INVENTORY HIGHLIGHTS";

        if (highlightsProgress != null)
            highlightsProgress.text = $"{completedCount}/{totalCount}";

        // Populate division cards
        PopulateDivisionCards();

        Debug.Log($"[DivisionsListScreen] Populated {totalCount} divisions, {completedCount} completed");
    }

    private void PopulateDivisionCards()
    {
        // Clear existing cards
        foreach (Transform child in divisionsContainer)
        {
            Destroy(child.gameObject);
        }

        var allDivisions = ManifestLoader.Instance.GetAllDivisions();

        foreach (var division in allDivisions)
        {
            var progress = InventoryManager.Instance.GetDivisionProgress(division.id);
            CreateDivisionCard(division, progress);
        }
    }

    private void CreateDivisionCard(DivisionData division, DivisionProgress progress)
    {
        if (divisionCardPrefab == null) return;

        GameObject card = Instantiate(divisionCardPrefab, divisionsContainer);
        var cardComponent = card.GetComponent<DivisionListCard>();
        if (cardComponent != null)
        {
            cardComponent.Setup(division, progress);
        }
    }
}