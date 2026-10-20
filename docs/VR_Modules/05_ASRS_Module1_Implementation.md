# ASRS Module 1 — "What Is an ASRS?": Implementation, Setup and Debugging

Companion to `04_ASRS36_Module1_Plan.md`. Covers what was audited, what was added or fixed on the laptop
checkout (branch `Station1Rack`, on top of commit `f0411fc`), how to wire it in the Editor, how to debug it,
and what still needs verifying.

`ASRS_Station_Tasks_2026.docx` is **not in the repository** and was not inspected. The requirements below come
from the implementation brief and from `04_ASRS36_Module1_Plan.md`.

---

## 1. Audit — what already existed

| Area | State found | Files |
|---|---|---|
| Step system | `SequenceManager` drives `ASRSModule1.unity` (13 steps). The `Lesson_Sequencer` system in `Members/Colin` is **not** used by this scene. | `Members/Alan/SequenceManager.cs` |
| Four station types | **Done.** Intro step, then Carousel, ASRS-36, ASRS-36u and Rack steps, each backed by an `ASRS_Station_Type` asset. | `Training/StationTypes/*.asset`, `ASRS_Station_Type.cs` |
| Carousel video | **Mostly done.** `Station_Type_Card` plays `Carousel_Overview.mp4` into a RenderTexture, with audio on, and stops on every step change. It had no pause/replay controls and no error handling. | `Station_Type_Card.cs`, `Videos/Carousel_Overview.mp4` |
| Lesson panel | **Done.** World-space Lesson Panel with title, progress, body, media box and Next. | `Lesson_Panel.cs`, `UI/LessonPanel.prefab` |
| Part identification | **Done.** Marker steps for the slotted table, RFID tag, epoxy block, ASRS unit and door handle. | scene, `Marker_Interactable.cs` |
| Load template | **Done.** `LoadSlottedTable` waits for an item on the loose table `SlottedTableSingleBuffer (U)`. | scene |
| Rack loading | **Wired but could stall** (see §2.3). | `Item_ASRS_Manual_Slot_Insert.cs` |
| Material flow walkthrough | Missing. | — |
| Pallet circulation diagram | Missing. | — |
| Rack cell inspection and material ID | Missing. | — |
| 3-question quiz | Missing. `Quiz_Manager` is a replay-the-steps, error-count quiz (Module 2 style); it isn't assigned in this scene. | `Quiz_Manager.cs` |
| Validation and logging | Missing. | — |

Rack facts that were verified in code rather than assumed:

- **Size and IDs:** 12 rows × 6 columns = 72 slots. TableID is `row*10000+col`. Rows 1–6 are Side A, rows 7–12 are Side B.
  This matches `ASRSArmTester.IndexToTableId`, and the unit tests confirm it for all 72 slots.
- **Vacant row:** row 5 starts empty (`Item_ASRS.Start`, `isVacantRow`).
- **Loaded materials, from the scene override:** row 1 holds Epoxy Penholder, row 2 holds Tube, and every other row holds Epoxy Block.
- **No `materialLocations`:** `Item_ASRS.materialLocations` doesn't exist in this checkout. `TableMap` is the only
  inventory, and it's what everything new reads.

---

## 2. What changed

### 2.1 New step flow (inserted by the builder, never duplicated)

`ASRS/Module 1/Build Module 1 In Current Scene` inserts only the steps whose action IDs are missing. It leaves
every existing step and every Inspector edit alone.

