// ============================================================
// CollectionController.cs
// Location: Assets/Scripts/Game/CollectionController.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// Handles the full collect sequence when player taps
// the Collect button on a ScrollUI parchment scroll.
//
// Sequence:
//   1. Validate not already collected
//   2. Play collect SFX
//   3. Play collect animation (artifact flies to HUD)
//   4. Wait for animation
//   5. Save to InventoryManager
//   6. AutoMatcher updates soldier + division progress
//   7. Hide the scroll
//   8. Show toast notification
//
// Attached to: CollectButton inside ScrollUI prefab
// Also registered as singleton on [MANAGERS] for
// direct calls from other scripts.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionController : MonoBehaviour
{
    // -- Singleton --
    public static CollectionController Instance { get; private set; }

    // -- Events --
    public static event System.Action<ArtifactData> OnArtifactCollected;
    public static event System.Action<string> OnCollectFailed;

    // -- Animation settings --
    [Header("Collect Animation")]
    [Tooltip("Duration of the collect fly-to-HUD animation")]
    public float collectAnimDuration = 0.8f;

    [Tooltip("Reference to the HUD token counter icon position (assign in Inspector)")]
    public RectTransform hudTargetIcon;

    // -- Toast settings --
    [Header("Toast")]
    [Tooltip("Optional toast text UI element for collect feedback")]
    public TextMeshProUGUI toastText;
    public float toastDuration = 2f;

    // -- Current artifact being collected --
    private ArtifactData _currentArtifact;
    private bool _isCollecting = false;

    // ============================================================
    //  Unity lifecycle
    // ============================================================

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

    // ============================================================
    //  Public API -- called by CollectButton.onClick in ScrollUI
    // ============================================================

    /// Sets the artifact reference before the collect button is shown.
    /// Called by ScrollUIManager when populating a scroll.
    public void SetCurrentArtifact(ArtifactData artifact)
    {
        _currentArtifact = artifact;
    }

    /// Called directly by the CollectButton onClick event in ScrollUI prefab.
    public void OnCollectPressed()
    {
        if (_currentArtifact == null)
        {
            Debug.LogWarning("[CollectionController] OnCollectPressed with no artifact set.");
            return;
        }

        if (_isCollecting)
        {
            Debug.Log("[CollectionController] Already collecting. Ignoring tap.");
            return;
        }

        if (InventoryManager.Instance != null
            && InventoryManager.Instance.IsCollected(_currentArtifact.id))
        {
            Debug.Log($"[CollectionController] {_currentArtifact.id} already collected.");
            OnCollectFailed?.Invoke(_currentArtifact.id);
            return;
        }

        StartCoroutine(CollectSequence(_currentArtifact));
    }

    /// Collect a specific artifact by data reference.
    /// Can be called directly from other scripts if needed.
    public void CollectArtifact(ArtifactData artifact)
    {
        if (artifact == null) return;
        _currentArtifact = artifact;
        OnCollectPressed();
    }

    // ============================================================
    //  Collect sequence
    // ============================================================

    private IEnumerator CollectSequence(ArtifactData artifact)
    {
        _isCollecting = true;
        Debug.Log($"[CollectionController] Collecting: {artifact.name}");

        // Step 1 -- Play collect SFX
        AudioManager.Instance?.PlayCollectSFX();

        // Step 2 -- Play collect animation (artifact flies to HUD icon)
        yield return StartCoroutine(PlayCollectAnimation(artifact));

        // Step 3 -- Save to inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.CollectArtifact(artifact.id);

        // Step 4 -- Update soldier + division progress via AutoMatcher
        if (AutoMatcher.Instance != null)
            AutoMatcher.Instance.Match(artifact);

        // Step 5 -- Hide the scroll
        if (ScrollUIManager.Instance != null)
            ScrollUIManager.Instance.HideScroll(artifact.id);

        // Step 6 -- Show toast notification
        ShowToast($"{artifact.name} collected!");

        // Step 7 -- Fire event for UI updates
        OnArtifactCollected?.Invoke(artifact);

        Debug.Log($"[CollectionController] Collect sequence complete: {artifact.name}");
        _isCollecting = false;
    }

    // ============================================================
    //  Collect animation -- artifact icon flies to HUD
    // ============================================================

    private IEnumerator PlayCollectAnimation(ArtifactData artifact)
    {
        // Basic collect animation -- scales down and fades
        // In Phase 12 this will be replaced with a full fly-to-HUD animation
        // For now: small delay to give the SFX time to play
        yield return new WaitForSeconds(collectAnimDuration);
    }

    // ============================================================
    //  Toast notification
    // ============================================================

    private void ShowToast(string message)
    {
        Debug.Log($"[CollectionController] Toast: {message}");

        if (toastText != null)
        {
            StopCoroutine(nameof(HideToastCoroutine));
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            StartCoroutine(HideToastCoroutine());
        }
    }

    private IEnumerator HideToastCoroutine()
    {
        yield return new WaitForSeconds(toastDuration);
        if (toastText != null)
            toastText.gameObject.SetActive(false);
    }

    public bool IsCollecting => _isCollecting;
}