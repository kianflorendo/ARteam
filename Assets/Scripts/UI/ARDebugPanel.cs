// ============================================================
// ARDebugPanel.cs
// Location: Assets/Scripts/UI/ARDebugPanel.cs
// Mt. Samat AR — Artifact Info Panel.
//
// Replaces the old debug-info display with a clean artifact
// description card that appears the moment a GPS or image-tracked
// artifact becomes visible in AR, and disappears when hidden.
//
// The same [SerializeField] debugText reference is kept so
// existing prefab wiring does not need to change.
// ============================================================

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARDebugPanel : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI debugText;   // existing wired TMP — repurposed for artifact info

    private Image        _bg;
    private ArtifactData _current;

    // ── Colours ──────────────────────────────────────────────
    private static readonly Color BG_VISIBLE = new Color(0.06f, 0.06f, 0.06f, 0.88f);
    private static readonly Color BG_HIDDEN  = Color.clear;

    // ─────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Grab or create the background Image on this GameObject
        _bg = GetComponent<Image>();
        if (_bg == null) _bg = gameObject.AddComponent<Image>();

        // Configure text for wrapping and overflow
        if (debugText != null)
        {
            debugText.enableWordWrapping = true;
            debugText.overflowMode       = TextOverflowModes.Ellipsis;
            debugText.color              = Color.white;
        }

        SetVisible(false);

        ArtifactSpawner.OnArtifactSpawned      += HandleSpawned;
        ArtifactSpawner.OnArtifactModelVisible  += HandleModelVisible;
        ArtifactSpawner.OnArtifactHidden        += HandleHidden;
    }

    private void OnDestroy()
    {
        ArtifactSpawner.OnArtifactSpawned      -= HandleSpawned;
        ArtifactSpawner.OnArtifactModelVisible  -= HandleModelVisible;
        ArtifactSpawner.OnArtifactHidden        -= HandleHidden;
    }

    // ─────────────────────────────────────────────────────────
    //  Event handlers
    // ─────────────────────────────────────────────────────────

    private void HandleSpawned(ArtifactInstance instance)
    {
        // Show info immediately — user can read while 3D model finishes loading
        if (instance != null)
            ShowInfo(instance.ArtifactData);
    }

    private void HandleModelVisible(string artifactId)
    {
        // If we already have this artifact's info showing, nothing to do.
        // If not yet shown (edge case), pull data from manifest.
        if (_current != null && _current.id == artifactId) return;

        var data = ManifestLoader.Instance?.GetArtifact(artifactId);
        if (data != null) ShowInfo(data);
    }

    private void HandleHidden(string artifactId)
    {
        if (_current != null && _current.id == artifactId)
        {
            _current = null;
            SetVisible(false);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Display
    // ─────────────────────────────────────────────────────────

    private void ShowInfo(ArtifactData artifact)
    {
        if (artifact == null) return;
        _current = artifact;

        var scroll = artifact.scroll;
        var sb     = new StringBuilder();

        // ── Title ────────────────────────────────────────────
        string title = !string.IsNullOrEmpty(scroll?.title)
            ? scroll.title
            : artifact.name;
        sb.AppendLine($"<b><size=15>{title.ToUpper()}</size></b>");

        // ── Meta row (category / location) ───────────────────
        if (!string.IsNullOrEmpty(scroll?.category))
            sb.AppendLine($"<size=11><color=#aaaaaa>{scroll.category}</color></size>");

        if (!string.IsNullOrEmpty(scroll?.location))
            sb.AppendLine($"<size=11><color=#aaaaaa>Location: {scroll.location}</color></size>");

        sb.AppendLine();

        // ── Description ───────────────────────────────────────
        if (!string.IsNullOrEmpty(scroll?.description))
            sb.AppendLine($"<size=12>{scroll.description}</size>");

        // ── Specs ─────────────────────────────────────────────
        var specs = scroll?.specs?.Items;
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

    private void SetVisible(bool visible)
    {
        if (_bg      != null) _bg.color                            = visible ? BG_VISIBLE : BG_HIDDEN;
        if (debugText != null) debugText.gameObject.SetActive(visible);
    }
}