| # | Step | Advances when | New? |
|---|---|---|---|
| 1 | Welcome: What Is an ASRS? (`M1_Welcome`) | Next | **new** |
| 2 | Storage Stations (intro) | Next | existing |
| 3–5 | ASRS-36x2u → ASRS-36x2 → Carousel (video). The Rack step was removed by **Update Intro (3 Stations) And Step Media** | Next (pages summary, then details) | existing (reduced) |
| 7 | Raw Material, WIP and Finished Goods (`M1_StorageRoles`) | Next | **new** |
| 8–13 | Material Flow 1–6 (`M1_Flow_1`…`M1_Flow_6`). The diagram shows each stage beside the panel. | Next | **new** |
| 14 | Pallet Circulation (`M1_PalletCirculation`) | All 6 diagram stages seen, then a 2 s hold | **new** |
| 15 | This Scene: 72 Slots | Next | existing |
| 16–20 | Slotted table → RFID tag → Epoxy block → ASRS unit → Door handle | Marker clicked | existing |
| 21 | Inspect the Storage Cells (`M1_InspectCells`) | 3 distinct cells inspected | **new** |
| 22 | Find the Material (`M1_IdentifyMaterial`) | A cell holding the named material selected | **new** |
| 23 | Load the Slotted Table (`LoadSlottedTable`) | Item present on the lesson table | existing |
| 24 | Load the ASRS (`LoadASRS`) | Verified rack insert, see §2.3 | existing (fixed) |
| 25 | Module 1 Check (`M1_QuizPassed`) | Quiz passed (≥2/3), then Continue | **new** |
| — | Module Complete | — | text updated |

On info steps (Advance On Next), the Required Action Id is just a label so the features can find their steps.
Next still advances those steps. Every step number after the station steps is one lower than shown above (24 steps in total).

Each step also has a **Media** field (an `ASRS_Station_Type`) for display only: its picture shows in the media box, even on
marker steps, without replacing the step's text or counting as a station type. The slotted-table step uses
`StationType_Template` and the ASRS-unit step uses `StationType_ASRS`. Add the pictures in each asset's **Image** field.

**Later changes:**
- **Pallet Circulation step removed:** the auto-play `M1_PalletCirculation` step is gone. The diagram sits inside the
  Lesson Panel's media box on the six material-flow steps, and Next advances them.
- **Part-identification pictures:** the slotted table (Template), RFID tag, epoxy block, ASRS unit and rack door steps
  each show a picture asset in the media box. The assets are `StationType_Template`, `_RFIDTag`, `_EpoxyBlock`, `_ASRS`
  and `_DoorHandle`. **ASRS → Module 1 → Make Station Type Images Into Sprites** converts any pictures dropped into
  `StationTypes/Images`.
- **Cell selection:** the inspector now does its own picking. On the index trigger (either hand) or a desktop
  left-click, it finds the nearest storage cell along the hand ray, ignoring the cabinet's glass, door and frame
  colliders that used to block it. Every selectable cell gets a faint outline, and the cell being pointed at lights up
  blue. The cells no longer use `Interactable_Select`, so they can't steal a grip either.
- **Load the ASRS by pointer:** on the `LoadASRS` step the inspector also offers a pointer route.
  1. Point at the loaded lesson table and pull the trigger. It's added to the player's real `Inventory` as
     "Slotted Table" and hidden, not destroyed, so it keeps TableID 050001 and the block on it.
  2. Point at an outlined empty cell and pull the trigger. Loading is allowed **only while the inventory holds a slotted
     table**, and only into the table's home cell; other cells explain why they're refused.
  3. Success means the table is confirmed in `TableMap`. One slotted table is then removed from the inventory and
     `LoadASRS` completes after the usual short hold.

  If the table was taken into the inventory some other way that destroys it, a copy saved when the step began is used.
  Carrying it in by hand (`Item_ASRS_Manual_Slot_Insert`) still works.

### 2.2 Features (runtime, `Assets/Members/Alan/Training/Module1/`)

All three features derive from `ASRS_Module1_Step_Feature`. It listens to the new
`SequenceManager.StepActivated` event and matches the step's Required Action Id. On its own steps a feature
shows its panel; on any other step it hides and resets. That means no animation, highlight or pick mode
outlives its step.

- **`ASRS_Pallet_Circulation_Diagram`**: a world-space diagram of ASRS Storage → Robot Retrieval → Conveyor/PLC
  → CNC & Assembly → Return Conveyor → back to storage, with direction arrows.
  - **Pallet movement:** the pallet glides between boxes, never teleporting. Speed and per-stage dwell are set in the Inspector.
  - **Highlighting:** the current box is highlighted, and visited boxes are tinted.
  - **Controls:** Play/Pause, Replay, ‹ Back and Next ›.
  - **Scene highlights:** each stage has an optional `sceneHighlights` list for scene objects, such as an arrow by the real conveyor.
  - **Read-only:** it never touches the rack, the arm or the conveyor. The processing stage says plainly that the CNC and
    assembly stations aren't modelled in this scene.
