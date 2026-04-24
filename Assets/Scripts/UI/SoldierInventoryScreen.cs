using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Soldier Inventory Screen - matches Terra Figma design: soldier_inventory_6_item_nav
/// Shows:
/// - Mission Progress card (50% progress bar)
/// - Large soldier card with emblem
/// - Collected Artifacts section with artifact cards (ACQUIRED badge, checkmark)
/// </summary>
public class SoldierInventoryScreen : MonoBehaviour
{
    [Header("Mission Progress Card")]
    public TextMeshProUGUI missionProgressTitle;
    public TextMeshProUGUI missionProgressDescription;
    public TextMeshProUGUI missionProgressPercent;
    public Image missionProgressBar;

    [Header("Soldier Card")]
    public Image soldierEmblemImage;
    public TextMeshProUGUI soldierBadgeText;        // "PHILIPINE WORK"
    public TextMeshProUGUI soldierInventoryStatus;  // "INVENTORY STATUS"
    public TextMeshProUGUI soldierNameText;         // "American Soldier"

    [Header("Collected Artifacts Section")]
    public TextMeshProUGUI collectedArtifactsTitle; // "Collected Artifacts"
    public Transform artifactsContainer;            // Parent for artifact cards
    public GameObject artifactCardPrefab;           // Prefab for each artifact card

    private SoldierData _currentSoldier;
    private SoldierProgress _currentProgress;

    // ───────────────────────────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────────────────────────

    public void ShowSoldier(string soldierId)
    {
        if (ManifestLoader.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogWarning("[SoldierInventoryScreen] Managers not ready");
            return;
        }

        // Find soldier data
        _currentSoldier = ManifestLoader.Instance.GetSoldier(soldierId);

        if (_currentSoldier == null)
        {
            Debug.LogWarning($"[SoldierInventoryScreen] Soldier {soldierId} not found");
            return;
        }

        // Get progress
        _currentProgress = InventoryManager.Instance.GetSoldierProgress(soldierId);

        PopulateScreen();
    }

    // ───────────────────────────────────────────────────────────────────
    // Screen Population
    // ───────────────────────────────────────────────────────────────────

    private void PopulateScreen()
    {
        // Mission Progress Card
        int collected = _currentProgress.collected.Count;
        int total = _currentSoldier.required_artifacts.Count;
        float progressPercent = total > 0 ? (float)collected / total : 0f;

        if (missionProgressTitle != null)
            missionProgressTitle.text = "Mission Progress";

        if (missionProgressDescription != null)
            missionProgressDescription.text = $"Collect all {total} vital artifacts to unlock historical intel.";

        if (missionProgressPercent != null)
            missionProgressPercent.text = $"{Mathf.RoundToInt(progressPercent * 100)}%";

        if (missionProgressBar != null)
            missionProgressBar.fillAmount = progressPercent;

        // Soldier Card
        if (soldierBadgeText != null)
            soldierBadgeText.text = "PHILIPPINE WORK"; // TODO: Get from soldier data

        if (soldierInventoryStatus != null)
            soldierInventoryStatus.text = "INVENTORY STATUS";

        if (soldierNameText != null)
            soldierNameText.text = _currentSoldier.name;

        // TODO: Load soldierEmblemImage from Addressables using _currentSoldier.bundle_key

        // Collected Artifacts Section
        PopulateArtifactCards();

        Debug.Log($"[SoldierInventoryScreen] Populated {_currentSoldier.name} - {collected}/{total} artifacts");
    }

    private void PopulateArtifactCards()
    {
        // Clear existing cards
        foreach (Transform child in artifactsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string artifactId in _currentSoldier.required_artifacts)
        {
            ArtifactData artifact = ManifestLoader.Instance.GetArtifact(artifactId);
            if (artifact == null) continue;

            bool isCollected = _currentProgress.collected.Contains(artifactId);
            CreateArtifactCard(artifact, isCollected);
        }
    }

    private void CreateArtifactCard(ArtifactData artifact, bool isCollected)
    {
        if (artifactCardPrefab == null) return;

        GameObject card = Instantiate(artifactCardPrefab, artifactsContainer);
        var cardComponent = card.GetComponent<SoldierArtifactCard>();
        if (cardComponent != null)
        {
            cardComponent.Setup(artifact, isCollected);
        }
    }
}