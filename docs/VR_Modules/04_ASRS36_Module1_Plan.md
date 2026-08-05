# ASRS-36 Training Simulation — Module 1: Component Identification & Process Flow

**Naming note:** this is a **separate training program** from the CNC Mill (ProMill 8000) modules covered
by `00_Program_Overview.md`–`03_Module2_Startup_Plan.md`. Both programs happen to call their first module
"Module 1" — this doc is the ASRS-36 one; do not confuse it with `01_Module1_Plan.md` (ProMill 8000).
**Scene:** `Assets/Scenes/ASRSModule1.unity` (standalone scene, not yet wired into a Bootstrap/`Module_Loader`
flow). A superseded prior version exists at `Assets/Scenes/ASRSTraining(OLD).unity`.

---

## 1. Overview

Module 1 teaches component identification for the Intelitek ASRS-36 by walking the learner through a live
process — an empty template gets loaded with a part, then transported into the ASRS storage rack — rather
than relying on cold, static click-to-identify highlighting.

Component recognition becomes a byproduct of watching (and optionally replaying/quizzing on) the real
workflow, which mirrors how the ASRS actually operates.

**Scope note:** Module 1 does not model the robot arm's kinematics. Transport between zones is abstracted
(simple move/fade/slide) so the same architecture can be extended in a later module to real arm-driven
motion without rework.

---

## 2. Components Represented

