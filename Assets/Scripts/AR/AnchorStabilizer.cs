using UnityEngine;

/// <summary>
/// Legacy placeholder kept for scene compatibility after removing geospatial GPS runtime logic.
/// </summary>
public class AnchorStabilizer : MonoBehaviour
{
    public static AnchorStabilizer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        enabled = false;
    }
}
