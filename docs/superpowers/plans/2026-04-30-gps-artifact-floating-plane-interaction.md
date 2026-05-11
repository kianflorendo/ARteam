# GPS Artifact Floating Plane & Rotation Interaction — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** GPS artifacts float at camera eye level on a virtual horizontal plane and are rotatable via single-finger drag (Y-axis full 360°, X-axis clamped -60° to +60°) without breaking image tracking, collect flow, scroll UI, or inventory.

**Architecture:** Three isolated changes — one field value in `OfflineGPSRouteManager` sets spawn height to camera eye level; one line in `ArtifactSpawner.SpawnCoroutine` attaches a new component exclusively to GPS collectible prefabs; a new `GPSArtifactInteraction` MonoBehaviour owns all collider setup, touch detection, and rotation logic.

**Tech Stack:** Unity 6 (6000.4.0f1), C#, AR Foundation 6.4.1, UnityEngine.Physics (built-in), UnityEngine.EventSystems (built-in), Unity Test Framework (Play Mode)

**Spec:** `docs/superpowers/specs/2026-04-30-gps-artifact-floating-plane-interaction-design.md`

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/AR/OfflineGPSRouteManager.cs` | Modify line 19 | Spawn at camera eye level |
| `Assets/Scripts/AR/ArtifactSpawner.cs` | Modify lines 127–129 | Attach GPSArtifactInteraction to GPS collectibles |
| `Assets/Scripts/AR/GPSArtifactInteraction.cs` | **Create** | BoxCollider setup, touch detection, X+Y rotation |
| `Assets/Tests/PlayMode/PlayModeTests.asmdef` | **Create** | Unity test assembly definition |
| `Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs` | **Create** | Automated tests for collider and rotation state |

---

## Task 1: Fix GPS Spawn Height to Camera Eye Level

**Files:**
- Modify: `Assets/Scripts/AR/OfflineGPSRouteManager.cs:19`

- [ ] **Step 1.1 — Verify current value**

Confirm line 19 of `Assets/Scripts/AR/OfflineGPSRouteManager.cs` reads exactly:
```csharp
public float spawnHeightOffset = -0.3f;
```

- [ ] **Step 1.2 — Change to 0f**

Replace line 19 with:
```csharp
public float spawnHeightOffset = 0f;
```

The surrounding context (lines 16–20) must look like this after the change:
```csharp
[Header("Route Progression")]
public float routeCheckInterval = 0.15f;
public float defaultSpawnDistanceFromPlayer = 1f;
public float spawnHeightOffset = 0f;

private readonly Dictionary<string, GameObject> _presentationAnchors = ...
```

**Why:** `PresentArtifact()` (line ~309) computes the anchor Y as `Camera.main.transform.position.y + spawnHeightOffset`. With `0f` the virtual plane is exactly at camera eye level — Option A. The old `-0.3f` was arbitrary and placed the object below eye level.

- [ ] **Step 1.3 — Confirm Unity compiles**

Save the file. In Unity Editor Console: confirm zero compile errors.

- [ ] **Step 1.4 — Commit**

```bash
git add "Assets/Scripts/AR/OfflineGPSRouteManager.cs"
git commit -m "fix: set GPS spawn height to camera eye level (spawnHeightOffset 0f)"
```

---

## Task 2: Create GPSArtifactInteraction

**Files:**
- Create: `Assets/Scripts/AR/GPSArtifactInteraction.cs`

- [ ] **Step 2.1 — Create the file**

Create `Assets/Scripts/AR/GPSArtifactInteraction.cs` with this exact content:

```csharp
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
            ResetDrag();
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
```

- [ ] **Step 2.2 — Confirm Unity compiles**

Save the file. In Unity Editor Console: confirm zero compile errors and zero warnings from `GPSArtifactInteraction`.

- [ ] **Step 2.3 — Commit**

```bash
git add "Assets/Scripts/AR/GPSArtifactInteraction.cs"
git commit -m "feat: GPSArtifactInteraction — eye-level floating plane rotation for GPS artifacts"
```

---

## Task 3: Wire GPSArtifactInteraction in ArtifactSpawner

**Files:**
- Modify: `Assets/Scripts/AR/ArtifactSpawner.cs:127–129`

- [ ] **Step 3.1 — Locate the exact insertion point**

In `Assets/Scripts/AR/ArtifactSpawner.cs`, find lines 125–131:
```csharp
                    var instance = spawnedObject.GetComponent<ArtifactInstance>()
                                   ?? spawnedObject.AddComponent<ArtifactInstance>();
                    instance.Initialise(artifact, artifact.anchor_mode);

                    _spawnedArtifacts[artifact.id] = spawnedObject;
                    OnArtifactSpawned?.Invoke(instance);
                    Debug.Log($"[ArtifactSpawner] Spawned 3D artifact: {artifact.name}");
