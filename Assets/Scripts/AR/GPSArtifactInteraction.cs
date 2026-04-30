// ============================================================
// GPSArtifactInteraction.cs
// Location: Assets/Scripts/AR/GPSArtifactInteraction.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Attached by ArtifactSpawner to GPS collectible prefabs only.
// Adds a BoxCollider for raycasting, then handles single-finger
// touch drag to rotate the 3D model on Y-axis (full 360°) and
// X-axis (clamped -60° to +60°).
//
// Does NOT affect image-tracked artifacts.
// Does NOT move the parent anchor — scroll UI is unaffected.
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public class GPSArtifactInteraction : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSensitivity = 0.3f;
    public float xClampMin = -60f;
    public float xClampMax = 60f;

    private float _currentX;
    private float _currentY;
    private bool _isDragging;
    private int _draggingFingerId = -1;

    private void Start()
    {
        EnsureCollider();

        // Capture initial local rotation so first drag continues from spawn orientation.
        // Convert from Unity's 0-360 euler range to -180..180 for correct clamping.
        var euler = transform.localEulerAngles;
        _currentX = euler.x > 180f ? euler.x - 360f : euler.x;
        _currentY = euler.y;
    }

    private void Update()
    {
        if (Camera.main == null || Input.touchCount == 0)
        {
            if (_isDragging) ResetDrag();
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!_isDragging)
                        TryBeginDrag(touch);
                    break;

                case TouchPhase.Moved:
                    if (_isDragging && touch.fingerId == _draggingFingerId)
                        ApplyRotation(touch.deltaPosition);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == _draggingFingerId)
                        ResetDrag();
                    break;
            }
        }
    }

    // ── Collider ─────────────────────────────────────────────────

    private void EnsureCollider()
    {
        // Skip if the prefab already ships with a collider.
        if (GetComponentInChildren<Collider>() != null)
            return;

        var col = gameObject.AddComponent<BoxCollider>();
        var renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            // Compute world-space encapsulating bounds across all renderers.
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Convert world-space bounds to local-space for the BoxCollider.
            col.center = transform.InverseTransformPoint(bounds.center);
            Vector3 ls = transform.lossyScale;
            col.size = new Vector3(
                bounds.size.x / Mathf.Abs(ls.x),
                bounds.size.y / Mathf.Abs(ls.y),
                bounds.size.z / Mathf.Abs(ls.z)
            );
        }
        else
        {
            // Fallback: no renderers found — use ArtifactSpawner.targetModelSize (1.2m).
            col.size = Vector3.one * 1.2f;
        }
    }

    // ── Touch detection ──────────────────────────────────────────

    private void TryBeginDrag(Touch touch)
    {
        // Guard: if the touch is over any UI element (scroll canvas, collect button,
        // nav bar), skip — do not rotate. EventSystem handles UI touches separately.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        // Only begin rotating if the touch actually hit this artifact's collider.
        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                _isDragging = true;
                _draggingFingerId = touch.fingerId;
            }
        }
    }

    // ── Rotation ─────────────────────────────────────────────────

    private void ApplyRotation(Vector2 delta)
    {
        // Horizontal drag → Y-axis rotation (spin). Unclamped — full 360°.
        _currentY += delta.x * rotationSensitivity;

        // Vertical drag → X-axis rotation (tilt). Clamped to prevent full flip.
        _currentX -= delta.y * rotationSensitivity;
        _currentX = Mathf.Clamp(_currentX, xClampMin, xClampMax);

        // Apply to localRotation — rotates the 3D model relative to the anchor.
        // The anchor (parent) stays fixed; the scroll UI following the anchor is unaffected.
        transform.localRotation = Quaternion.Euler(_currentX, _currentY, 0f);
    }

    private void ResetDrag()
    {
        _isDragging = false;
        _draggingFingerId = -1;
    }
}
