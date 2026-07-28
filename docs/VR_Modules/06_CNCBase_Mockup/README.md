# 06 — CNCBase Scope-Reduced Mockup

An in-VR mockup of Intelitek **CNCBase**, the mill-station control software for the ProMill 8000.
"Scope-reduced" is the whole design rule: **the mockup implements only the CNCBase functions that have a
physical counterpart in the Unity scene.** If a control has nothing in-scene to act on or read from, it is
not built.

| Doc | Contents |
|---|---|
| `README.md` (this file) | Direction, locked decisions, the scope rule, what changes vs today, risks |
| `01_Screen_Spec.md` | Screen/control inventory — every control mapped to the scene object it drives |
| `02_Architecture.md` | Script design, data flow, machine↔world axis mapping, integration with the lesson engine |
| `03_Build_Plan.md` | Phased build with verify criteria, lab-verification checklist, promotion path |

Context: `00_Program_Overview.md` (program), `03_Module2_Startup_Plan.md` (the module this lands in),
`05_Module_Framework_HOWTO.md` (framework rules), `04_Interaction_Ideas_Backlog.md` (physical-interaction
ideas that build on top of this).

---

## 1. Locked decisions

| # | Decision | Consequence |
|---|---|---|
| **D1** | **Mill station only.** CNCBase screen + the ProMill 8000 controls it maps to + the mill PC. | The SCORBASE panel and the whole arm station are untouched. `Startup_State_Controller` keeps owning them. |
| **D2** | **The panel drives the mill live.** Jog moves real axes, the DRO reads real positions, Run drives `MillingAnimation`. | The panel is a *window into the machine*, not a scripted readout. Biggest teaching value; the largest share of the work. |
| **D3** | **Built in `Assets/Members/Colin/Training/`, promoted later.** | Follows the HOWTO ownership rule now; `03_Build_Plan.md` §5 documents the promotion checklist for when M4/M6 need it. |

## 2. The scope rule

> A CNCBase control ships in the mockup **iff** clicking it changes something the trainee can see on the
> ProMill 8000 in the scene, **or** it reads a value the machine actually has.

Two corollaries:

- **No invented machine state.** The DRO reads `AxisMovement.OffsetFromOrigin`; run status reads
  `MillingAnimation.IsPlaying`. Nothing is faked into a string.
- **Fidelity is functional, not pixel-level.** This does *not* reverse
  `04_Interaction_Ideas_Backlog.md` §"Explicitly out of fit", which rules out a pixel-accurate CNCBase
  recreation. We keep real CNCBase **terminology, mode model, and control grouping**; we do not reproduce
  its Windows chrome, menu bar, or NC editor. Wording in that backlog entry should be tightened to
  "pixel-accurate" rather than "simplified panels only" once this plan is accepted.

## 3. What ships vs what is cut

Grounded in Intelitek's own CNCBase feature list ([product page](https://intelitek.com/cncbase/)).

| Real CNCBase capability | In-scene counterpart | Verdict |
|---|---|---|
| **Online** (talks to the controller) vs **Simulation** (no hardware) | Whether panel commands reach the `AxisMovement`s | **IN** — the core M2 teaching point, and it finally makes the `Simulation` distractor *behave* wrong instead of just scoring wrong |
| Manual movement per axis, custom **step** and **speed** settings | `AxisMovement.MoveBy` / `MoveToOffset`, `speed` | **IN** — the Manual (jog) screen |
| Real-time position display of slides/tool | `AxisMovement.OffsetFromOrigin` × 1000 → mm | **IN** — the DRO, always-visible in the header |
| Machine Home / reference | `AxisMovement.ResetToOrigin()` + a `homed` flag | **IN** — DRO shows `---` until homed |
| Configurable **soft limits** | `enableLimits` + the training overrides (±0.14 / ±0.076 / −0.27…0 m) | **IN** — hitting one raises a panel alarm line |
| Program execution display: block being executed, run time | `MillingAnimation` phase + a stopwatch | **IN**, reduced to a canned block list |
| Spindle activation & speed | *Nothing spins today* | **IN, minimal** — add a spin on `SpindleMotor`; see `01` §5 |
| Manual override of feed rate | `AxisMovement.speed` scale | **IN, small** — a 3-position feed override |
| NC code color editor, block numbering, comment management | Nothing — **G-code writing was dropped from the roadmap** (`00` §1) | **OUT** |
| Graphic tool-path verification | Would need a second render target | **OUT** — in VR the real mill *is* the verification |
| Estimate Runtime | Nothing | **OUT** |
| Tool library / tool offsets | One tool modeled | **OUT** — static `T1` readout only |

