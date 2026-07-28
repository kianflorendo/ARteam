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
    private RectTransform _rt;
    private DeviceTilt?    _currentTilt; // null = not yet applied

    private static readonly Color BG_VISIBLE = new Color(0.06f, 0.06f, 0.06f, 0.88f);
    private static readonly Color BG_HIDDEN  = Color.clear;

    // Portrait: bottom sheet, full DesignRoot width, above the ActionPanel.
    private static readonly Vector2 PORTRAIT_ANCHOR_MIN = new Vector2(0f, 0f);
    private static readonly Vector2 PORTRAIT_ANCHOR_MAX = new Vector2(1f, 0f);
    private static readonly Vector2 PORTRAIT_PIVOT       = new Vector2(0.5f, 0f);
    private static readonly Vector2 PORTRAIT_SIZE_DELTA  = new Vector2(-20f, 220f);
    private static readonly Vector2 PORTRAIT_ANCHORED_POS = new Vector2(0f, 145f);

    // Tilted: rotated in place to a right-edge vertical strip. The canvas itself
    // stays fixed-portrait (see DeviceTiltHelper for why); sizeDelta.x becomes
    // the panel's visual length after the 90-degree rotation, sizeDelta.y its
    // visual thickness sticking out from the edge.
    private static readonly Vector2 TILT_ANCHOR_MIN = new Vector2(1f, 0.5f);
    private static readonly Vector2 TILT_ANCHOR_MAX = new Vector2(1f, 0.5f);
    private static readonly Vector2 TILT_PIVOT       = new Vector2(1f, 0.5f);
    private static readonly Vector2 TILT_SIZE_DELTA  = new Vector2(500f, 220f);
    private static readonly Vector2 TILT_ANCHORED_POS = new Vector2(-10f, 0f);

    private void Start()
    {
        _rt = GetComponent<RectTransform>();

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
        ApplyOrientationLayout(force: true);

        ArtifactSpawner.OnArtifactHidden += HandleHidden;
    }

    private void Update()
    {
        ApplyOrientationLayout(force: false);
    }

    private void ApplyOrientationLayout(bool force)
    {
        if (_rt == null) return;

        var tilt = DeviceTiltHelper.GetTilt();
        if (!force && _currentTilt.HasValue && _currentTilt.Value == tilt) return;

        _currentTilt = tilt;
        bool isTilted = tilt != DeviceTilt.Portrait;

        _rt.anchorMin        = isTilted ? TILT_ANCHOR_MIN     : PORTRAIT_ANCHOR_MIN;
        _rt.anchorMax        = isTilted ? TILT_ANCHOR_MAX     : PORTRAIT_ANCHOR_MAX;
        _rt.pivot            = isTilted ? TILT_PIVOT          : PORTRAIT_PIVOT;
        _rt.sizeDelta        = isTilted ? TILT_SIZE_DELTA     : PORTRAIT_SIZE_DELTA;
        _rt.anchoredPosition = isTilted ? TILT_ANCHORED_POS   : PORTRAIT_ANCHORED_POS;

        _rt.localRotation = tilt switch
        {
            DeviceTilt.TiltLeft  => Quaternion.Euler(0f, 0f, 90f),
            DeviceTilt.TiltRight => Quaternion.Euler(0f, 0f, -90f),
            _                    => Quaternion.identity,
        };
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
