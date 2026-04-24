// ============================================================
// CompletionDetector.cs
// Location: Assets/Scripts/Game/CompletionDetector.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// Called by AutoMatcher after every artifact collection.
// Checks if a soldier set or division set is now complete.
//
// CRITICAL GUARD: checks !progress.completed before triggering
// to prevent duplicate badge generation if collect fires twice.
//
// On completion:
//   1. Marks set as complete in InventoryManager
//   2. Plays completion fanfare SFX
//   3. Calls AFPTokenManager to generate badge (Phase 9)
//
// Called by: AutoMatcher.cs
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class CompletionDetector : MonoBehaviour
{
    // -- Singleton --
    public static CompletionDetector Instance { get; private set; }

    // -- Events --
    public static event System.Action<string> OnSoldierCompleted;  // soldierId
    public static event System.Action<string> OnDivisionCompleted; // divisionId

    // ============================================================
    //  Unity lifecycle
    // ============================================================

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

    // ============================================================
    //  Check -- called by AutoMatcher after every collect
    // ============================================================

    /// Checks if the soldier set or division set is now complete.
    /// Either or both IDs may be empty -- handles both cases safely.
    public void Check(string soldierId, string divisionId)
    {
        if (!string.IsNullOrEmpty(soldierId))
            CheckSoldier(soldierId);

        if (!string.IsNullOrEmpty(divisionId))
            CheckDivision(divisionId);
    }

    // ============================================================
    //  Soldier completion check
    // ============================================================

    private void CheckSoldier(string soldierId)
    {
        if (InventoryManager.Instance == null
            || ManifestLoader.Instance == null) return;

        var progress = InventoryManager.Instance.GetSoldierProgress(soldierId);

        // Guard -- already completed, skip to prevent duplicate badge
        if (progress.completed) return;

        var soldierData = ManifestLoader.Instance.GetSoldier(soldierId);
        if (soldierData == null) return;

        // Check if all required artifacts are collected
        if (!IsSetComplete(soldierData.required_artifacts, progress.collected))
            return;

        // Set is complete -- mark it
        InventoryManager.Instance.MarkSoldierComplete(soldierId);

        Debug.Log($"[CompletionDetector] SOLDIER SET COMPLETE: {soldierData.name}");

        // Play completion fanfare
        AudioManager.Instance?.PlayCompletionFanfareSFX();

        // Fire event for UI (Soldier screen glows, completion animation)
        OnSoldierCompleted?.Invoke(soldierId);

        // Generate AFP token badge (Phase 9 -- stubbed until AFPTokenManager exists)
        TryGenerateBadge("soldier", soldierId, soldierData.token_badge);
    }

    // ============================================================
    //  Division completion check
    // ============================================================

    private void CheckDivision(string divisionId)
    {
        if (InventoryManager.Instance == null
            || ManifestLoader.Instance == null) return;

        var progress = InventoryManager.Instance.GetDivisionProgress(divisionId);

        // Guard -- already completed
        if (progress.completed) return;

        var divisionData = ManifestLoader.Instance.GetDivision(divisionId);
        if (divisionData == null) return;

        if (!IsSetComplete(divisionData.required_artifacts, progress.collected))
            return;

        InventoryManager.Instance.MarkDivisionComplete(divisionId);

        Debug.Log($"[CompletionDetector] DIVISION SET COMPLETE: {divisionData.name}");

        // Play completion fanfare
        AudioManager.Instance?.PlayCompletionFanfareSFX();

        // Fire event for UI (Division emblem unlocks)
        OnDivisionCompleted?.Invoke(divisionId);

        // Generate AFP token badge
        TryGenerateBadge("division", divisionId, divisionData.token_badge);
    }

    // ============================================================
    //  Badge generation -- stub until AFPTokenManager (Phase 9)
    // ============================================================

    private void TryGenerateBadge(string type, string referenceId, BadgeConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning($"[CompletionDetector] No badge config for {type} {referenceId}");
            return;
        }

        // AFPTokenManager is implemented in Phase 9.
        // This will call AFPTokenManager.Instance.GenerateBadge() then.
        Debug.Log($"[CompletionDetector] Badge ready to generate: " +
                  $"{config.badge_name} ({type}: {referenceId}) -- " +
                  "AFPTokenManager will handle this in Phase 9.");
    }

    // ============================================================
    //  Helper -- checks if all required artifacts are collected
    // ============================================================

    private bool IsSetComplete(
        List<string> required,
        List<string> collected)
    {
        if (required == null || required.Count == 0) return false;
        if (collected == null) return false;

        foreach (var artifactId in required)
        {
            if (!collected.Contains(artifactId))
                return false;
        }
        return true;
    }
}