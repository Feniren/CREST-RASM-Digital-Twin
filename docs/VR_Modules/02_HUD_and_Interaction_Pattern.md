# Lesson HUD & Interaction Pattern (shared by all modules)

**Scope:** Category 2 — Development. The concrete UX layer for the **guided phase** described in
`00_Program_Overview.md` §2 and the `Lesson_Sequencer` design in `01_Module1_Plan.md` §2.
**Source of proof:** this layout was built and validated in a sibling PCVR trainer
(`VR/TrainingModuleTester`, Unity 6 + OpenXR + XRI). The patterns and pitfalls below are carried over so the
ProMill 8000 modules don't re-learn them. XRI version differs (3.5.1 there vs **3.3.1** here) but the APIs
used are the same.

This doc covers the four UX pillars the modules need: **part highlighting, task prompting, timers, and
progress** — plus the direct-grab/snap mechanics for placement-style steps (e.g. component drag). It is a
recipe, not new architecture: it plugs into `Lesson_Sequencer` / `Lesson_Controller`.

---

## 0. The one VR pitfall that drives everything

**Screen-Space Overlay canvases do not render to an HMD.** A normal Unity HUD (progress text, timer,
buttons on a `renderMode = ScreenSpaceOverlay` canvas) is invisible in VR — it only shows on the desktop
mirror. Every readable surface must be **World Space**. So all four pillars below live on world-space
canvases, and the persistent HUD is **mounted to the controller (wrist)** rather than the screen.

Do not put lesson UI on an overlay canvas and assume the trainee will see it.

---

## 1. Progress + Timers → wrist-mounted HUD

The running state the trainee needs at a glance — **current step `X / N`** and the **timer** (count-up in
guided mode, countdown in timed/practice mode) — plus persistent controls (reset, audio mute, HUD toggle)
go on a small **world-space canvas parented to a controller**, angled so it reads when the wrist is raised.

### Proven wrist-canvas transform
| Property | Value | Why |
|---|---|---|
| Parent | `Left Controller` (under XR Origin → Camera Offset) | rides the hand; no head-lock, no billboard needed |
| Canvas `renderMode` | **World Space** | overlay is invisible in VR (see §0) |
| Raycaster | **`TrackedDeviceGraphicRaycaster`** (not `GraphicRaycaster`) | lets the UI-ray click buttons |
| Local position | ~`(0, 0.05, -0.07)` | sits just above/behind the controller origin |
| Local euler | ~`(55, 0, 0)` | pitched back so it faces the eyes when the wrist tilts up |
| Local scale | ~`0.0004` | world-space canvas uses pixel units; this maps ~px→mm sensibly |

### Layout — vertical stack on the wrist canvas (top → bottom)
```
   ┌─────────────────┐
   │     3 / 9        │  ← progress (X / N)
   │   ⏱  02:14       │  ← timer
   │   [   RESET   ]  │  ← reset button
   │   [🔊]   [👁]     │  ← audio mute · HUD toggle
   └─────────────────┘
```

### Wiring notes (learned the hard way)
- **Reparent existing UI, don't rebuild it.** If a timer/button group already exists on an overlay canvas,
  move the GameObjects onto the wrist canvas (`Button.onClick` persistent listeners and code-wired
  references survive reparenting). Rebuilding loses wiring.
- After reparenting into a tiny-scale canvas, **reset each child's local transform** (`localScale = 1`,
  `localEuler = 0`, anchors → center `(0.5,0.5)`, explicit `anchoredPosition`). Unity preserves world
  transform on reparent, which leaves garbage local values (scale in the thousands).
- A toggle-visibility button must **not** target the panel that contains the button itself, or the trainee
  can hide it with no way back.

### Maps to existing design
`Lesson_Controller` (Bootstrap, persistent) owns active-module/step/score/time. A small `Wrist_HUD`
component reads it and updates the wrist `TMP` fields on `STATE_CHANGED`-style events. Progress text is just
`"{stepIndex + 1} / {stepCount}"`.

---

## 2. Part highlighting — persistent current-target glow

Two distinct highlights, both **suppressed in practice mode**:

1. **Current-target glow (persistent).** Whenever the sequencer advances, the part the trainee must act on
   **glows continuously** until handled — this is the primary "do this next" cue. Drive it from the
   step-change event, not from hover.
2. **Hover affordance (transient).** On controller hover, color the hovered part green (correct target) or
   red (wrong) for immediate feedback.

### Recipe
- This project already scaffolds the **XRI Fresnel highlight affordance** (XRI Starter Assets,
  `01_Module1_Plan.md` §Reuse). Use it for the hover affordance — it fires automatically on a
  `Simple Interactable`/`Grab Interactable`.
- For the **persistent** glow, a `Part_Highlighter` listens for the sequencer's step-change, clears the
  previous target's highlight, and applies the outline to the new `currentTargetPart`. The tester used an
  **outline-layer swap + outline-material color**; the equivalent here is enabling the Fresnel affordance (or
  an outline material) on the target independent of hover.
