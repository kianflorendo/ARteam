// ============================================================
// ScrollUIManager.cs
// Location: Assets/Scripts/UI/ScrollUIManager.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Object pool of 5 ScrollUI World Space Canvas instances.
// Forces each pooled canvas to World Space render mode at runtime
// to survive being instantiated under a Screen Space Overlay parent.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScrollUIManager : MonoBehaviour
{
    public static ScrollUIManager Instance { get; private set; }

    [Header("Pool")]
    public GameObject scrollUIPrefab;
    [Range(1, 10)] public int poolSize = 5;

    [Header("Layout")]
    public float offsetRight = 0f;     // metres along anchor.right (0 = centred on anchor)
    public float offsetUp = 0.15f;     // metres above anchor — floats above image / artifact

    private readonly List<GameObject> _pool = new();
    private readonly Dictionary<string, GameObject> _active = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (scrollUIPrefab == null)
        {
            Debug.LogError("[ScrollUIManager] scrollUIPrefab is not assigned.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(scrollUIPrefab);
            go.SetActive(false);
            ForceWorldSpace(go);
            _pool.Add(go);
        }
    }

    // ── Public API ────────────────────────────────────────────

    public void ShowScroll(ArtifactData artifact, Transform anchor)
    {
        if (artifact == null || anchor == null) return;

        // If already visible for this artifact, just re-position
        if (_active.TryGetValue(artifact.id, out var existing))
        {
            PositionScroll(existing.transform, anchor);
            return;
        }

        var go = GetFromPool();
        if (go == null)
        {
            Debug.LogWarning("[ScrollUIManager] Pool exhausted — no scroll available.");
            return;
        }

        PopulateScroll(go, artifact);
        PositionScroll(go.transform, anchor);

        // Wire ScrollFollowCamera to this anchor
        var follow = go.GetComponent<ScrollFollowCamera>();
        if (follow != null) follow.SetAnchor(anchor, offsetRight, offsetUp);

        ForceWorldSpace(go);
        go.SetActive(true);
        _active[artifact.id] = go;

        var animator = go.GetComponent<ScrollAnimator>();
        if (animator != null) animator.AnimateIn();

        AudioManager.Instance?.PlayScrollUnfurlSFX();
    }

    public void HideScroll(string artifactId)
    {
        if (!_active.TryGetValue(artifactId, out var go)) return;

        var animator = go.GetComponent<ScrollAnimator>();
        if (animator != null)
            StartCoroutine(HideAfterAnimation(artifactId, go, animator));
        else
            ReturnToPool(artifactId, go);
    }

    public void HideAllScrolls()
    {
        var ids = new List<string>(_active.Keys);
        foreach (var id in ids) HideScroll(id);
    }

    // ── Internal helpers ──────────────────────────────────────

    void PopulateScroll(GameObject go, ArtifactData artifact)
    {
        var scroll = artifact.scroll;

        // Paths match the actual ScrollUI.prefab hierarchy:
        // ScrollUI > ParchmentPanel > TitleText / CategoryBadgeText / DescriptionText / LocationHint / CollectButton
        SetTMP(go, "ParchmentPanel/TitleText",
            scroll != null ? scroll.title : artifact.name);

        SetTMP(go, "ParchmentPanel/CategoryBadgeText",
            scroll != null ? scroll.category : artifact.type);

        SetTMP(go, "ParchmentPanel/DescriptionText",
            scroll != null ? scroll.description : "");

        SetTMP(go, "ParchmentPanel/LocationHint",
            scroll != null ? scroll.location : "");

        // Collect button — visible only for collectible type
        var collectBtn = go.transform.Find("ParchmentPanel/CollectButton");
        if (collectBtn != null)
        {
            bool isCollectible = artifact.type == ArtifactType.Collectible;
            collectBtn.gameObject.SetActive(isCollectible);

            if (isCollectible)
            {
                // CollectionController lives as a singleton on MANAGERS — not on the prefab.
                // Tell the singleton which artifact this scroll belongs to, then wire the
                // button's onClick so pressing it calls the singleton's OnCollectPressed().
                CollectionController.Instance?.SetCurrentArtifact(artifact);

                var btn = collectBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                        CollectionController.Instance?.OnCollectPressed());
                }
            }
        }
    }

    void PositionScroll(Transform scrollTransform, Transform anchor)
    {
        scrollTransform.position = anchor.position
            + anchor.right * offsetRight
            + Vector3.up * offsetUp;

        // The prefab's children use "pixel" units (100–200) but the canvas
        // localScale is 1, making them 100–200 metres in world space.
        // Scale 0.002 maps 100px → 0.2 m, giving a palm-sized panel at arm's length.
        scrollTransform.localScale = Vector3.one * 0.002f;
    }

    // Forces the canvas (and any nested canvases) to World Space so the
    // scroll renders in AR space even when instantiated under a Screen Space
    // Overlay parent (Unity makes nested canvases inherit the parent mode
    // and marks them read-only in the Inspector — we override at runtime).
    void ForceWorldSpace(GameObject go)
    {
        foreach (var canvas in go.GetComponentsInChildren<Canvas>(true))
        {
            canvas.renderMode = RenderMode.WorldSpace;
            if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }
    }

    GameObject GetFromPool()
    {
        foreach (var go in _pool)
            if (!go.activeSelf) return go;
        return null;
    }

    void ReturnToPool(string artifactId, GameObject go)
    {
        _active.Remove(artifactId);
        go.SetActive(false);
        // Do NOT call _pool.Add here — the object was never removed from _pool
        // when retrieved (GetFromPool just finds the first inactive item).
        // Adding again would create duplicates that grow unbounded each cycle.
    }

    IEnumerator HideAfterAnimation(string artifactId, GameObject go, ScrollAnimator animator)
    {
        animator.AnimateOut();
        yield return new WaitForSeconds(animator.AnimationDuration);
        ReturnToPool(artifactId, go);
    }

    static void SetTMP(GameObject root, string path, string text)
    {
        var t = root.transform.Find(path);
        if (t == null) return;
        var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text ?? "";
    }
}
