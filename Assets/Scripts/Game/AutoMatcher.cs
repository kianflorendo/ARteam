using UnityEngine;

public class AutoMatcher : MonoBehaviour
{
    public static AutoMatcher Instance { get; private set; }

    public static event System.Action<string> OnSoldierProgressUpdated;
    public static event System.Action<string> OnDivisionProgressUpdated;

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

        if (!string.IsNullOrEmpty(artifact.soldier_id))
        {
            InventoryManager.Instance.AddToSoldierProgress(artifact.soldier_id, artifact.id);
            OnSoldierProgressUpdated?.Invoke(artifact.soldier_id);
            Debug.Log($"[AutoMatcher] Soldier {artifact.soldier_id} updated with artifact {artifact.id}");
        }

        if (artifact.shared_soldier_ids != null)
        {
            foreach (var sharedId in artifact.shared_soldier_ids)
            {
                if (string.IsNullOrEmpty(sharedId)) continue;
                InventoryManager.Instance.AddToSoldierProgress(sharedId, artifact.id);
                OnSoldierProgressUpdated?.Invoke(sharedId);
                Debug.Log($"[AutoMatcher] Shared soldier {sharedId} updated with artifact {artifact.id}");
            }
        }

        if (!string.IsNullOrEmpty(artifact.division_id))
        {
            InventoryManager.Instance.AddToDivisionProgress(artifact.division_id, artifact.id);
            OnDivisionProgressUpdated?.Invoke(artifact.division_id);
            Debug.Log($"[AutoMatcher] Division {artifact.division_id} updated with artifact {artifact.id}");
        }

        if (CompletionDetector.Instance != null)
        {
            CompletionDetector.Instance.Check(artifact.soldier_id, artifact.division_id);

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
