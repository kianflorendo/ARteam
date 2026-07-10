using System.Collections.Generic;
using UnityEngine;

public class CompletionDetector : MonoBehaviour
{
    public static CompletionDetector Instance { get; private set; }

    public static event System.Action<string> OnSoldierCompleted;
    public static event System.Action<string> OnDivisionCompleted;

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

    public void Check(string soldierId, string divisionId)
    {
        if (!string.IsNullOrEmpty(soldierId))
            CheckSoldier(soldierId);

        if (!string.IsNullOrEmpty(divisionId))
            CheckDivision(divisionId);
    }

    private void CheckSoldier(string soldierId)
    {
        if (InventoryManager.Instance == null
            || ManifestLoader.Instance == null) return;

        var progress = InventoryManager.Instance.GetSoldierProgress(soldierId);

        // Guard against duplicate badge generation if Check fires twice for the same set.
        if (progress.completed) return;

        var soldierData = ManifestLoader.Instance.GetSoldier(soldierId);
        if (soldierData == null) return;

        if (!IsSetComplete(soldierData.required_artifacts, progress.collected))
            return;

        InventoryManager.Instance.MarkSoldierComplete(soldierId);

        Debug.Log($"[CompletionDetector] SOLDIER SET COMPLETE: {soldierData.name}");

        AudioManager.Instance?.PlayCompletionFanfareSFX();

        OnSoldierCompleted?.Invoke(soldierId);

        TryGenerateBadge("soldier", soldierId, soldierData.token_badge);
    }

    private void CheckDivision(string divisionId)
    {
        if (InventoryManager.Instance == null
            || ManifestLoader.Instance == null) return;

        var progress = InventoryManager.Instance.GetDivisionProgress(divisionId);

        if (progress.completed) return;

        var divisionData = ManifestLoader.Instance.GetDivision(divisionId);
        if (divisionData == null) return;

        if (!IsSetComplete(divisionData.required_artifacts, progress.collected))
            return;

        InventoryManager.Instance.MarkDivisionComplete(divisionId);

        Debug.Log($"[CompletionDetector] DIVISION SET COMPLETE: {divisionData.name}");

        AudioManager.Instance?.PlayCompletionFanfareSFX();

        OnDivisionCompleted?.Invoke(divisionId);

        TryGenerateBadge("division", divisionId, divisionData.token_badge);
    }

    private void TryGenerateBadge(string type, string referenceId, BadgeConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning($"[CompletionDetector] No badge config for {type} {referenceId}");
            return;
        }

        // AFPTokenManager (Phase 9) will replace this stub with actual badge generation.
        Debug.Log($"[CompletionDetector] Badge ready to generate: " +
                  $"{config.badge_name} ({type}: {referenceId})");
    }

    private bool IsSetComplete(List<string> required, List<string> collected)
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
