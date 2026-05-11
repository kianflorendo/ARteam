# GPS Artifact Floating Plane & Rotation Interaction — Design Spec
**Date:** 2026-04-30
**Project:** Mt. Samat AR — Terra App
**Status:** Approved for implementation

---

## Problem Statement

GPS artifacts currently spawn at camera position + 1m forward and appear statically — no
interaction possible. The user wants GPS artifacts to float on a virtual horizontal plane
at camera eye level and be rotatable by the user via single-finger drag (both Y-axis and
X-axis).

---

## Scope — What Changes

| File | Change |
|---|---|
| `Assets/Scripts/AR/GPSArtifactInteraction.cs` | **NEW** — isolated interaction component |
| `Assets/Scripts/AR/ArtifactSpawner.cs` | **1 line added** inside `SpawnCoroutine` |
| `Assets/Scripts/AR/OfflineGPSRouteManager.cs` | **1 field value changed** (`spawnHeightOffset`) |

### What is NOT touched
- `ImageAnchorManager.cs` — image tracking untouched
- `CollectionController.cs` — collect flow untouched
- `ScrollUIManager.cs` — scroll UI untouched
- `InventoryManager.cs` — inventory/progress untouched
- `AutoMatcher.cs` — badge matching untouched
- `CompletionDetector.cs` — completion detection untouched
- `DataModels.cs` / `manifest.json` — data untouched
- `ArtifactInstance.cs` — shared artifact component untouched

---

## Design

### Option A — Virtual Floating Plane at Camera Height

The artifact spawns on an invisible virtual horizontal plane whose Y position equals
`Camera.main.transform.position.y` at the moment of spawn. The object floats at eye level,
always visible, always facing the user initially, and does not move after spawn. No AR
plane detection required — fully offline compatible.

**Why not AR ground plane (ARPlaneManager):**
Mt. Samat is outdoor terrain (grass, stone paths, uneven ground). ARPlane detection
outdoors is unreliable and slow. Option A is instantaneous and always works.

---

## File 1 — `OfflineGPSRouteManager.cs` (1 field change)

```
BEFORE:  public float spawnHeightOffset = -0.3f;
AFTER:   public float spawnHeightOffset = 0f;
```

**Why:** The `-0.3f` offset was an arbitrary value that placed the object slightly below
camera height. Setting to `0f` makes the virtual plane sit exactly at camera eye level,
which is the correct Option A behaviour.

**Risk:** None. This is an Inspector-exposed field. The object spawns 0.3m higher than
before — still 1m in front of camera, now exactly at eye level.

---

## File 2 — `ArtifactSpawner.cs` (1 line added)

**Location:** Inside `SpawnCoroutine`, after `instance.Initialise(artifact, artifact.anchor_mode)`,
before `_spawnedArtifacts[artifact.id] = spawnedObject`.

```csharp
if (artifact.anchor_mode == AnchorMode.GPS)
    spawnedObject.AddComponent<GPSArtifactInteraction>();
```

**Why here:** The `spawnedObject` at this point is the fully instantiated, auto-scaled
3D prefab. `AnchorMode.GPS` is the existing constant from `DataModels.cs`. Image-tracked
artifacts (`AnchorMode.Image`) never enter this branch — zero risk to image tracking.

**Why `spawnedObject` not `anchorObject`:** The scroll UI follows `anchorTransform` (the
parent). Rotating `spawnedObject.localRotation` rotates only the 3D model relative to
the anchor. The anchor stays fixed; the scroll stays readable. Correct separation.

---

## File 3 — `GPSArtifactInteraction.cs` (new)

**Location:** `Assets/Scripts/AR/GPSArtifactInteraction.cs`

### Responsibilities
1. Add a `BoxCollider` on `Start()` sized from actual renderer bounds — enables raycasting
2. Detect single-finger touch that begins ON this artifact via `Physics.Raycast`
3. Skip touches over UI elements (collect button, scroll text) via `EventSystem.IsPointerOverGameObject`
4. Translate finger drag delta to X-axis and Y-axis rotation
5. Track `fingerId` to handle multi-touch correctly

