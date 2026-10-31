# ASRS Module 1 — Implementation Verification Checklist (answered)

**Project:** Unity SmartCIM 4.0 ASRS
**Module:** M1 — What Is ASRS? (Conceptual Overview)
**Review date:** 2026-10-30
**Unity scene / branch / commit:** `Assets/Scenes/ASRSModule1.unity` / `Station1Rack` / `bf5b17d` (Module 1 work in `6f2f862`)
**Reviewer:** Claude (file review of scripts, prefabs, assets and the serialized scene)

> **How this was answered:** this is a file review. A box is **checked** when the functionality exists in the scripts and
> is wired in the scene or prefab data. Unchecked items are marked:
>
> - **BUG**: present but wrong.
> - **TODO**: missing.
> - **NOT FILE-VERIFIABLE**: depends on how it looks or runs on the headset.
>
> `docs/VR_Modules/05_ASRS_Module1_Implementation.md` is **partly out of date** with the saved scene. It lists a
> `M1_StorageRoles` step, a `LoadSlottedTable` step, a Rack station step and 24 steps. The scene has **20 steps** and
> none of those three.

### Actual step sequence in the saved scene

| # | Step | Action ID | Advances on |
|---|---|---|---|
| 1 | Welcome (but `stationType` = SmartCIM 4.0 Cell, see BUG-1) | `M1_Welcome` | Next |
| 2 | ASRS-36x2u | — | Next |
| 3 | ASRS-36x2 | — | Next |
| 4 | Carousel (video) | — | Next |
| 5–10 | Material Flow 1–6 (diagram in the media box) | `M1_Flow_1`…`M1_Flow_6` | Next |
| 11 | "This scene: 72 slots" (but `stationType` = ASRS, see BUG-2) | — | Next |
| 12–16 | Markers: slotted table → RFID tag → epoxy block → ASRS unit → door handle | — | Marker clicked |
| 17 | Inspect the Storage Cells | `M1_InspectCells` | 3 distinct cells |
| 18 | Find the Material | `M1_IdentifyMaterial` | Correct material picked |
| 19 | **Load the slotted table into the ASRS** | `LoadASRS` | Table confirmed in `TableMap` (see BUG-3) |
| 20 | Module 1 Check (quiz) | `M1_QuizPassed` | Pass, then Continue |

**Score:** 44 of 62 items checked. The 18 unchecked items come down to 3 bugs, a few missing pieces and 4 items that can't be judged from files.

## A. Current implementation

