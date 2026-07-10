using UnityEngine;

// Legacy scene-reference compatibility shim. GPS progression is now handled by OfflineGPSRouteManager.
public class GeospatialAnchorManager : MonoBehaviour
{
    public static GeospatialAnchorManager Instance { get; private set; }

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

    public bool IsSpawned(string artifactId) => false;

    public int GetSpawnedCount() => 0;

    public string GetAccuracyString()
    {
        return LocationServiceManager.Instance != null
            ? LocationServiceManager.Instance.GetStatusString()
            : "Offline GPS inactive";
    }
}
