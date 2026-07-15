# Module 2 — "System Startup & Program Execution" — Build Plan

**Scope:** Category 2 — Development. See `00_Program_Overview.md` for the program-wide context, lesson
format, and shared foundation. New content from the June 2026 restructure (not in the docx); it replaces the
G-Code Programming module — trainees **run** NC programs but never write G-code.
**Target:** PC VR — Quest 3 via Link (Standalone OpenXR).
**Procedure sources:** official Intelitek documentation (see §Sources) plus lab-specific conventions —
anything not confirmed by a manual is flagged in §Lab Verification Checklist.

---

## Objective & Pass-Gate

**Learning objective:** the trainee performs the full cold-start of the cell in three phases — (1) powers on
both station PCs, the robot controller, and the ProMill 8000; (2) launches SCORBASE and CNCBase; (3) brings
the robotic arm active in SCORBASE (control on, Search Home sync, standalone mode) and verifies it, then
brings the ProMill 8000 active in CNCBase (active, homed, running `start_fms.nc` for local control) and
verifies it.

**Pass-gate:** complete the **entire startup sequence unaided** in the practice phase. Wrong-order actions
count as errors. The module is complete only when the end state is reached: **all systems active + a
verification action performed**.

**End state ("all systems active"):**
- Arm station: SCORBASE running, control on (active), all axes home-synced, **standalone** mode set.
- Mill station: CNCBase running, connected active, mill homed, `start_fms.nc` executing (local-control
  wait-loop).
- Verification: arm responds to a jog command; mill status panel confirms program running.

---

## The Cell (what's in the scene)

Two stations, each with a dedicated PC:

| Station | Hardware | Software | Role |
|---------|----------|----------|------|
| Arm | Yaskawa Motoman GP8 + robot controller* | SCORBASE | Robot control; syncs/homes the arm; standalone vs CIM-managed mode |
| Mill | ProMill 8000 | CNCBase | Mill control; homing; opening and running NC programs |

\* Controller is a **placeholder** for now — modeled as a generic robot controller box with a power switch.
The real controller model and its power-up behavior are flagged for lab verification. (Note: SCORBASE is
documented by Intelitek for SCORBOT arms; this cell pairs it with the Motoman GP8 — the procedure shape
holds, arm-specific steps need lab confirmation.)

**Key concepts taught:**
- **Active vs offline (SCORBASE):** active = the software controls the real controller in real time
  (Options | Control On); offline = simulation only, nothing moves.
- **Sync / Search Home:** each axis is driven to its home switch sequentially, initializing the encoder
  reference ("hard home"). All positions are meaningless until this completes — per-axis confirmation marks
  appear as each axis homes.
- **Standalone vs CIM-managed:** standalone = the station runs under its own software; CIM-managed = the
  station obeys the cell-level manager (OpenCIM/OpenMES) over the network. This module uses **standalone**;
  CIM-managed total-assembly-line control is deferred module M5.
- **Local control via `start_fms.nc`:** running this program puts the mill into a wait-loop where it waits
  for cell-level commands — the mill station manages itself until told otherwise.
- **Startup order:** every system is powered first and both control programs launched, then the **arm** is
  brought fully active, homed and verified **before** the mill is brought active and run — the robot must be
  in a known, homed state before mill operations (documented CIM-cell practice).

---

## Guided Phase — Step Sequence

The canonical procedure. Step list follows the `Lesson_Step` conventions from `01_Module1_Plan.md`
(`Step_Id`, kind, prompt text); software steps use **simplified panels** — real terminology and correct
step order on world-space button panels, not pixel-accurate SCORBASE/CNCBase recreations.

