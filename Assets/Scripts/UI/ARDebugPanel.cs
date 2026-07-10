using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARDebugPanel : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI debugText;

    public bool IsVisible { get; private set; }

    private Image        _bg;
    private ArtifactData _current;

    private static readonly Color BG_VISIBLE = new Color(0.06f, 0.06f, 0.06f, 0.88f);
    private static readonly Color BG_HIDDEN  = Color.clear;

    private void Start()
    {
        _bg = GetComponent<Image>();
        if (_bg == null) _bg = gameObject.AddComponent<Image>();

        if (debugText == null)
            debugText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (debugText != null)
        {
            debugText.enableWordWrapping = true;
            debugText.overflowMode       = TextOverflowModes.Ellipsis;
            debugText.color              = Color.white;
        }

        SetVisible(false);

        ArtifactSpawner.OnArtifactHidden += HandleHidden;
    }

    private void OnDestroy()
    {
        ArtifactSpawner.OnArtifactHidden -= HandleHidden;
    }

    private void HandleHidden(string artifactId)
    {
        if (_current != null && _current.id == artifactId)
        {
            _current = null;
            SetVisible(false);
        }
    }

    public void ShowInfo(ArtifactData artifact)
    {
        if (artifact == null) return;
        _current = artifact;

        var scroll = artifact.scroll;
        var sb     = new StringBuilder();

        string title = !string.IsNullOrEmpty(scroll?.title) ? scroll.title : artifact.name;
        sb.AppendLine($"<b><size=15>{title.ToUpper()}</size></b>");

        if (!string.IsNullOrEmpty(scroll?.category))
            sb.AppendLine($"<size=11><color=#aaaaaa>{scroll.category}</color></size>");

        if (!string.IsNullOrEmpty(scroll?.location))
            sb.AppendLine($"<size=11><color=#aaaaaa>Location: {scroll.location}</color></size>");

        sb.AppendLine();

        if (!string.IsNullOrEmpty(scroll?.description))
            sb.AppendLine($"<size=12>{scroll.description}</size>");

        var specs = scroll?.specs;
        if (specs != null && specs.Count > 0)
        {
            sb.AppendLine();
            foreach (var spec in specs)
                sb.AppendLine(
                    $"<size=11><color=#aaaaaa>{spec.key}:</color>  " +
                    $"<color=#dddddd>{spec.value}</color></size>");
        }

        if (debugText != null)
            debugText.text = sb.ToString();

        SetVisible(true);
    }

    public void Hide()
    {
        _current = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (_bg      != null) _bg.color                          = visible ? BG_VISIBLE : BG_HIDDEN;
        if (debugText != null) debugText.gameObject.SetActive(visible);
    }
}