| Component | Role in Module 1 | Implementation status |
|---|---|---|
| Template (empty) | First component identified — pin-hole tray | Built — `Item_Slotted_Table` |
| RFID Tag | Second component identified — tracking/ID tag on the template | **Marker only**, no dedicated object — see §5 |
| Epoxy Block | Third component identified — the part to be loaded | Built — `Item_Epoxy_Block` |
| Loaded Template | Template + Epoxy Block combined state, after loading | Built — `Item_Slotted_Table.SetItem()` |
| ASRS Storage Unit | Identified/stated as a unit before interacting with it | Built — `Item_ASRS` |
| Storage Cell | Individual slot; Empty / Occupied state; destination cell | Built — index into `Item_ASRS.TableMap` |
| Door Handle | Interactive trigger — clicking/interacting opens the door | **In progress** — animation-driven, no confirmed click script — see §5 |
| ASRS Door | Open / Closed state, driven by Door Handle interaction | **In progress** — see §5 |
| Robotic Arm + Gripper | Present in scene, not animated/interactive this module | Built (present, not wired to this module's flow) — `ASRSArmController` |
| Controller-USB | Passive/labeled only | **Not implemented** — no file/prefab found |
| Emergency Stop Button | Passive/labeled only | **Not implemented** — lesson step exists (`estop_36u`), no marker/object placed yet |

---

## 3. Materials / Parts Used in Module 1

Module 1 uses a fixed, specific part roster (confirmed set, not the placeholder cube/cover/box example
referenced in earlier drafts of this doc):

| Item | Role | Files |
|---|---|---|
| Template | Pin-hole tray; base carrier for the part | `Assets/Scripts/Item Scripts/Item_Slotted_Table.cs`; prefab `Assets/Game_Objects/SlottedTableSingleBuffer.prefab`; controller `Assets/Members/Alan/SlottedTableSingleBuffer (U).controller` |
| RFID Tag | Identification/tracking tag associated with the template | Marker only — `Target_Marker_Id: RFIDTag` in `ASRSM1_Lesson.asset`, no mesh/script |
| Epoxy Block | The physical part loaded onto the template | `Assets/Scripts/Item Scripts/Item_Epoxy_Block.cs`; prefab `Assets/Game_Objects/Item/Item_Epoxy_Block.prefab`; mesh `Assets/Meshes/EpoxyBlock.fbx`; material `Assets/Materials/Epoxy.mat` |
| Loaded Template | Template + Epoxy Block, after the load step | `Item_ASRS.LoadEpoxyBlocks()` instantiates a block onto every empty table; load animation `Assets/Members/EpoxyToTable.anim` |

**Important constraint from Intelitek documentation:** the ASRS only handles templates, never bare parts
directly — the Epoxy Block must be loaded onto the Template before the ASRS transports anything.

---

## 4. Step Sequence (Confirmed design)

1. **Identify the Template** — component ID step; narration explains the template's role as the part
   carrier.
2. **Identify the RFID Tag** — component ID step; narration explains its tracking/ID function.
3. **Identify the Epoxy Block** — component ID step; narration explains it as the part to be loaded.
4. **Load the Epoxy Block onto the Template** — animation of block moving onto template; state changes
   Empty Template → Loaded Template.
5. **Identify/State the ASRS Storage Unit** — narration introduces the unit as a whole before interaction
   begins.
6. **Interact with the Door Handle** — user-triggered interaction (click/OnClick); this is the input that
   drives the next step.
7. **Door Opens** — animation triggered by the Door Handle interaction; reveals the empty target Storage
   Cell.
8. **Bring the Loaded Template into the Empty Storage Cell** — Loaded Template (with Epoxy Block) moves
   from staging into the target Cell.
9. **Close the Door** — door animates closed; Cell state updates to Occupied.

Steps 1-3 are pure identification (no state change). Step 4 is the first state-changing action. Steps 5-9
shift from identification into interaction — Step 6 is notable as the first step where the user's action
(not just a Next/advance click) directly causes the animation (Door Handle → Door Open), rather than the
controller auto-playing a transition.

**Current lesson asset vs. this sequence:** `Assets/Members/Alan/ASRSM1_Lesson.asset` (a `Lesson_Definition`)
currently only encodes 5 marker-based ID steps — `slotted_table`, `rfid_tag`, `epoxy_block`, `rfid_sensor`,
`estop_36u`. Steps 4 and 6-9 above (load, door interaction, transport, close) are not yet represented as
lesson steps; they exist today only as in-scene animation/logic (`EpoxyToTable.anim`,
`Assets/Members/Alan/TableToRack.anim`, the door controllers) without step-gating. Closing that gap —
authoring the remaining steps into the lesson asset — is the main outstanding work to make this doc's
9-step sequence real end to end.

---

## 5. Implementation Notes & Open Items

- **RFID Tag vs. RFID Sensor — two different things.** The "RFID Tag" identification step (step 2) points at
  a marker on the template model (`Target_Marker_Id: RFIDTag`), not a coded object. The actual working RFID
  logic in the scene is `Item_RFID_Sensor` (`Assets/Scripts/Item Scripts/Item_RFID_Sensor.cs`,
  prefab `RFIDSensor.prefab`) — a sensor mounted near the rack that reads a passing `Item_Slotted_Table` via
  `OnTriggerEnter`/`Exit` and dispatches `RACK_TASK.INSERT`/`RETRIEVE` to `Item_ASRS`. The lesson's `rfid_sensor`
  step targets this real object. Don't conflate the two when writing narration — "RFID Tag" (step 2) is the
  tag *on the template*; the *sensor* is a separate rack-side component not otherwise called out in §2's
  component list.
- **Door Handle → Door interaction is still in progress.** Three most-recent commits
  (`1a0e706`, `9a2bc43`, `59542f5`) progressively build the door-open animation via Animator Controllers
  (`HingeJoint.controller`, `LeftDoorHinge.controller`) and clip (`LeftDoorOpen.anim`), and the working tree
  has further uncommitted edits to all three plus the scene — this is active, unfinished work as of this
  writing. No dedicated door-click script (e.g. an `Interactable_Handle`-style component) was confirmed wired
  to the ASRS door; commit `1a0e706`'s message notes an outstanding VR-interaction bug ("Needs to fix the
  animation"). Treat step 6-7 (Door Handle → Door Opens) as **not yet verified working in VR** until
  confirmed in-headset.
- **Two parallel step-gating systems exist in the project** — worth resolving before writing more lesson
  content:
  - `Lesson_Sequencer` / `Lesson_Definition` / `Lesson_Controller` (`Assets/Members/Colin/Training/Scripts/`),
    the system `01_Module1_Plan.md` documents for the CNC program. `ASRSM1_Lesson.asset` and
    `DigitalTwin_Lesson.asset` are real instances of this, wired via `DigitalTwin_Lesson_Bootstrap.cs`.
  - `SequenceManager.cs` + `Marker_Interactable.cs` + `InstructionDisplay.cs` (all
    `Assets/Members/Alan/` / `Assets/Members/Alan/Training/`) — a simpler, independently-built step/marker
    system that appears to be what actually drives `ASRSModule1.unity` at runtime today.
  It's unclear which is canonical for this module; confirm with the team before assuming `Lesson_Sequencer`
  governs this scene the way it does the CNC modules.
- **Controller-USB and Emergency Stop Button are unbuilt.** Neither has a script, prefab, or mesh in the
  project. The `estop_36u` lesson step exists but has no matching marker/object placed in the scene yet.
  These remain label-only per the design (§2), but need an actual in-scene object to label before the step
  can fire.
- **`Item_ASRS` is not yet on the shared `Job_Queue` pattern** — its `Machine_Job` list field is commented
  out (`// public List<Machine_Job> Jobs`), so unlike the CNC program's `Item_Station`/`Job_Queue` FIFO, the
  ASRS rack's insert/retrieve tasks aren't currently queued through the shared job system.

---

## 6. Files

**Core / built**
- `Assets/Scenes/ASRSModule1.unity` — module scene
- `Assets/Members/Alan/Item_ASRS.cs` — rack manager: 12×6 grid (`RackRows`/`RackCols`),
  `GenerateRackTables()`, `TableMap`/`materialLocations`, `SlotInsert`/`SlotRetrieve`/`RetrieveByID`,
  `GetIndex()`
- `Assets/Scripts/Item Scripts/Item_Slotted_Table.cs` — Template: `Item`, `AnchorPoint`, `TableID`,
  `SetItem()`/`UnSetItem()`
- `Assets/Scripts/Item Scripts/Item_Epoxy_Block.cs` — Epoxy Block (`Item_Parent` subclass, `Pickup = true`)
- `Assets/Scripts/Item Scripts/Item_RFID_Sensor.cs` — RFID sensor (rack-side, drives insert/retrieve tasks)
- `Assets/Members/Alan/ASRSArmController.cs`, `ASRSArmTester.cs` — arm gantry motion + keyboard debug harness
- `Assets/Members/Alan/SequenceManager.cs`, `Assets/Members/Alan/Training/Marker_Interactable.cs`,
  `InstructionDisplay.cs` — the step-gating system currently driving this scene (see §5)
- `Assets/Members/Alan/ASRSM1_Lesson.asset` — `Lesson_Definition` instance with the 5 current ID steps

**In progress (door animation, uncommitted as of writing)**
- `Assets/Members/Alan/HingeJoint.controller`, `LeftDoorHinge.controller`, `LeftDoorOpen.anim`
- `Assets/Members/Alan/TableToRack.anim`, `SlottedTableSingleBuffer (U).controller`

**Reuse / reference**
- `Assets/Meshes/ASRS.fbx`, `Assets/Prefabs/ASRS Variant 1.prefab`,
  `Assets/Meshes/Remake/00-0424-0000_ASRS36x2.obj` — rack models
- `Assets/Game_Objects/RobotArm.prefab`, `PneumaticGripper.prefab` — arm + gripper (present, unused by M1 flow)
- `Assets/Scripts/Job_System/` — shared FIFO pattern used elsewhere, not yet connected to `Item_ASRS`
- `Assets/Members/Colin/Training/Scripts/` — `Lesson_Sequencer`/`Lesson_Controller`/`Module_Loader` (see §5
  for open question on whether this governs the ASRS scene)

**Superseded / dead**
- `Assets/Scenes/ASRSTraining(OLD).unity` — prior scene version
- `Assets/Members/Alan/RackScanner.cs` — fully commented out, replaced by `Item_RFID_Sensor`

---

## 7. Verification

1. **Identification (steps 1-3):** each of Template, RFID Tag marker, and Epoxy Block highlights/labels
   correctly and advances `ASRSM1_Lesson` on the correct interaction.
2. **Load (step 4):** triggering the load plays `EpoxyToTable.anim` and flips the template's state from
   Empty to Loaded (`Item_Slotted_Table.Item` becomes non-null).
3. **Door (steps 6-7):** clicking/interacting with the Door Handle plays `LeftDoorOpen.anim` end-to-end in
   VR (not just in-editor) — this is the currently-unverified item flagged in §5.
4. **Transport + stow (steps 8-9):** the loaded template moves into the target empty cell
   (`TableToRack.anim` or successor), the door closes, and `Item_ASRS.TableMap`/`SlotInsert` reflects the
   cell as Occupied.
5. **Lesson coverage:** confirm whether `Lesson_Sequencer` or `SequenceManager` is the system of record (see
   §5) and author the missing steps (load, door, transport, close) into whichever is canonical so the full
   9-step sequence is step-gated, not just steps 1-3/5.
