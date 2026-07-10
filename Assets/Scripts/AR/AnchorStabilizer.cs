using UnityEngine;

// Legacy scene-reference compatibility shim. Geospatial GPS runtime logic has been removed.
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