### Rotation Behaviour
| Drag direction | Axis | Constraint |
|---|---|---|
| Left / Right | Y-axis | Unclamped — full 360° spin |
| Up / Down | X-axis | Clamped to [-60°, +60°] — prevents full flip |

Rotation applied as `transform.localRotation = Quaternion.Euler(_currentX, _currentY, 0f)`.
Z-axis always 0 — no roll.

### Inspector Fields
```csharp
public float rotationSensitivity = 0.3f;  // degrees per pixel
public float xClampMin = -60f;
public float xClampMax =  60f;
```

### BoxCollider Setup (Start)
- Reads `GetComponentsInChildren<Renderer>()` for world-space bounds
- Converts to local-space using `transform.InverseTransformPoint` (center) and
  `bounds.size / lossyScale` (size)
- Called in `Start()` not `Awake()` — guarantees auto-scale (5-frame wait in
  `ArtifactSpawner.SpawnCoroutine`) is complete before bounds are read
- Fallback: `col.size = Vector3.one * 1.2f` if no renderers found (matches `targetModelSize`)

### Touch Guard — UI Priority
Before beginning a rotation drag:
```csharp
if (EventSystem.current != null &&
    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
    return; // touch is on scroll UI — do not rotate
```
This ensures tapping the collect button or scrolling the parchment text never
accidentally triggers rotation.

### State Machine
```
IDLE
  ↓ TouchPhase.Began + raycast hits this artifact + not over UI
DRAGGING (fingerId locked)
  ↓ TouchPhase.Moved
Apply rotation delta to _currentX, _currentY
  ↓ TouchPhase.Ended / Canceled
IDLE
```

Only one finger tracked at a time. If a second finger begins while dragging, it is
ignored (first finger owns the drag until released).

---

## Integration Safety Analysis

| Concern | Analysis | Safe? |
|---|---|---|
| Scroll UI still shows | Scroll follows `anchorTransform` (parent). Rotation is on `spawnedObject` (child). Anchor never moves. | ✅ Yes |
| Collect button still works | Wired via Unity `EventSystem` (UI). `Physics.Raycast` is separate. `IsPointerOverGameObject` guard prevents conflict. | ✅ Yes |
| Image tracking unaffected | `AnchorMode.GPS` branch only. `ImageAnchorManager` path never touches `GPSArtifactInteraction`. | ✅ Yes |
| Inventory / progress | Collect flow unchanged. `InventoryManager.CollectArtifact()` called by `CollectionController` as before. | ✅ Yes |
| Multi-touch safety | `_draggingFingerId` lock ensures only the first artifact-hitting finger rotates. | ✅ Yes |
| Auto-scale compatibility | `BoxCollider` set up in `Start()`, after `SpawnCoroutine`'s 5-frame bounds wait. | ✅ Yes |
| Info-only GPS artifacts | `ArtifactSpawner` only reaches `AddComponent<GPSArtifactInteraction>()` inside the `ArtifactType.Collectible` block — info-only path is separate and untouched. | ✅ Yes |

---

## Testing Checklist

- [ ] GPS artifact spawns at exact camera eye level (no offset down)
- [ ] Artifact faces camera on spawn (existing behaviour preserved)
- [ ] Drag left/right → Y-axis rotation, full 360°, smooth
- [ ] Drag up/down → X-axis rotation, clamped to [-60°, +60°]
- [ ] Dragging beyond X clamp stops rotating, does not snap or flip
- [ ] Tapping collect button collects artifact correctly — rotation does NOT fire
- [ ] Scroll remains visible and readable while artifact is rotated
- [ ] Scrolling the scroll text (if implemented) does not rotate artifact
- [ ] Second finger on screen does not interfere with active rotation
- [ ] Image-tracked artifacts (INFO-TEST-001 to 005, A-001, A-003, A-004) have no rotation
- [ ] After collecting a GPS artifact, next GPS artifact spawns fresh with no residual rotation state

---

## Out of Scope (Not in This Plan)

- Floating bob / gentle up-down animation (future polish)
- Pinch-to-scale the artifact (future, Lean Touch+)
- Two-finger twist rotation (future, Lean Touch+)
- Resetting rotation to default on double-tap (future)
- Info-only GPS artifacts receiving rotation (not applicable — no 3D model)