- **Restore the persistent glow on hover-exit.** Hover handlers recolor/clear the shared outline; when the
  controller leaves a part, re-apply the current target's glow so it doesn't get dropped.
- **No answer-preview ("ghost").** Do **not** spawn a translucent copy at the correct destination — it reads
  as "already placed" and gives away the answer. Guide with the highlight on the part to act on instead.

---

## 3. Task prompting — billboarded world-space panel

The step prompt ("Grab the spindle head", "Move the table to X = 280") shows on a **world-space panel that
turns to face the trainee** so it's readable from anywhere in the play space.

### Recipe — `Face_Camera` billboard
```csharp
// LateUpdate, on the prompt/dialogue canvas:
var cam = Camera.main; if (cam == null) return;            // XR HMD carries the MainCamera tag
Vector3 dir = transform.position - cam.transform.position;
transform.rotation = Quaternion.LookRotation(dir, Vector3.up);   // upright, faces the player
```
- Cache the camera; re-resolve only when null.
- Expose a **`flip180` bool**. World-space UI reads from one face only; depending on canvas orientation the
  text can come out mirrored — flip if so. (In the tester, facing the user meant pointing the canvas's `-Z`
  at the camera, i.e. `flip180 = true`.)
- Billboard **floating** panels (the step prompt, step-complete popup). Do **not** billboard fixed
  in-world screens (a wall monitor) or the wrist HUD (it tracks the hand).

`Lesson_Sequencer` sets the prompt text on this panel when it activates a step.

---

## 4. Direct-grab + snap (placement-style steps)

For steps where the trainee physically places a part (component-into-place, "move table to target"), use
**direct grab** with a forgiving snap. Reuse the existing distance-check completion logic; the input layer is
controller-agnostic.

### Grab setup (per movable part)
- Add a **kinematic, non-gravity `Rigidbody`** (`isKinematic = true`, `useGravity = false`) + the part's
  `Collider` + an **`XRGrabInteractable`** (`useDynamicAttach = true`, `throwOnDetach = false`).
- Subscribe to `selectExited` to run the completion check.

### Three rules that make it not feel broken
1. **Defer the completion check one frame after release.** Firing it *inside* the `selectExited` callback —
   and mutating the collider/transform there — leaves the interactable unable to be grabbed again. A single
   `yield return null` before checking lets XRI finish its release bookkeeping. *(This was the #1 bug.)*
2. **A wrong attempt must stay grabbable.** On a miss, float the part back to its start, but disable/re-enable
   the `XRGrabInteractable` and reset `isKinematic = true` around the return animation so the part
   re-registers cleanly. The trainee can immediately try again.
3. **Snap is forgiving and aligns rotation.** Hand placement is far less precise than a mouse — use a snap
   radius of ~**0.15–0.20 m** (`Mathf.Max(perStepTolerance, vrSnapRadius)`), and on success snap **both
   position and rotation** to the target pose so the part seats correctly.

---

## New helper components (this project's `PascalCase_With_Underscores`, global namespace)

| Script | Role | Lives on |
|---|---|---|
| `Face_Camera` | billboards a world-space panel to face the HMD (`flip180` option) | each floating prompt/popup canvas |
| `Wrist_HUD` | reads `Lesson_Controller`, updates wrist progress/timer TMP fields on step change | wrist canvas (controller) |
| `Part_Highlighter` | persistent glow of `Lesson_Sequencer.currentTarget`; restores glow on hover-exit | a manager in the module scene |

These extend the existing plan — `Lesson_Step` / `Lesson_Sequencer` / `Lesson_Controller`
(`01_Module1_Plan.md` §2) — they don't replace it. The sequencer fires step-change; `Part_Highlighter`,
`Wrist_HUD`, and the prompt panel are listeners.

---

## Pitfalls checklist (carry-over from the proven build)
- [ ] No lesson UI on a **Screen-Space Overlay** canvas — World Space only.
- [ ] Wrist/world canvases use **`TrackedDeviceGraphicRaycaster`**, and the EventSystem uses the XRI
      **XR UI Input Module** (not `InputSystemUIInputModule`), or the ray can't click.
- [ ] Reset child local transforms after reparenting into a small-scale canvas.
- [ ] Completion check fires **one frame after** `selectExited`, never inside it.
- [ ] Wrong placement keeps the part grabbable (toggle interactable + reset kinematic).
- [ ] Snap radius is hand-scale (~0.18 m) and snaps rotation too.
- [ ] No ghost/answer-preview at the target — highlight the source part instead.
- [ ] Billboarded prompts: verify text isn't mirrored; flip if needed.
- [ ] Highlights + prompts + wrist hints are **suppressed in practice mode**.

## Verification
1. In headset: wrist HUD shows `X / N` + timer; ray clicks RESET / audio / HUD-toggle.
2. Step advances → the new target part glows; glow survives hovering other parts and moves on completion.
3. Prompt panel stays readable as the trainee walks/turns.
4. Grab a part, miss the target → it returns and is immediately grabbable again; hit within ~0.18 m → snaps
   seated. State advances only on success.
5. Practice mode: glow + prompts + wrist hints off; actions scored; pass-gate enforced.
