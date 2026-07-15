# Module 1 — "CNC Milling: What & Why" — Build Plan

**Scope:** Category 2 — Development. See `00_Program_Overview.md` for the program-wide context, lesson
format, and shared foundation.
**Target:** PC VR — Quest 3 via Link (Standalone OpenXR).
**Source:** docx Category 2 task #3 + Module Specifications row M1.

---

## Objective & Pass-Gate

**Learning objective:** the trainee identifies all major components, explains 3-axis motion (right-hand
rule), and names ≥ 4 of the 5 milling operations.

**Pass-gate:** 6-part component-ID quiz, **≥ 4/6** to mark the module complete.

**Estimated runtime per trainee:** ~8 minutes (per docx deliverable).

---

## Phase 0.5 — Bootstrap Scene + Menu + Loader (shared, persistent)

Built once here; reused unchanged by M2–M3. See `00_Program_Overview.md` §6 for the architecture rationale.

1. **Bootstrap scene:** create `Assets/Scenes/Training/Bootstrap.unity` and set it as **build index 0**. It
   loads once and stays loaded. It owns the shared, persistent objects: the XR Origin rig (Phase 1), the
   `Lesson_Controller`, audio, and a screen-fade.
2. **Menu hub:** a world-space UI panel in Bootstrap listing the available modules (M1 now; M2/M3 later).
   No scene load needed to show it.
3. **`Module_Loader`** (MonoBehaviour in Bootstrap, `Assets/Scripts/Training/Module_Loader.cs`):
   - `LoadModule(string sceneName)` → fade out → `SceneManager.LoadSceneAsync(name, Additive)` →
     `SetActiveScene` → fade in.
   - `ReturnToMenu()` → fade out → `UnloadSceneAsync(activeModule)` → show menu → fade in.
   - The XR rig is never reloaded across transitions.
4. **Build Settings:** add Bootstrap (index 0) and every module scene to **Scenes in Build**, or
   `LoadSceneAsync` cannot find them.

**Verify:** launch from Bootstrap → menu appears in VR → selecting M1 fades in `Module1_Overview` additively
→ "Return to menu" unloads it with no XR-rig hitch.

## Phase 1 — XR Rig + Module Scene

The XR rig is **shared** (built in Bootstrap, not duplicated per module). The module scene holds only its
own content.

1. **XR Origin rig (in Bootstrap):** add a rig from the XRI Starter Assets
   (`Assets/Samples/XR Interaction Toolkit/3.3.1/`) — camera, left/right controllers with **ray + poke
   interactors**, and locomotion (teleport/snap-turn) as needed for the tour.
2. **Player wiring:** populate `Entity_Player.LeftHandAnchor` / `RightHandAnchor` (slots already exist) and
   complete the unbound `XRControllerPosition` + controller actions in
   `Assets/Input/Player_Input.inputactions`.
3. **Desktop fallback:** confirm `Entity_Player`'s runtime XR detection still falls back to keyboard/mouse
   so scenes can be iterated in-editor without a headset.
4. **Module scene:** create `Assets/Scenes/Training/Module1_Overview.unity` (loaded additively by
   `Module_Loader`). **Do not** build modules inside `DigitalTwin.unity` — scene merges on that file have
   silently dropped prefab overrides before. Instantiate the existing mill prefab
   (`reconstructedPM8000` / `Mill.prefab`) into the module scene; it carries no XR rig of its own.
5. **HaptX:** not required for M1 (no force interactions). Deferred to M3 (Safety).

**Verify:** with a headset on Link, head + controllers track from the Bootstrap rig while M1 is loaded
additively; without a headset, desktop fallback runs in-editor.

---

## Phase 2 — Guided-Lesson System (new, reusable code)

The one genuine gap. New scripts under `Assets/Scripts/Training/`, modeled on the existing `Job_Queue` FIFO
pattern (`Assets/Scripts/Job_System/`).

### `Lesson_Step`
Serializable class / ScriptableObject describing one step:
- prompt text;
- optional narration clip;
- target GameObject(s);
- expected interaction (select / move-to / answer);
- success condition.

### `Lesson_Sequencer` (MonoBehaviour)
Drives an ordered `List<Lesson_Step>` (FIFO, mirrors `Job_Queue.jobPeek` / `jobPop`):
- highlights the current target via the **XRI Fresnel affordance**;
- shows the prompt on a world-space canvas;
- waits for the correct interaction, then advances;
- exposes a **`Guided` vs `Practice`** flag — in Practice, prompts + highlights are suppressed and the
  trainee's answers are scored.