| # | Step_Id | Kind | Interaction | Teaching point |
|---|---------|------|-------------|----------------|
| 0 | `intro_cell` | Info | — | Tour of the two stations: arm PC + controller + GP8; mill PC + ProMill 8000. The three-phase startup (power all → launch both → bring each active & verify). |
| **Phase 1 — power on every system** |||||
| 1 | `arm_pc_on` | Panel_Action | Arm-station PC power button | Each station has a dedicated PC; boot it first. |
| 2 | `arm_controller_on` | Panel_Action | Robot controller power switch (the black box) | The controller drives the arm; SCORBASE talks to it. |
| 3 | `mill_pc_on` | Panel_Action | Mill-station PC power button | The mill station needs its own PC running too. |
| 4 | `mill_power_on` | Panel_Action | ProMill 8000 main power (the real `kaig` switch, clickable in-world) | Mill hardware power. Every system is now powered. |
| **Phase 2 — launch the control software** |||||
| 5 | `scorbase_launch` | Panel_Action | "SCORBASE" icon on arm-PC screen | Arm-station control software. |
| 6 | `cncbase_launch` | Panel_Action | "CNCBase" icon on mill-PC screen | Mill-station control software. Both programs are now up. |
| **Phase 3 — bring each machine active, home, and verify** |||||
| 7 | `scorbase_online` | Panel_Action | **Control On** button → status shows *Active* | Active vs offline: only active commands move hardware. |
| 8 | `scorbase_home` | Panel_Action | **Search Home — All Axes** → wait for per-axis check marks | Sync: encoder reference is established axis-by-axis; nothing is trustworthy before homing. |
| 9 | `scorbase_standalone` | Panel_Action | Mode toggle → **Standalone** | Standalone vs CIM-managed; CIM mode teased for a later module. |
| 10 | `verify_arm` | Panel_Action | **Test Move (A1)** — jog one axis and back | Prove the arm responds — active, homed, standalone. |
| 11 | `cncbase_online` | Panel_Action | **Connect: Active** (vs Simulation distractor) | Active means the real machine, not simulation. |
| 12 | `cncbase_home` | Panel_Action | **Machine Home / Reference Point** → Home | Mill homing establishes the factory reference; all positioning is relative to it. |
| 13 | `run_start_fms` | Panel_Action | **Open** → select `start_fms.nc` → **Run Program** | Local control: the mill enters a wait-loop, ready for cell commands. |
| 14 | `verify_mill` | Panel_Action | **Confirm: Running** — status readout shows `start_fms.nc` | Prove the mill is in local control; the arm loads the first workpiece onto the vise (demo block). |
| 15 | `guided_done` | Info | — | "All systems active." Recap of the order and why. Transition to practice. |

---

## Practice Phase — Scoring

Same scene, same interactables, **no prompts or highlights**:

- The trainee performs the full sequence from cold (everything reset to powered-off).
- **Order-sensitive:** each action is checked against the canonical sequence. An out-of-order action (e.g.,
  launching control software before every system is powered, or bringing the mill active before the arm is
  homed and verified) is recorded as an error and the action does not take effect.
- **Pass condition:** end state reached (all systems active + both verification actions). Errors are
  reported on the results panel; the run completes regardless of error count, but the module is only marked
  complete on a run with the end state achieved.
- Score persists via `Lesson_Controller` / `Save_Data_Interface` like M1.

---

## Simplified-Panel UI Spec

World-space canvases on the two in-scene PC monitors (TextMeshPro + XRI UI, per
`02_HUD_and_Interaction_Pattern.md` — wrist HUD, billboarded prompts, and highlight affordances are reused
unchanged, not restated here).

**SCORBASE panel (arm PC):**
- Title bar: "SCORBASE"
- **Control On** button → status line `Offline` → `Active`
- **Search Home — All Axes** button → per-axis rows with check marks appearing sequentially
- Mode toggle: **Standalone / CIM** (CIM disabled with tooltip "Module 5")