```

- [ ] **Step 3.2 — Insert the GPS interaction line**

Add one `if` block between `instance.Initialise(...)` and `_spawnedArtifacts[...] = spawnedObject`. The result must look exactly like this:

```csharp
                    var instance = spawnedObject.GetComponent<ArtifactInstance>()
                                   ?? spawnedObject.AddComponent<ArtifactInstance>();
                    instance.Initialise(artifact, artifact.anchor_mode);

                    if (artifact.anchor_mode == AnchorMode.GPS)
                        spawnedObject.AddComponent<GPSArtifactInteraction>();

                    _spawnedArtifacts[artifact.id] = spawnedObject;
                    OnArtifactSpawned?.Invoke(instance);
                    Debug.Log($"[ArtifactSpawner] Spawned 3D artifact: {artifact.name}");
```

**Key facts to confirm before saving:**
- `AnchorMode.GPS` is the constant `"gps"` from `DataModels.cs` — no magic strings
- This block is inside `if (prefabTask.Result != null)` — only runs when a 3D prefab loaded successfully
- The fallback path (`else` block below, for missing prefabs) is NOT touched
- The `else if (artifact.type == ArtifactType.InfoOnly)` block is NOT touched
- Image-tracked artifacts pass `AnchorMode.Image` (`"image"`) — they never enter this branch

- [ ] **Step 3.3 — Confirm Unity compiles**

Save the file. In Unity Editor Console: confirm zero compile errors.

- [ ] **Step 3.4 — Commit**

```bash
git add "Assets/Scripts/AR/ArtifactSpawner.cs"
git commit -m "feat: attach GPSArtifactInteraction to GPS collectible artifacts on spawn"
```

---

## Task 4: Create Play Mode Test Infrastructure and Tests

**Files:**
- Create: `Assets/Tests/PlayMode/PlayModeTests.asmdef`
- Create: `Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs`

> **Why Play Mode tests:** `GPSArtifactInteraction.Start()` runs only in Play Mode (Unity does not call `Start()` in Edit Mode tests unless you use `[UnityTest]`). Play Mode tests run the scene loop, so `Start()` fires normally after `AddComponent`.

- [ ] **Step 4.1 — Create the test directory and assembly definition**

Create directory `Assets/Tests/PlayMode/` then create `Assets/Tests/PlayMode/PlayModeTests.asmdef` with this exact content:

```json
{
    "name": "PlayModeTests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4.2 — Create the test file**

Create `Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs` with this exact content:

```csharp
// ============================================================
// GPSArtifactInteractionTests.cs
// Location: Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs
// Play Mode tests for GPSArtifactInteraction.
// Run via: Window → General → Test Runner → PlayMode → Run All
// ============================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GPSArtifactInteractionTests
{
    // ── Collider setup ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator EnsureCollider_AddsBoxCollider_WhenNoColliderPresent()
    {
        var go = new GameObject("TestArtifact_NoCollider");
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        Assert.IsNotNull(
            go.GetComponent<BoxCollider>(),
            "GPSArtifactInteraction.Start() must add a BoxCollider when none is present.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator EnsureCollider_DoesNotAddDuplicate_WhenColliderAlreadyExists()
    {
        var go = new GameObject("TestArtifact_ExistingCollider");
        go.AddComponent<BoxCollider>();                  // pre-existing collider
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        var colliders = go.GetComponents<Collider>();
        Assert.AreEqual(
            1,
            colliders.Length,
            "GPSArtifactInteraction must not add a second Collider when one already exists.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator EnsureCollider_UsesFallbackSize_WhenNoRenderersPresent()
    {
        var go = new GameObject("TestArtifact_NoRenderer"); // no MeshRenderer
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        var col = go.GetComponent<BoxCollider>();
        Assert.IsNotNull(col, "BoxCollider must be added even when no Renderer is present.");
        Assert.AreEqual(
            new Vector3(1.2f, 1.2f, 1.2f),
            col.size,
            "Fallback BoxCollider size must be Vector3.one * 1.2f (matches ArtifactSpawner.targetModelSize).");

        Object.Destroy(go);
    }

    // ── Initial rotation state ────────────────────────────────────

    [UnityTest]
    public IEnumerator Start_PreservesIdentityRotation_WhenSpawnedWithNoRotation()
    {
        var go = new GameObject("TestArtifact_ZeroRotation");
        // Spawn with identity rotation — same as ArtifactSpawner does
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        // Start() reads localEulerAngles but does NOT write localRotation —
        // rotation only changes when the user drags. Identity must be preserved.
        Assert.AreEqual(
            Quaternion.identity,
            go.transform.localRotation,
            "Start() must not modify localRotation — initial rotation must remain Quaternion.identity.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Start_PreservesNonZeroRotation_WhenSpawnedWithExistingRotation()
    {
        var go = new GameObject("TestArtifact_NonZeroRotation");
        var spawnRotation = Quaternion.Euler(0f, 180f, 0f); // ArtifactSpawner sets 180° Y
        go.transform.localRotation = spawnRotation;
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        Assert.AreEqual(
            spawnRotation,
            go.transform.localRotation,
            "Start() must not modify localRotation — spawn rotation must be preserved.");

        Object.Destroy(go);
    }
}
```

- [ ] **Step 4.3 — Confirm Unity recognises the test assembly**

In Unity Editor:
1. Wait for asset refresh (bottom-right spinner stops)
2. Open Window → General → Test Runner
3. Switch to **PlayMode** tab
4. Confirm `GPSArtifactInteractionTests` appears in the test list with 5 tests

If the test list is empty, check the Console for asmdef errors.

- [ ] **Step 4.4 — Run all tests and confirm they pass**

In Test Runner (PlayMode tab): click **Run All**.

Expected result — all 5 tests pass:
```
✓ EnsureCollider_AddsBoxCollider_WhenNoColliderPresent
✓ EnsureCollider_DoesNotAddDuplicate_WhenColliderAlreadyExists
✓ EnsureCollider_UsesFallbackSize_WhenNoRenderersPresent
✓ Start_PreservesIdentityRotation_WhenSpawnedWithNoRotation
✓ Start_PreservesNonZeroRotation_WhenSpawnedWithExistingRotation
```

If any test fails, fix `GPSArtifactInteraction.cs` before proceeding. Do not continue to Task 5 with failing tests.

- [ ] **Step 4.5 — Commit**

```bash
git add "Assets/Tests/PlayMode/PlayModeTests.asmdef" "Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs"
git commit -m "test: play mode tests for GPSArtifactInteraction collider and rotation state"
```

---

## Task 5: Manual On-Device Verification

> `Input.GetTouch`, `Physics.Raycast`, and AR camera feed require a physical Android device. These checks cannot be automated in Unity Test Runner without a dedicated input simulation framework.

**Build steps:** File → Build Settings → Android → select your connected device → Build and Run.

**Prerequisites:** GPS-TEST-001 (Bolo Knife, sequence_index=1, 1m walk) must be active in `manifest.json` — it is.

### 5A — Spawn and placement

- [ ] **5A.1 — GPS artifact spawns at camera eye level**

Walk 1m from your starting position to unlock GPS-TEST-001.
**Expected:** Artifact appears directly in front of you at eye level. You do not need to look down to see it.

- [ ] **5A.2 — Artifact faces the camera on spawn**

On appearance, the artifact's front face is toward you.
**Expected:** You see the front of the model, not its back or side.

### 5B — Rotation

- [ ] **5B.1 — Y-axis rotation with left/right drag**

Place one finger on the 3D artifact model and drag left. Then drag right.
**Expected:** Artifact spins smoothly on its Y-axis. Full 360° is possible. No jump or snap when changing direction.

- [ ] **5B.2 — X-axis rotation with up/down drag**

Place one finger on the artifact and drag upward. Then drag downward.
**Expected:** Artifact tilts on X-axis. Motion stops at approximately -60° (tilted back) and +60° (tilted forward). The artifact does NOT flip upside-down.

- [ ] **5B.3 — Dragging on empty space does not rotate**

Place a finger on empty screen space (not on the artifact) and drag in all directions.
**Expected:** The artifact does not rotate.

### 5C — Collect button and scroll safety

- [ ] **5C.1 — Collect button collects correctly — no rotation**

While GPS-TEST-001 is visible, tap the green **Collect** button on the parchment scroll.
**Expected:** Artifact is collected (toast "Bolo Knife collected!" appears, scroll hides, inventory updated). The artifact does NOT rotate when tapping the button.

- [ ] **5C.2 — Scroll remains readable while rotating**

Rotate the artifact in any direction while the parchment scroll is visible beside it.
**Expected:** The scroll stays stationary and fully readable. Only the 3D model rotates.

### 5D — Multi-touch and session safety

- [ ] **5D.1 — Second finger does not interfere with rotation**

While dragging with one finger to rotate, place a second finger anywhere on screen.
**Expected:** Rotation continues smoothly driven by the original finger. Second finger has no effect.

- [ ] **5D.2 — Image-tracked artifacts have no rotation**

Point the camera at any test image marker (INFO-TEST-001 through INFO-TEST-005).
**Expected:** No `GPSArtifactInteraction` component on image-tracked artifacts. Touching the spawned scroll area does not trigger rotation.

- [ ] **5D.3 — Next GPS artifact spawns with clean rotation**

After collecting GPS-TEST-001, walk 5m to unlock GPS-TEST-002 (M2 60mm Mortar).
**Expected:** New artifact spawns fresh with default orientation (facing camera). No residual rotation from GPS-TEST-001.

---

## Spec Coverage Verification

| Spec requirement | Implemented in |
|---|---|
| Virtual plane at camera eye level | Task 1 — `spawnHeightOffset = 0f` |
| Object stays fixed after spawn | Existing anchor system — unchanged |
| Y-axis rotation, full 360° | Task 2 — `_currentY += delta.x * sensitivity` (unclamped) |
| X-axis rotation, clamped -60° to +60° | Task 2 — `Mathf.Clamp(_currentX, xClampMin, xClampMax)` |
| Single-finger drag only | Task 2 — `_draggingFingerId` lock |
| Touch must start on artifact | Task 2 — `Physics.Raycast` + `hit.transform.IsChildOf(transform)` |
| Collect button safe — no rotation | Task 2 — `EventSystem.IsPointerOverGameObject` guard |
| Scroll UI unaffected | Rotation on `spawnedObject` (child); scroll follows `anchorTransform` (parent) |
| Image tracking unaffected | `AnchorMode.GPS` branch only in `ArtifactSpawner` |
| Info-only GPS artifacts unaffected | `AddComponent` inside `ArtifactType.Collectible` + `prefabTask.Result != null` block only |
| BoxCollider from renderer bounds | Task 2 — `EnsureCollider()` in `Start()` |
| BoxCollider fallback 1.2f | Task 2 — `col.size = Vector3.one * 1.2f` |
| No duplicate collider | Task 2 — `GetComponentInChildren<Collider>() != null` guard |