## 4. What changes vs the panel that exists today

Today `M2_Module_Builder.cs:176-210` builds a 2-tab CNCBase canvas of 7 buttons, and
`Startup_State_Controller.cs:101-127` writes its readouts as literal strings. After this plan:

- The canvas grows a **persistent header** (mode + 3-axis DRO + status/alarm line) and a **4th screen**
  (Manual jog).
- A new `CNCBase_Machine_Link` becomes the **single component allowed to touch the mill**, and the new
  `CNCBase_Panel` becomes the **single writer of CNCBase readouts**. `Startup_State_Controller`'s mill
  `case` bodies stop setting text and instead forward to the panel — its arm/SCORBASE half is unchanged.
- Cold reset gains real work: axes return to origin, `MillingAnimation.Stop()`, doors close.
- **No new `Lesson_Step_Kind`.** Every interaction is still `Panel_Action` through
  `Startup_Action_Button` → `Action_Button_Registry` → `Lesson_Sequencer.Notify_Action`. The lesson engine
  is not modified at all.

## 5. Direction — why live is worth it

Three things only work if the panel really drives the machine:

1. **Simulation stops being a trick question.** Today picking `Connect: Simulation` is scored wrong and
   nothing else happens. Live, the trainee picks Simulation, presses jog, and *the mill does not move* —
   the concept teaches itself.
2. **Homing becomes meaningful.** "Nothing is trustworthy before homing" is currently a sentence in a
   prompt. Live, the DRO reads `---` and refuses to display coordinates until Machine Home has run.
3. **It is the substrate for the physical backlog.** `04_Interaction_Ideas_Backlog.md` §2 wants
   `Turn_Knob` handwheel jog and `Throw_Switch` power controls. Both need exactly one thing that does not
   exist yet: a command surface that converts an input into real axis motion. That is
   `CNCBase_Machine_Link`. Building it here means the handwheel later is a new *input*, not a new *system*.

## 6. Risks

| Risk | Mitigation |
|---|---|
| **Machine↔world axis confusion.** The model's node names lie (`PB_XY_Saddle` is a cover plate); machine X/Y/Z are world X/Z/Y. | The mapping is declared **once**, in `CNCBase_Machine_Link`'s serialized fields, named by *machine* axis. No UI code ever names a Unity axis. `02` §3. |
| **`OffsetFromOrigin` is relative to the pose at `Awake()`**, which for an additively-loaded module scene is the builder-authored pose — not a real machine home. | Home = `ResetToOrigin()`, and the DRO is gated behind a `homed` flag. Whether machine coordinates should read zero-at-home or zero-at-a-travel-extreme is a lab question (`03` §4). |
| **Two writers fighting over the readouts** (`Startup_State_Controller` vs the panel). | Hard rule: the panel is the only writer. The state controller forwards, never formats. |
| **Practice mode leaking answers.** A live machine gives feedback that a scripted panel did not. | Feedback must confirm/deny *after* the action, never preview it — the `04` §1 contract. Distractors must produce a plausible wrong outcome, not a "wrong!" message. |
| **Scene regeneration wipes hand-tuned panel layout.** | Everything is authored in `M2_Module_Builder.BuildScene()`. Tune in-editor, then bake back before rebuilding (`05` Rules & gotchas). |
| **Scope creep back toward a CNCBase clone.** | §2's rule is the test. A control with no in-scene counterpart is either cut or rendered as a non-interactive affordance. |

## 7. Open questions

Answers change the spec, not the architecture — none of these block the build starting.

1. **Terminology:** the current panel says `Connect: Active`; Intelitek's own wording is **Online**. Confirm
   against the lab's CNCBase build before renaming (the M2 lesson text and `Action_Id`s would follow).
2. **Jog step sizes** the lab actually uses (proposed 0.1 / 1 / 10 mm).
3. **Machine-coordinate zero** — at home, or at a travel extreme (`03` §4).
4. **`start_fms.nc` behavior** — modeled as a no-motion wait-loop; still unconfirmed (`03_Module2` checklist).
5. **Jog pendant / FANUC panel.** The ProMill 8000 has ports for both. If the lab uses one, a *physical*
   jog control is a better mockup target than software jog buttons — it would consume the same
   `CNCBase_Machine_Link` API.

Sources: [CNCBASE® — Intelitek](https://intelitek.com/cncbase/) ·
[ProMill 8000 CNC Machining Center — Intelitek](https://intelitek.com/promill-8000-cnc-machining-center/) ·
[ProMill 8000 User Manual (ManualsLib)](https://www.manualslib.com/manual/1335769/Intelitek-Promill-8000.html)