- **`ASRS_Rack_Cell_Inspector`** and **`ASRS_Rack_Cell_Target`**: the first time they're needed, these build one trigger box
  per slot at the anchors `Item_ASRS` saved in `Start`.
  - **Target size:** each box is the template's collider footprint, grown to about 90% of the row height so a ray aimed at
    the part still hits the cell. Boxes are on the Default layer with an `Interactable_Select`, so they use the existing
    trigger raycast and desktop click.
  - **When active:** the targets exist only during the inspection steps and the quiz's cell question, so they never get in
    the way of carrying the table.
  - **Selecting a cell:** shows a single shared highlight box and one panel that moves next to the selected cell. The panel
    shows the address, number of 72, side, occupancy, stored TableID, and the material with its description.
  - **Data source:** everything is read from `TableMap` at selection time. There are no per-frame scans and no writes.
  - **Exercise target:** the material is whatever cell `010001` stores at runtime (Epoxy Penholder). Any cell holding the
    same material counts as correct. A wrong pick explains what that cell actually holds.
- **`ASRS_Module1_Quiz`**:
  - **Questions:**
    - Q1, station types: which station rotates to a fixed point.
    - Q2, material flow and storage roles: where processed parts go.
    - Q3: pick a cell on the real rack that stores the material held in cell `020001` (Tube), resolved at runtime.
  - **Answering:** one question at a time. Each answer is locked in by `ASRS_Quiz_Session`, so a double press can't score
    twice, and shown with an explanation.
  - **Result:** 2/3 or better passes and shows Continue, which reports `M1_QuizPassed` once. Below that, Retry resets
    everything. Module 2's `Quiz_Manager` is untouched.
- **`ASRS_Rack_Addressing`** and **`ASRS_Quiz_Session`**: pure C# logic with no `UnityEngine`, so they can be unit tested.
- **`ASRS_Module1_Log`**: Console prefixes `[ASRS][Module1]`, `…[Rack]`, `…[Cells]`, `…[Quiz]`, `…[Video]` and `…[Flow]`.
- **`ASRS_Module1_UI`**: binds each button to both the UI ray and `Interactable_Select`. A 0.3 s debounce stops one press
  arriving twice, which would otherwise skip a question.

### 2.3 Rack-loading fix (`Item_ASRS_Manual_Slot_Insert`)

The stall was real. `Entity_XR_Hand` parents a carried table to the hand, so when the table first touches the slot
trigger it's still held, and the component skipped it. When the trainee then let go **inside** the slot, no new
`OnTriggerEnter` fired, and the step waited forever.

The fix has five parts:

- **Release inside the slot:** `OnTriggerStay` now catches a release that happens inside the slot.
- **Too early:** a correct table that arrives before `LoadASRS` is held as pending, not inserted, and is inserted
  automatically once that step starts. Before this, an early insert left the step with nothing to wait for.
- **Verified success:** success means the rack's `TableMap` now holds this table at its index. Merely touching the trigger
  isn't enough, and a failed insert doesn't notify.
- **Once only:** `NotifyAction("LoadASRS")` fires once per slot. One-shot logs explain rejections (wrong TableID, too
  early, slot occupied).
- **Physics:** velocity is zeroed and the table is made kinematic, so it can't fall out of the slot.

Nothing in `Item_ASRS` changed. The 72-slot addressing, vacant row and anchors are untouched.

### 2.4 Carousel video (`Station_Type_Card`)

The existing system was extended; it isn't a second player.

- **Controls:** Pause/Play and Replay, shown only while a video is on the card.
- **Errors:** `errorReceived` falls back to the station image, or to a visible "couldn't be played" message, and logs the error.
- **Missing parts:** a warning is logged if a station has a clip but the card has no player or surface.
- **Stopping:** `OnDisable` stops playback, so a hidden panel can never keep playing.

