using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Division Detail Screen - matches Terra Figma design: division_detail_6_item_nav
/// Shows:
/// - Division emblem hero section (large circular emblem)
/// - Division name and motto
/// - Mission Progress card with progress bar and active mission badge
/// - 2-column grid of artifact cards (COLLECTED vs MISSING states with dashed border)
/// - Historical Log section at bottom
/// </summary>
public class DivisionDetailScreen : MonoBehaviour
{
    [Header("Hero Section")]
    public Image divisionEmblemImage;           // Large circular emblem
    public TextMeshProUGUI divisionNameText;    // "21st Division"
    public TextMeshProUGUI divisionMottoText;   // "MOUNTAIN WATCHERS"

    [Header("Mission Progress Card")]
    public TextMeshProUGUI missionProgressTitle;        // "Mission Progress"
    public TextMeshProUGUI missionProgressDescription;  // "Finding Lost History"
    public TextMeshProUGUI missionProgressCount;        // "1/6 Artifacts Found"
    public Image missionProgressBar;
    public TextMeshProUGUI activeMissionBadge;          // "Active Mission: Operation Ridge"

    [Header("Artifact Collection Section")]
    public TextMeshProUGUI artifactCollectionTitle;     // "Artifact Collection"
    public Transform artifactsGridContainer;            // 2-column grid
    public GameObject artifactGridCardPrefab;           // Card prefab (shows COLLECTED or MISSING state)

    [Header("Historical Log Section")]
    public TextMeshProUGUI historicalLogTitle;          // "Historical Log"
    public TextMeshProUGUI historicalLogText;           // Division history text

    private DivisionData _currentDivision;
    private DivisionProgress _currentProgress;

    // ───────────────────────────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────────────────────────

    public void ShowDivision(string divisionId)
    {
        if (ManifestLoader.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogWarning("[DivisionDetailScreen] Managers not ready");
            return;
        }

        // Find division data
        _currentDivision = ManifestLoader.Instance.GetDivision(divisionId);

        if (_currentDivision == null)
        {
            Debug.LogWarning($"[DivisionDetailScreen] Division {divisionId} not found");
            return;
        }

        // Get progress
        _currentProgress = InventoryManager.Instance.GetDivisionProgress(divisionId);

        PopulateScreen();
    }

    // ───────────────────────────────────────────────────────────────────
    // Screen Population
    // ───────────────────────────────────────────────────────────────────

    private void PopulateScreen()
    {
        // Hero Section
        if (divisionNameText != null)
            divisionNameText.text = _currentDivision.name;

        if (divisionMottoText != null)
            divisionMottoText.text = _currentDivision.motto.ToUpper();

        // TODO: Load divisionEmblemImage from Addressables using _currentDivision.emblem_key

        // Mission Progress Card
        int collected = _currentProgress.collected.Count;
        int total = _currentDivision.required_artifacts.Count;
        float progressPercent = total > 0 ? (float)collected / total : 0f;

        if (missionProgressTitle != null)
            missionProgressTitle.text = "Mission Progress";

        if (missionProgressDescription != null)
            missionProgressDescription.text = "Finding Lost History";

        if (missionProgressCount != null)
            missionProgressCount.text = $"{collected}/{total} Artifacts Found";

        if (missionProgressBar != null)
            missionProgressBar.fillAmount = progressPercent;

        if (activeMissionBadge != null)
            activeMissionBadge.text = "Active Mission: Operation Ridge"; // TODO: Get from division data

        // Artifact Collection Grid
        if (artifactCollectionTitle != null)
            artifactCollectionTitle.text = "Artifact Collection";

        PopulateArtifactGrid();

        // Historical Log
        if (historicalLogTitle != null)
            historicalLogTitle.text = "Historical Log";

        if (historicalLogText != null)
            historicalLogText.text = "The 21st saw heavy action in the northern ridges during the winter offensive."; // TODO: Get from division data

        Debug.Log($"[DivisionDetailScreen] Populated {_currentDivision.name} - {collected}/{total} artifacts");

        // Play completion fanfare if just completed
        if (_currentProgress.completed && collected == total)
        {
            AudioManager.Instance?.PlayCompletionFanfareSFX();
        }
    }

    private void PopulateArtifactGrid()
    {
        // Clear existing cards
        foreach (Transform child in artifactsGridContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string artifactId in _currentDivision.required_artifacts)
        {
            ArtifactData artifact = ManifestLoader.Instance.GetArtifact(artifactId);
            if (artifact == null) continue;

            bool isCollected = _currentProgress.collected.Contains(artifactId);
            CreateArtifactGridCard(artifact, isCollected);
        }
    }

    private void CreateArtifactGridCard(ArtifactData artifact, bool isCollected)
    {
        if (artifactGridCardPrefab == null) return;

        GameObject card = Instantiate(artifactGridCardPrefab, artifactsGridContainer);
        var cardComponent = card.GetComponent<DivisionArtifactGridCard>();
        if (cardComponent != null)
        {
            cardComponent.Setup(artifact, isCollected);
        }
    }
}