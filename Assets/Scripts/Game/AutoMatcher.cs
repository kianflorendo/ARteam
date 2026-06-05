// ============================================================
// AutoMatcher.cs
// Location: Assets/Scripts/Game/AutoMatcher.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// Called immediately after collect is confirmed.
// Updates soldier progress AND division progress
// simultaneously from a single artifact collect.
// Then calls CompletionDetector to check if any
// set is now complete.
//
// Called by: CollectionController.cs after saving to inventory
// ============================================================

using UnityEngine;

public class AutoMatcher : MonoBehaviour
{
    // -- Singleton --
    public static AutoMatcher Instance { get; private set; }

    // -- Events -- fired after each progress update
    public static event System.Action<string> OnSoldierProgressUpdated;  // soldierId
    public static event System.Action<string> OnDivisionProgressUpdated; // divisionId

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
    //  Match -- called by CollectionController after collect
    // ============================================================

    /// Updates soldier and division progress for this artifact.
    /// Both are updated in the same call -- one artifact can
    /// contribute to both a soldier set AND a division set.
    public void Match(ArtifactData artifact)
    {
        if (artifact == null)
        {
            Debug.LogWarning("[AutoMatcher] Match called with null artifact.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[AutoMatcher] InventoryManager not found.");
            return;
        }

        // -- Update primary soldier progress --
        if (!string.IsNullOrEmpty(artifact.soldier_id))
        {
            InventoryManager.Instance.AddToSoldierProgress(
                artifact.soldier_id, artifact.id);
            OnSoldierProgressUpdated?.Invoke(artifact.soldier_id);
            Debug.Log($"[AutoMatcher] Soldier {artifact.soldier_id} updated " +
                      $"with artifact {artifact.id}");
        }

        // -- Update shared soldier progress --
        // Shared artifacts count towards multiple soldier collections simultaneously.
        if (artifact.shared_soldier_ids != null)
        {
            foreach (var sharedId in artifact.shared_soldier_ids)
            {
                if (string.IsNullOrEmpty(sharedId)) continue;
                InventoryManager.Instance.AddToSoldierProgress(sharedId, artifact.id);
                OnSoldierProgressUpdated?.Invoke(sharedId);
                Debug.Log($"[AutoMatcher] Shared soldier {sharedId} updated " +
                          $"with artifact {artifact.id}");
            }
        }

        // -- Update division progress --
        if (!string.IsNullOrEmpty(artifact.division_id))
        {
            InventoryManager.Instance.AddToDivisionProgress(
                artifact.division_id, artifact.id);
            OnDivisionProgressUpdated?.Invoke(artifact.division_id);
            Debug.Log($"[AutoMatcher] Division {artifact.division_id} updated " +
                      $"with artifact {artifact.id}");
        }

        // -- Check completion for primary soldier + division --
        if (CompletionDetector.Instance != null)
        {
            CompletionDetector.Instance.Check(artifact.soldier_id, artifact.division_id);

            // Check completion for each shared soldier too
            if (artifact.shared_soldier_ids != null)
            {
                foreach (var sharedId in artifact.shared_soldier_ids)
                {
                    if (!string.IsNullOrEmpty(sharedId))
                        CompletionDetector.Instance.Check(sharedId, "");
                }
            }
        }
    }
}