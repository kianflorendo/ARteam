// ============================================================
// ArtifactInstance.cs
// Location: Assets/Scripts/AR/ArtifactInstance.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// Component attached to every spawned AR artifact GameObject.
// Holds a reference to the artifact's data from manifest.json
// so any script can read artifact info from the spawned object.
// Created and attached by ArtifactSpawner.cs on spawn.
// ============================================================

using UnityEngine;

public class ArtifactInstance : MonoBehaviour
{
    // -- Artifact data reference --
    public ArtifactData ArtifactData { get; private set; }

    // -- State --
    public bool IsVisible { get; private set; } = true;
    public bool IsCollected { get; private set; } = false;

    // -- Spawn anchor type for reference --
    public string AnchorType { get; private set; } // "image" or "gps"

    // ============================================================
    //  Initialise -- called by ArtifactSpawner immediately after
    //  instantiating this prefab
    // ============================================================

    public void Initialise(ArtifactData data, string anchorType)
    {
        ArtifactData = data;
        AnchorType = anchorType;
        IsVisible = true;
        IsCollected = InventoryManager.Instance != null
                       && InventoryManager.Instance.IsCollected(data.id);

        Debug.Log($"[ArtifactInstance] Spawned: {data.name} " +
                  $"(type={data.type}, anchor={anchorType}, " +
                  $"collected={IsCollected})");
    }

    // ============================================================
    //  Visibility control
    //  Called by ImageAnchorManager when tracking is lost/found
    // ============================================================

    public void Hide()
    {
        IsVisible = false;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        IsVisible = true;
        gameObject.SetActive(true);
    }

    // ============================================================
    //  Called by CollectionController after collect animation
    // ============================================================

    public void MarkAsCollected()
    {
        IsCollected = true;
    }
}