**CNCBase panel (mill PC):**
- Title bar: "CNCBase"
- Connect choice: **Active / Simulation** (Simulation is a wrong choice in this lesson)
- **Machine Home** button → axis position readout zeroes
- **Open** → file list containing `start_fms.nc` (plus distractor files in practice mode)
- **Run Program** → status line `Running: start_fms.nc`
- **Jog** controls (used only for the verification step on the arm panel's counterpart; mill jog optional)

Both panels are dark/inert until their PC's power button has been pressed (screen-off material → UI).

---

## Lab Verification Checklist

Flagged items — documented from standard Intelitek practice or lab convention, **not** confirmed against
this lab's hardware. Verify on-site before the module is considered accurate:

- [ ] **Exact hardware power order** — documented default: PC → controller → software → active → home. Any
      lab deviation (e.g., controller before PC, main breaker step) supersedes this.
- [ ] **Robot controller model** for the Motoman GP8 (placeholder in the scene) and its power-up behavior
      (boot time, status lights, pendant involvement).
- [ ] **SCORBASE + GP8 pairing** — confirm the lab's SCORBASE build drives the GP8 and what its
      active/home/standalone controls actually look like on that build.
- [ ] **`start_fms.nc` contents** — modeled as a wait-loop for cell commands ("local control"); not in any
      official Intelitek manual, so confirm what it actually does and where it lives on the mill PC.
- [ ] **Standalone-mode setting** — confirm how/where standalone vs CIM mode is actually set in the lab
      (SCORBASE menu vs OpenCIM device-driver configuration).
- [ ] **Verification actions** — confirm the lab's accepted "it's alive" checks (jog distance/axis, mill
      status indication).

---

## Files

**New**
- `Assets/Scenes/Training/Module2_Startup.unity` (loaded additively)
- `M2_Lesson` asset (Lesson_Definition) + one new step kind, `Panel_Action` (ordered button/switch actions)
- Two PC-monitor panel prefabs (SCORBASE panel, CNCBase panel)
- Placeholder robot-controller prop with power switch; PC tower/monitor props with power buttons

**Reuse / extend**
- Bootstrap scene, `Module_Loader`, `Lesson_Sequencer`, `Lesson_Controller` (built in M1 — unchanged)
- `Training_Builder.cs` — extend generation to Module 2 (scene, markers, lesson asset, menu button)
- Mill prefab (`reconstructedPM8000`), rigged GP8 arm model, `02_HUD_and_Interaction_Pattern.md` UX layer

---

## Verification (build-time)

1. **Guided flow:** each of the 16 steps gates correctly — the target interactable highlights, the prompt
   shows, only the correct action advances; software panels stay inert until their PC is powered.
2. **Order gating:** in guided mode, out-of-order actions do nothing; in practice mode they log an error.
3. **End state:** completing the final step (`verify_mill`) flips the module to "all systems active" and
   fades the workpiece onto the vise; results panel shows errors.
4. **Practice scoring + persistence:** unaided run records errors and completion; module completion persists
   across restart via `Save_Data_Interface` / `Data_Loader`.
5. **Reset:** re-entering the module returns everything to cold (powered-off) state.

---

## Sources

Official Intelitek documentation grounding the procedure (claims not covered here are in the Lab
Verification Checklist):

- SCORBASE User Manual (v7+, ER-4u/ER-2u) — online/offline modes, Control On, Search Home:
  <https://downloads.intelitek.com/Manuals/Robotics/ER-4u/Scorbase_USB_I.pdf>
- Controller-USB User Manual (Cat. #100341) — homing/encoder hard-home behavior:
  <https://downloads.intelitek.com/Manuals/Robotics/ER-4u/Controller-USB-H.pdf>
- ProMill 8000 User Manual — CNCBase startup, Machine Home, opening/running NC programs:
  <https://www.manualslib.com/manual/1335769/Intelitek-Promill-8000.html>
- CNCBase & CNCMotion datasheet:
  <https://www.intelitek.com/resources/pdf/35-1007-3200_DS_SW_CNCB-M_Ver_F.pdf>
- OpenCIM User Guide — device drivers, standalone vs CIM-managed stations:
  <https://download.intelitek.com/Manuals/CIM-FMS/OpenCIM_User_guide_M.pdf>
- OpenMES User Guide (cell-level manager, successor to OpenCIM Manager):
  <https://downloads.intelitek.com/Manuals/CIM-FMS/OpenMES_User_Guide.pdf>
- CIM-cell startup/homing order (robot first, then mill) — IIT Kharagpur CIM lab procedure:
  <http://vlabs.iitkgp.ernet.in/vlabs/rtvlab1/cimprg.html>
