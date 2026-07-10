using UnityEngine;

public class ArtifactInstance : MonoBehaviour
{
    public ArtifactData ArtifactData { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public bool IsCollected { get; private set; } = false;
    public string AnchorType { get; private set; }

    public void Initialise(ArtifactData data, string anchorType)
    {
        ArtifactData = data;
        AnchorType = anchorType;
        IsVisible = true;
        IsCollected = InventoryManager.Instance != null
                       && InventoryManager.Instance.IsCollected(data.id);

        Debug.Log($"[ArtifactInstance] Spawned: {data.name} " +
                  $"(type={data.type}, anchor={anchorType}, collected={IsCollected})");
    }

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

    public void MarkAsCollected()
    {
        IsCollected = true;
    }
}