### `Lesson_Controller` (MonoBehaviour, implements `Save_Data_Interface`)
- **lives in the Bootstrap scene** (persistent), not in module scenes — so progress/scores survive additive
  load/unload and there is a single, stable `Save_Data_Interface` implementor (avoids `Data_Loader` re-scan
  churn as module implementors come and go);
- owns module state (active module, guided/practice, running score);
- persists progress + quiz scores by extending `Save_Data`
  (`Assets/Scripts/Save_System/Save_Data.cs`) with `Module_Progress` and `Quiz_Scores`;
- auto-discovered by `Data_Loader`.

### Clickable components
Add an XRI **Simple Interactable** to each labeled mill part so both controller-ray and hand-poke selection
work and the affordance highlight fires automatically. On select, call into `Lesson_Sequencer`. Keep the
existing `Item_Parent` camera-raycast path working for desktop testing.

---

## Phase 3 — Module 1 Content

### 3.1 Narrated tour
- The ProMill 8000's role in the SmartCIM 4.0 cell.
- The 5 milling operations: face, pocket, contour, drill, slot.
- The right-hand rule for X/Y/Z.

### 3.2 Six clickable labeled components
Exploded / explorable view; each highlights and shows a label + prompt in guided mode. List follows the
official Intelitek ProMill 8000 Quick Start guide (34-0000-8000 Rev-C) "Machine components" plus
teaching-critical parts:

1. Spindle motor
2. Spindle head
3. Vise
4. Guard door
5. Emergency stop button
6. Electronics cabinet

Unmodeled official parts (right-side connection panel, jog pendant, monitor stand) are covered by a
narration-only info step — no markers.

### 3.3 Axis-motion demo
Drive `AxisMovement` (`Assets/Members/Colin/ProMill8000/AxisMovement.cs`) for the three travels, scaled to
the model, with an on-screen axis indicator:

| Axis | Travel | Motion |
|------|--------|--------|
| X | 280 mm | table left–right |
| Y | 152 mm | table forward–back |
| Z | 270 mm | spindle up–down |

### 3.4 Milling-operation demo
Call `MillingAnimation.Play()` (`MillingAnimation.cs`) to show the cutting sequence (plunge → square →
retract) on a perspex block using `Blue_Glass.mat` / `Epoxy.mat`.

### 3.5 Practice + assessment
Same scene, help removed:
- **6-part component-ID quiz** — name/select each component with no highlights.
- World-space quiz UI from TextMeshPro + XRI UI prefabs.
- Score gates at **≥ 4/6** before "complete."

---

## Files

**New**
- `Assets/Scenes/Training/Bootstrap.unity` (persistent; build index 0) + world-space menu hub
- `Assets/Scenes/Training/Module1_Overview.unity` (loaded additively)
- `Assets/Scripts/Training/Module_Loader.cs`
- `Assets/Scripts/Training/Lesson_Step.cs`
- `Assets/Scripts/Training/Lesson_Sequencer.cs`
- `Assets/Scripts/Training/Lesson_Controller.cs` (lives in Bootstrap)
- World-space quiz UI prefab (under `Assets/Game_Objects/` or `Assets/Prefabs/`)

**Reuse / extend**
- `Assets/Members/Colin/ProMill8000/AxisMovement.cs`, `MillingAnimation.cs` (drive demos)
- `Assets/Scripts/Save_System/Save_Data.cs` (add `Module_Progress`, `Quiz_Scores`)
- `Assets/Scripts/Player Scripts/Entity_Player.cs` (hand anchors)
- `Assets/Input/Player_Input.inputactions` (controller bindings)

**Reference only**
- `Assets/Samples/XR Interaction Toolkit/3.3.1/` (rigs, interactors, Fresnel affordance)

---

## Verification

1. **Boot + menu + routing:** Play from `Bootstrap.unity`; the menu appears in VR; selecting M1 fades in
   `Module1_Overview` additively; "Return to menu" unloads it with no XR-rig hitch.
2. **XR rig:** with the OpenXR **Mock Runtime** (no headset) and again on **Quest 3 via Link**, head/
   controller tracking works from the persistent Bootstrap rig while M1 is loaded; desktop fallback runs
   in-editor.
3. **Guided flow:** each of the 6 components highlights on hover; its prompt shows; selecting the correct one
   advances the sequencer; a wrong selection does not.
4. **Demos:** axis-motion demo drives X/Y/Z correctly; `MillingAnimation.Play()` runs the full sequence on
   the perspex block.
5. **Practice mode:** prompts + highlights suppressed; quiz records answers; pass-gate enforces ≥ 4/6 before
   "complete."
6. **Persistence:** complete the module, restart, confirm progress + quiz score reload via
   `Save_Data_Interface` / `Data_Loader`.