Continue is still the panel's Next. No internet connection is needed; the clip is a local asset.

### 2.5 Shared-script changes and why

| File | Change | Why |
|---|---|---|
| `SequenceManager.cs` | Added the `StepActivated` event, `CurrentStepIndex`/`CurrentStep`, a `logTag` field, and one log line per step change and per `NotifyAction`. Clarified one tooltip. | Lets step-scoped features show and clean up without `SequenceManager` knowing about them, and makes progression traceable. No behaviour change: Module 2 runs exactly as before, apart from extra Console lines. |
| `Station_Type_Card.cs` | Video controls, error handling, stop on disable. | §2.4 |
| `Item_ASRS_Manual_Slot_Insert.cs` | Stay-based insert, gating, verification, single notify, logs, read-only accessors. | §2.3. `RackmovementTest` (no SequenceManager) still inserts with no gating. |
| `Lesson_Panel_Builder.cs` | One line: a rebuilt prefab includes the video controls. | So a rebuild doesn't drop them. |
| `StationType_Rack.asset`, `StationType_Carousel.asset` | Second-page text only. Rack: storage vs handling system, and templates. Carousel: contrast with Cartesian ASRS. | Required by the brief. |

---

## 3. Unity setup checklist (laptop)

1. Open `ASRSModule1.unity` and let Unity compile. Expect no errors; this was compile-checked outside Unity, see §6.
2. **ASRS → Module 1 → Add Video Controls To Lesson Panel Prefab.** This adds Pause/Replay to `LessonPanel.prefab`'s
   MediaBox and wires `Station_Type_Card.videoControls / pauseButton / pauseLabel / replayButton`. It's safe to re-run.
3. **ASRS → Module 1 → Build Module 1 In Current Scene.** It creates `Module1_Features`, which holds the diagram,
   cell inspector and quiz, inserts the missing steps, wires references and runs the validator.
4. Read the validator output (**ASRS → Module 1 → Validate Scene** re-runs it). Fix every red line and review the yellow ones.
5. Check the panel positions in the Scene view. `DiagramPanel` and `QuizPanel` are placed 1.45 m to the right of the
   Lesson Panel, in the same spot, because they're never shown together. `CellInfoPanel` moves next to the rack at runtime.
6. Save the scene.

Every reference the builder wires, in case you need to fix one by hand:

| GameObject | Component | Field | Value | Required |
|---|---|---|---|---|
| Module1_Features/PalletCirculationDiagram | ASRS_Pallet_Circulation_Diagram | Sequence Manager | the scene's SequenceManager | yes (else found at runtime) |
| 〃 | 〃 | Root | child `DiagramPanel` (must be a child, not the host itself) | yes |
| 〃 | 〃 | Stages (6) → Node | `Node_1`…`Node_5` (stage 6 reuses `Node_1`) | yes |
| 〃 | 〃 | Pallet, text fields, buttons, Controls Root | children of `DiagramPanel` | yes |
| 〃 | 〃 | Stages → Scene Highlights | e.g. an arrow by the real conveyor | optional |
| Module1_Features/RackCellInspector | ASRS_Rack_Cell_Inspector | Rack | the active `ASRS Variant 1` Item_ASRS | yes |
| 〃 | 〃 | Highlight Material | `Assets/Materials/Green_Glass.mat` (the markers' material) | optional |
| 〃 | 〃 | Root / Panel / Prompt / Cell / Feedback / Close | `CellInfoPanel` and its children | yes |
| 〃 | 〃 | Material Notes | one per material the rack loads (filled from MaterialList) | recommended |
| 〃 | 〃 | Identify Reference Table Id | `010001` (not row 5) | yes |
| Module1_Features/Module1Quiz | ASRS_Module1_Quiz | Cell Inspector | `RackCellInspector` | yes (Q3) |
| 〃 | 〃 | Questions (3) / Pass Mark (2) | filled by the builder | yes |
| 〃 | 〃 | Option Buttons/Labels (4), Next, Retry, Continue, texts | children of `QuizPanel` | yes |
| SequenceManager | SequenceManager | Quiz Manager | **leave empty** for Module 1 | — |
| SequenceManager | SequenceManager | Log Tag | `[ASRS][Module1]` (set by builder) | optional |
| Rack row-5 slot `050001` | Item_ASRS_Manual_Slot_Insert | Rack / Sequence Manager / Notify `LoadASRS` | via **ASRS → Hook Up ASRS Variant 1 (Module 1)** | yes |

---

## 4. Debugging guide

- **Starting Module 1:** open `ASRSModule1.unity` and press Play. For desktop testing, mouse-click is the same
  interact raycast that VR uses with the trigger.
- **Console filters:**
  - `[ASRS][Module1]` shows the whole module.
  - `…[Rack]`, `…[Cells]`, `…[Quiz]`, `…[Video]` or `…[Flow]` isolate one system.
  - `[ASRS][Module1][Validate]` shows the validator.
- **Progression:** every step start logs `Step n/N '<title>' started`, and every `NotifyAction` logs whether it
  completed the step or why it was ignored (wrong step, or no step waiting). A stall is either a step that never
  logs "completes", or an action logged as "ignored — step n waits for 'X'".
- **Rack insertion:** watch `[Rack]`. You'll see `rejected table` (wrong TableID), `arrived before step` (early,
  inserted later), `already occupied`, or `inserted and confirmed in rack inventory`, followed by
  `Action 'LoadASRS' completes step`. If nothing at all appears, the table never overlapped slot `050001`'s trigger:
  check that its collider is a trigger, and check the lesson table's TableID with the validator.