### Lesson structure and existing tour
- [ ] Module 1 is selectable and starts correctly. **TODO.** The scene is in Build Settings and starts correctly (`Start()` hides all targets, then runs `ActivateStep(0)`). But no menu (`Main Menu`, `Widget_Map_Select`, Colin's `Module_Loader`) references `ASRSModule1`, so it can't be selected. You can only reach it by opening the scene directly.
- [ ] Existing seven-step physical orientation / guided walkthrough is present, with the intended revised sequence. The physical tour is **5 marker steps** (12–16) plus the 72-slot info step (11), not seven. The robot arm has no marker. Confirm whether five is the intended revision.
- [ ] Welcome / ASRS purpose explains raw-material storage, WIP, and finished-goods storage. **BUG-1.** Step 1 has `stationType = StationType_SmartCIM`, and `SequenceManager.GetInfoText` replaces a step's own text whenever that field is set. The authored welcome text ("Welcome to Module 1. An ASRS stores parts in…") and its instructions are therefore **never shown**. The SmartCIM summary that replaces it does cover raw material / WIP / finished goods, and so does the ASRS-36x2 details page. **Fix:** move SmartCIM from `stationType` to `media` on step 1.
- [ ] Information panel text is readable and correct. Two text problems: the welcome instructions promise "load a template into the ASRS" (contradicts the scope change), and "three storage station types" (the checklist expects four). Readability itself is NOT FILE-VERIFIABLE.
- [x] Pulsing markers highlight the correct ASRS objects (table, RFID tag, part, rack/arm, door as applicable). Five marker steps target the slotted table (`1343739601`), RFID tag (`254648658`), epoxy block (`1643142830`), ASRS unit (`1704333287`) and door handle (`456656642`). All five references resolve in the scene. There is no arm marker.
- [x] Continue / action-based steps advance through the current `SequenceManager` with no skipped or stuck step. Next debounce (0.3 s) and UI debounce prevent double-advance. Any `NotifyAction` that doesn't match the current step is ignored and logged. Cosmetic side effect of BUG-1 and BUG-2: steps 1 and 11 count as station types, so the progress text reads "Station type n of **5**".
- [x] Returning to / restarting Module 1 resets markers, UI, and completion state. Reloading the scene resets everything, and each step feature resets itself on step exit. There is no in-module restart button.

### Four storage station types
- [x] Carousel is explained (step 4, two pages, contrasts the carousel with a Cartesian ASRS).
- [x] ASRS-36 is explained. The asset is named "ASRS-36x2" and describes *two* 6×6 racks (72). Confirm with the source document whether it should be a single 36-cell ASRS-36.
- [x] ASRS-36u is explained, including its rotary axis / Scorbase context ("ASRS-36x2u": extra rotary axis, USB controller, Scorbase). The plexiglass-door-sensor and "72-bay" claims have not been checked against the source.
- [ ] Rack is explained and distinguished from the complete ASRS. **TODO.** `StationType_Rack.asset` exists and has good "storage vs handling system" text, but commit `6f2f862` deliberately removed its step. This item needs a decision: re-add it, or drop it from the requirements.
- [ ] All four introductions occur in the intended lesson order. **TODO.** Only three appear, in the order 36x2u → 36x2 → Carousel.
- [ ] Training distinguishes a 36-cell unit from the Unity model's two 36-slot sides (72 total). **BUG-2.** Step 11 has exactly this text ("The physical SmartCIM ASRS-36u stores 36 templates per unit… This scene models… 72-slot"), but its `stationType = StationType_ASRS` replaces it with the generic ASRS summary. The point survives only partially, in the ASRS-36x2 / 36x2u summaries. **Fix:** move the ASRS asset to `media` on step 11.

### Carousel video
- [x] Carousel demonstration VideoClip is assigned / available on the active laptop checkout. `Videos/Carousel_Overview.mp4` (9.7 MB) is tracked in git and referenced by `StationType_Carousel.video`, with `playVideoAudio: 1`.
- [x] Video screen appears on the correct step (step 4 only, through `stationCard.Show(stationType)`).
- [x] Play, pause, replay and audio work (or omitted controls are explicitly not required). `TogglePause` and `Replay` exist in `Station_Type_Card`, and `LessonPanel.prefab` wires `videoControls`, `pauseButton` and `replayButton`. Audio is on.
- [x] Video stops / hides on lesson transition. Every step re-sets the card (Show/Hide/ShowEmpty), and `OnDisable` stops playback.
- [x] Missing-video fallback does not block the module. `errorReceived` shows the station image or a "couldn't be played" placeholder. Next is never gated on the video.
- [ ] Video is legible and performs acceptably in the target VR configuration. **NOT FILE-VERIFIABLE** (Quest decode, size and readability).

### Material-flow explanation
- [x] Raw materials are loaded / stored in ASRS (`M1_Flow_1`).
- [x] ASRS robot picks a template / part for conveyor transfer (`M1_Flow_2`).
- [x] PLC / conveyor routes the pallet to its destination (`M1_Flow_3`).
- [x] CNC / assembly processes the material (`M1_Flow_4`, labelled as not modelled in this scene).
- [x] Processed parts return to the conveyor (`M1_Flow_5`).
- [x] Finished products return to ASRS storage (`M1_Flow_6`).
- [x] This is a conceptual presentation unless a specific physical interaction is intentionally required. The Flow 1 text says "nothing in the real rack moves", and the diagram never touches `Item_ASRS`, the arm or the conveyor.

## B. Revised interactive objective — slot selection and part identification

- [ ] Previous **physical rack-loading requirement** is removed from the Module 1 step sequence. **BUG-3.** Step 19 (`LoadASRS`) is still a required step. The cell inspector also has a "Load" pointer mode that adds the table to the inventory and inserts it into cell 050001. The welcome text promises this task. **Fix:** delete step 19 from `SequenceManager.steps`, and remove "load a template into the ASRS" from the welcome text.
- [ ] No leftover insertion action ID or missing component can block Module 1 completion. Currently `LoadASRS` gates completion. Once step 19 is removed, nothing will block: `HasReachedStep` returns true for an unknown ID, so `Item_ASRS_Manual_Slot_Insert` on slot 050001 inserts immediately and its `NotifyAction("LoadASRS")` is only logged as ignored. The inspector's Load mode will simply never activate. No step uses `LoadSlottedTable`, and `lessonTable` is assigned directly.
- [x] Existing physical insertion / retrieval functionality remains available outside Module 1. `Item_ASRS_Manual_Slot_Insert` still inserts without gating when no `SequenceManager` is present (`RackmovementTest`). `Item_ASRS` is unchanged.
- [x] Rack cells can be selected using the scene's actual XR / mouse input system. The inspector uses its own Input System actions: XR left/right `triggerPressed`, and `<Mouse>/leftButton` when no XR device is active. It raycasts for the nearest cell target and ignores the glass, door and frame. On desktop it aims from the **screen centre**, not the mouse cursor.
- [x] Selected cell highlights visibly; prior highlights clear. One shared `SelectedCellHighlight` box and per-hand pointing boxes, with faint outlines on every cell. Re-selecting the open cell or pressing Close clears it.
- [x] Selected cell displays its correct one-based index and six-digit `TableID`. The panel shows "Cell 010001  #1 of 72 · Side A, Row r, column c". The 22 edit-mode logic tests cover all 72 addresses.
- [x] Occupied / vacant status matches runtime `Item_ASRS.TableMap`. `GetCellInfo` reads `TableMap` at each selection, with no caching.
- [x] Actual stored material / part is shown where available, without hardcoded assumptions. The name comes from the stored item's `Item_Parent.Name`. The identify target is resolved at runtime from cell `010001` and falls back to the first occupied cell. Five of the eight `materialNotes` descriptions are empty.
- [x] Empty-cell and missing-part-data cases have clear feedback: "Vacant — no template stored here" / "Material: none (empty template)". If the rack has no materials at all, the identify step logs an error and auto-completes, so the trainee isn't trapped.
- [x] Guided task asks the trainee to locate an available part type (step 18: "Find a cell that stores: <material>").
- [x] Incorrect selection gives feedback without advancing ("Not that one — cell X holds Y. Look for Z.").
- [x] Correct identification completes the task **once** and advances the lesson. A `completionReported` guard and a 2 s hold, and `CompleteStepAfterHold` only fires if the step is still current.
- [ ] Selection does not move, retrieve, reparent, or insert inventory. This is true for the Inspect, Identify and quiz modes, but **not** for the Load mode on step 19. Resolved by BUG-3.
- [x] Interaction resets correctly on module retry / exit. `OnFeatureEnter` clears the inspected set, selection and `completionReported`. `OnFeatureExit` disables the targets and input actions.

## C. Remaining work to preserve and finish

### Animated pallet circulation diagram
- [x] Diagram shows ASRS → retrieval → conveyor/PLC route → processing → return → ASRS (six stages in `PalletCirculationDiagram.prefab`; stage 6 reuses the ASRS box).
- [x] Pallet animation advances visibly around the conceptual loop. The pallet glides (25 units/s) to the next box on each Flow step's Next.
- [x] Labels and directional indicators are readable. Stage labels and arrows exist in the prefab. On-headset readability is NOT FILE-VERIFIABLE.
- [ ] Replay / reset and step exit work without leftover moving objects. Reset and exit cleanup exist: leaving the flow steps clears the stage, visited state and highlights, and un-embeds the diagram. **But replay isn't available.** Play/Pause/Replay controls are hidden on the walkthrough steps; they were only shown on the `M1_PalletCirculation` step, which was removed.
- [x] Animation does not operate real ASRS or conveyor controls.

### Three-question interactive quiz
- [x] Exactly three questions are presented.
- [x] Questions cover ASRS purpose, material flow and/or correct cell/part identification. Q1 station types (carousel), Q2 material flow (finished goods back to ASRS), Q3 a real rack-cell pick for the material in `020001`. **Both multiple-choice questions have option A as the correct answer.**
- [x] Answers are captured only once per question (`ASRS_Quiz_Session.Submit` lock plus debouncer; unit tested).
- [x] Correct answers are graded accurately. If Q3 can't resolve a material, it is scored as correct and an error is logged.
- [x] Score **2/3 or 3/3** passes; **0/3 or 1/3** fails (`passMark: 2`; unit tested 0–3).
- [x] Failure allows retry and correctly resets quiz state (Retry → new session, back to Q1, score 0).
- [x] Passing notifies Module 1 completion only once (`completionReported` guard on Continue).
- [x] Quiz is connected to the real lesson sequence (not an unrelated step-order quiz). It is the last step (`M1_QuizPassed`), and `SequenceManager.quizManager` is empty.

### Final integration, QA and documentation prerequisites
- [ ] Full Module 1 works end-to-end in desktop Play Mode. The flow is complete in the files apart from BUG-1, BUG-2 and BUG-3.
- [x] No broken Inspector references or missing scripts on scene objects. Every Module 1 reference resolves to an object in the scene: sequence manager, roots, panels, quiz buttons, rack, lesson table and highlight material.
- [ ] No new runtime errors or unexpected repeated notifications in Unity Console. **NOT FILE-VERIFIABLE.** The code guards against repeated notifications.
- [ ] Lesson step ordering, action IDs, and transitions are verified. The action IDs are consistent between the scene and the scripts, but the ordering has BUG-1, BUG-2 and BUG-3.
- [x] Module reset and replay are successful (scene reload plus per-step feature resets).
- [ ] VR ray interactions and world-space UI work on Quest 3 (if hardware available). **NOT FILE-VERIFIABLE.**
- [x] Other ASRS controls/modules still function. The `SequenceManager` changes are additive (event, accessors, logs). `Quiz_Manager` and Module 2 are untouched. `Item_ASRS_Manual_Slot_Insert` keeps its ungated path.
- [ ] Desktop-only changes are reconciled with laptop/GitHub branch, without overwriting newer assets. **TODO.** Unrelated uncommitted changes in the working tree (`Mat_MachinedAluminum.mat`, TMP fallback font, OpenXR settings, `Physics2DSettings`, `.slnx`) need reviewing.
- [ ] Final scene/prefab/required asset changes are committed and backed up. **TODO** after the fixes. The current Module 1 scene, prefabs and video are committed in `6f2f862`.

## D. Verification notes / bugs / changes needed

| Area | Verified? | Notes / bug / next action | File, object or Inspector reference |
|---|---|---|---|
| Lesson walkthrough | Partly | BUG-1: the welcome text is hidden by `stationType`. Module 1 isn't in any menu. 5 marker steps, not 7 | SequenceManager → Steps[0] `stationType` → move to `media` |
| Station types | Partly | Three types, Rack step removed (TODO: decide). BUG-2: the 36-vs-72 text is hidden. Naming is ASRS-36x2/36x2u | Steps[1–3], Steps[10] `stationType` → `media`; `StationType_Rack.asset` |
| Carousel video | Yes | Clip tracked and assigned, controls wired, error fallback present | `StationType_Carousel.asset`, `LessonPanel.prefab` → Station_Type_Card |
| Material flow | Yes | Six conceptual steps present and correct | Steps[4–9] `M1_Flow_1…6` |
| Slot selection | Yes | Correct and read-only in Inspect mode. On desktop it aims from the screen centre | `Module1_Features/RackCellInspector` |
| Part identification | Yes | Runtime material from `010001`, wrong-pick feedback, single completion | RackCellInspector `identifyReferenceTableId: 010001` |
| Old rack-load gate removed | **No (BUG-3)** | The `LoadASRS` step is still required, and the inspector's Load mode moves inventory. Delete the step and edit the welcome text. Removing it is safe | SequenceManager Steps[18] `LoadASRS`; Steps[0] instructionsText |
| Pallet animation | Mostly | Glides one stage per Next. No replay controls on flow steps | `PalletCirculationDiagram` prefab instance |
| Quiz | Yes | Logic correct and unit tested. Both MC answers are option A | `Module1_Features/Module1Quiz` |
| End-to-end / Quest 3 | No | Blocked by the three bugs. Quest behaviour is not file-verifiable | — |

## E. Program summary prerequisites

Write `ASRS_Module_1_Program_Summary.md` once BUG-1, BUG-2 and BUG-3 are fixed. It should record that physical rack
loading was removed **within Module 1 only**. The manual insert and gripper code stay in place for other modules.