- **Cell interaction:** `[Cells] Built 72 cell target(s)` appears on the first inspection step. Each selection logs the
  address, occupancy and material. If clicks do nothing, the ray is probably hitting something else first, such as the
  door or glass collider on the Default layer: pause and look at `RackCellTargets` in the hierarchy.
- **Quiz scoring:** each answer logs `Qn: correct/wrong (score s/3)`, and the result logs `PASS` or `FAIL`. To test
  0, 1, 2 and 3 correct, answer deliberately and use Retry; the score resets to 0/3.
- **Completion:** `Action 'M1_QuizPassed' completes step 25`, then `Walkthrough complete`, and the panel shows
  "Module Complete".

---

## 5. Test plan

| # | Scenario | How to check |
|---|---|---|
| 1 | Module starts | Play: step 1 "Welcome" shows and the Console logs it |
| 2 | Four stations | Next through Carousel, ASRS-36, ASRS-36u and Rack: titles say "Station type n of 4", and the photos show for ASRS-36 and ASRS-36u |
| 3 | Carousel video | Plays with audio. Pause/Replay work. Next stops it. Renaming the clip should give a placeholder message, not a black box |
| 4 | Flow steps | M1_Flow_1–6 advance on Next and the diagram follows stage by stage |
| 5 | Diagram | Play runs a full lap. Pause holds. Replay restarts. ‹ › step. The step completes about 2 s after all six stages are seen. The diagram hides afterwards |
| 6 | Inspect all cells | Every one of the 72 cells selects. Re-selecting a cell or pressing Close closes it |
| 7 | Occupancy accuracy | Row 5 shows Vacant, row 1 Epoxy Penholder, row 2 Tube, and other rows Epoxy Block |
| 8 | Material ID | A wrong cell gives feedback and the step stays. A cell holding Epoxy Penholder completes the step |
| 9 | Wrong insertion | Drop the table into another row-5 slot: it's rejected (log) and the step stays |
| 10 | Correct insertion | Release the table inside slot `050001`, including releasing while already inside: one "completes step" line |
| 11 | Quiz 0/1/2/3 | Results show FAIL, FAIL, PASS, PASS. Continue only appears on a pass |
| 12 | Retry | After a fail, the score shows 0/3 and question 1 is shown |
| 13 | Completion | Continue → "Module Complete" |
| 14 | Re-entry | Stop and Play again (or reload the scene): everything starts at step 1, with no leftover highlights or panels |
| 15 | Other modules | Open the Module 2 scene and `RackmovementTest`: behaviour is unchanged apart from extra `[ASRS][Sequence]` log lines |

---

## 6. Test results

**Executed and passed (outside Unity, 2026-10-19):**

- **Compile check:** every script was compiled against the project's Unity 6000.3.8f1 engine and package DLLs.
  Runtime assembly: 166 files, 0 errors. Editor assembly: 23 files, 0 errors. The script is in the session
  scratchpad and isn't part of the repo.
- **Edit Mode logic tests** (`Editor/Tests/ASRS_Module1_Logic_Tests.cs`): 22 of 22 passed under NUnitLite. They cover
  72-slot address round-trips, a cross-check against the project's `ASRSArmTester.IndexToTableId` (copied verbatim),
  rejection of invalid IDs, quiz scores of 0 to 3 against the pass mark, double-submit protection, advancing without an
  answer, and reset.

**Executed and failed:** none.

**Not run: needs the Unity Editor.** The builder, validator, prefab patch, and every scenario in §5. The Edit Mode tests
should also be re-run in Unity's Test Runner (EditMode tab) to confirm Unity discovers them in `Assembly-CSharp-Editor`.

**Not run: needs Quest 3 hardware.** Trigger-ray reach to the cells through the rack doors, panel readability and
scale, video decoding on Android, and the release-inside-slot insert under real hand tracking.

---

## 7. Desktop synchronisation notes

Commit `f0411fc` (laptop) already contains the station types, carousel video, Lesson Panel and earlier `SequenceManager`
work. This session's changes are uncommitted on top of it.

Likely conflicts when merging the desktop work:

| Area | Laptop change | Risk |
|---|---|---|
| `SequenceManager.cs` | Event, accessors and log lines added in `ActivateStep`/`NotifyAction` | **Medium.** If the desktop edited the same methods, merge by hand and keep both sets of changes |
| `Station_Type_Card.cs` | Video controls and error handling | **Medium.** If the desktop has its own carousel video controls, keep one set and delete the other |
| `StationType_Carousel/Rack.asset` | Second-page text | **Low.** Text-only YAML. Pick whichever wording you prefer |
| `ASRSModule1.unity` | **Not edited by hand.** It only changes when you run the builder | **High once built.** Scene YAML merges badly |
| `LessonPanel.prefab` | Only changes when you run "Add Video Controls" | Medium |
| `Item_ASRS_Manual_Slot_Insert.cs` | Rewritten insert logic | Low, unless the desktop changed it too |

Recommended order:

1. Commit this work on its own branch, with **no scene or prefab changes**: don't run the builder until step 3.
2. On the desktop, push the unpushed work, then merge this branch. The scripts should merge cleanly or with small hand edits.
3. On the merged scene, run **Add Video Controls**, **Hook Up ASRS Variant 1 (Module 1)**, then
   **Build Module 1 In Current Scene**, and save. The builder only adds what's missing, so it adapts to whatever the
   desktop scene already contains. That includes any desktop-only steps; check the inserted positions in the validator log.
4. If the desktop has its own carousel controls or quiz, keep one implementation. The validator flags a leftover
   `Quiz_Manager` assignment.

---

## 8. Remaining limitations

- **Not run in Unity:** nothing has been run in Play mode or on a headset. Everything beyond compilation and the
  pure-logic tests is unverified.
- **Ray reach to cells:** this depends on the rack's door and glass colliders. If they're on the Default layer and closed,
  the interact ray may not reach the cells (§4).
- **Narration:** no narration audio exists. Steps support it through each step's `narration` field, or the station
  type's own `narration`.
- **ASRS-36u text:** the existing claims about the plexiglass door sensors and the 72-bay installation weren't checked
  against the source document. Verify them, or remove them.
- **Lesson length:** the target of about 8 minutes is unmeasured. Time a full run.
- **Highlight material:** `Green_Glass.mat` is assumed to suit Quest (URP). If it renders opaque, swap it for any
  transparent URP Unlit material.
- **Inventory changes:** the identification exercise and Q3 read the live inventory. If a later module changes the rack
  before Module 1 runs in the same session, the target material follows whatever is actually stored.
- **CNC and assembly stations:** these aren't in this scene. The diagram labels them as conceptual.
