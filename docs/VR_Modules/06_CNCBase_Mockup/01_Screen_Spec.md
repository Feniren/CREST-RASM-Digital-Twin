# 01 — Screen Spec

Every control in the mockup, and the scene object it drives or reads. A row exists here **only** because
the right-hand column is non-empty — that is the scope-reduction rule from `README.md` §2.

---

## 1. Scene inventory (what the mill station actually has)

Verified against `M1_Module_Builder.cs` and `M2_Module_Builder.cs`, not from memory.

| Scene object | Path / component | What it can do |
|---|---|---|
| Mill assembly | `PM8000_Training.prefab` (mill + vise + `Demo_Block`) | Instantiated by M2 at `(1.681, 0, 3.44)`, rot Y 270° |
| Machine **X** — table | `Worktable_Base/WB_XAxis_Drive`, `AxisMovement` (world **X**) | ±0.14 m (**280 mm**), speed 0.05 |
| Machine **Y** — saddle | `Worktable_Base/WB_YAxis_Drive`, `AxisMovement` (world **Z**) | ±0.076 m (**152 mm**), speed 0.05; carries the X stage via `dependents` |
| Machine **Z** — spindle | `SpindleBase/SpindleMotor`, `AxisMovement` (world **Y**) | −0.27…0 m (**270 mm**), speed 0.1 |
| Canned cycle | `MillController`, `MillingAnimation` | `Play()` / `Stop()` / `IsPlaying` — plunge, square pocket, retract |
| Guard doors | `doors`, `Item_Mill_Doors` + `Door_Click_Toggle` | `AlternateInteract()` slides both leaves along world Z |
| Vise + workpiece | `DualAxisVice`, `Demo_Block` | Appended to X and Y `dependents` — they ride the table |
| Main power | `kaig` part, clickable via `BuildPartAction` | `mill_power_on` action; tinted green by `Startup_State_Controller` |
| E-stop | `emergency_stop` marker (M1) | Currently identification-only; reserved for M3 |
| Mill PC + monitor | `Mill_Computer`, `Mill_Monitor` props | Host the panel canvas and the `mill_pc_on` power button |
| Panel canvas | `CNCBase_Panel` world canvas, 520×460 px @ 0.0009 | Where everything below lives |

**Not in the scene, therefore not in the mockup:** tool changer / tool library, coolant, chip conveyor,
probe, second workpiece fixture, pendant.

## 2. Panel layout

One canvas. A **persistent header** (always visible — it is CNCBase's real-time display) over a
**tab strip** of four screens (CNCBase's dialog boxes). Tabs reuse the existing `Panel_Tab_Group`, so the
guided highlight can already reveal the tab holding the current step.

```
┌──────────────────────────────────────────────┐
│ CNCBase                          T1   ⏱ 00:00│  ← title · tool · run time
│ Mode: OFFLINE                                │  ← OFFLINE / ONLINE / SIMULATION
│ X   ---.--   Y   ---.--   Z   ---.--    mm   │  ← DRO — dashes until homed
│ ────────────────────────────────────────────  │
│ Status: not launched                         │  ← status / alarm line
├──────────────────────────────────────────────┤
│ [ Connect ] [ Manual ] [ Program ] [ Setup ] │  ← Panel_Tab_Group
├──────────────────────────────────────────────┤
│                                              │
│              (active tab content)            │
│                                              │
└──────────────────────────────────────────────┘
```

The header replaces today's two loose readouts (`Mill_Status`, `Mill_Readout`) and is the **only** place
machine state is displayed. `Setup` is a read-only tab (§6) — it exists so the panel does not look like a
toy, and it costs almost nothing.

## 3. Tab: Connect — *mode and reference*

The M2 startup path. All four ids already exist in `M2_Lesson`; only their *effects* change.

| Control | Action_Id | Drives | Reads back |
|---|---|---|---|
| **Launch CNCBase** | `cncbase_launch` | Panel wakes: `CanvasGroup.alpha` 0.25 → 1 | `Mode: OFFLINE`, `Status: select connection` |
| **Connect: Online** | `cncbase_online` | `Link.Online = true` — commands now reach the axes | `Mode: ONLINE` |
| **Connect: Simulation** | `distractor_simulation` | `Link.Online = false` — **commands are accepted and do nothing** | `Mode: SIMULATION` |
| **Machine Home** | `cncbase_home` | `ResetToOrigin()` on X, Y, Z sequentially; sets `Homed` | DRO switches from `---.--` to live mm; `Status: homed` |

- **Simulation is the teaching moment.** It is not blocked and not scolded. The trainee selects it, goes to
  Manual, presses jog — and the table does not move. `Status: SIMULATION — no machine motion` is the only
  hint, and it appears *after* the attempt, never before (the practice contract in `04` §1).
- **Homing gates everything.** Jog and Run are rejected with `Status: home the machine first` until
  `Homed`. This is real CNC behavior and it is the module's stated teaching point.
- Terminology: the current lesson says `Connect: Active`. Intelitek's wording is **Online**. Renaming is
  pending lab confirmation (`README.md` §7.1) — the `Action_Id`s stay stable either way.

## 4. Tab: Manual — *jog*

New. This is the tab that only exists because the panel drives the machine.

| Control | Drives | Notes |
|---|---|---|
| **X− / X+**, **Y− / Y+**, **Z− / Z+** | `Link.Jog(axis, ±step)` → `MoveToOffset(OffsetFromOrigin + step)` | **Animated**, not teleported — see `02_Architecture.md` §4 |
| **Step:** `0.1` / `1` / `10` mm | Panel-local field; no machine call | Matches CNCBase's "customized step settings". Sizes pending lab confirmation |
| **Feed:** `25% / 50% / 100%` | Scales `AxisMovement.speed` from the baked baseline | CNCBase's "manual override of programmed feed rate" |
| **Spindle On / Off** | `Link.Spindle(bool)` → spins `SM_Rotating` | See §5 |
| **Doors Open / Close** | `Item_Mill_Doors.AlternateInteract(null)` | Already works; surfaced here so jogging with the guard open is visible — and available to M3 |

Behavior rules:

- A jog that would exceed a soft limit is **clamped by `AxisMovement` itself** (`enableLimits` is already
  configured) and the panel raises `Status: soft limit — X`. Nothing new is written to guard travel; the
  existing limits *are* the machine's travel envelope (280 / 152 / 270 mm).
- Jog is **step-only in v1** (one press = one step). Press-and-hold continuous jog is deferred; it needs a
  pointer-down/up button rather than `Startup_Action_Button`'s `onClick`, and the handwheel idea in
  `04` §2 (`Turn_Knob`) is the better version of that interaction anyway.
- Jog buttons are **not** lesson steps in M2 — they carry no `Action_Id` unless a future step grades them.
  They are free exploration, which is exactly what the guided phase currently lacks.

## 5. Spindle — the one addition that has no counterpart yet

Nothing in the scene spins. CNCBase's "spindle activation and speed control" is otherwise unbuildable.

**Recommendation: build the minimum.** `SpindleBase/SpindleMotor/SM_Rotating` is already a separate node
(it is the M1 `spindle_motor` marker). A ~15-line `CNCBase_Spindle.cs` rotating it about its local axis at
an RPM-scaled rate satisfies the scope rule and makes `MillingAnimation` read as cutting rather than
gesturing.

**If that is cut**, the Spindle controls become a read-only `Spindle: —` row in Setup, not a dead button.
Do not ship a button that does nothing.

## 6. Tab: Program — *run and monitor*

| Control | Action_Id | Drives | Reads back |
|---|---|---|---|
| Program list — `start_fms.nc` | `run_start_fms` | **No motion.** Sets `Running` + wait-loop state | `Status: RUNNING start_fms.nc — waiting for cell command`, run-time clock ticks |
| Program list — `part_042.nc` | `distractor_prog1` | `MillingAnimation.Play()` — the real pocket cycle | Block readout steps through the canned cycle; run time ticks |
| Program list — `calib_probe.nc` | `distractor_prog2` | Rejected: `Status: ALARM — no probe installed` | A wrong choice with a *machine* consequence |
| **Stop** | — | `MillingAnimation.Stop()`, clears run state | `Status: STOPPED` |
| **Confirm: Running** | `verify_mill` | M2's verification step; pops `Demo_Block` onto the vise | Unchanged from today |
| Block readout | — | Reads `MillingAnimation` phase | `N040 G01 X-50.0 F120` — canned, one line per cycle phase |

`start_fms.nc` running **without motion** is deliberate and matches the M2 plan's model of it as a local-
control wait-loop. It is also the cheapest correct answer: a wait-loop that visibly milled a part would be
wrong. Flagged for lab confirmation.

`part_042.nc` is where the existing `MillingAnimation` finally gets used inside M2 — a distractor that
machines a part when the cell is not ready is a far better error than a button that greys out.

**Run requires `Homed`.** `MillingAnimation` uses absolute `MoveToOffset` calls from the origin, so
running it from a jogged position is coherent — but a real control refuses to run un-homed, and gating it
keeps the DRO honest.

## 7. Tab: Setup — *read-only*

No interactions. Four rows read straight off the link:

```
Machine        ProMill 8000
Tool           T1  (Ø6 mm end mill)
Soft limits    X ±140.0   Y ±76.0   Z -270.0…0.0  mm
Rapid feed     X 50   Y 50   Z 100  mm/s
```

Values are read from the `AxisMovement` components at start, **not typed in** — if someone rebalances the
travel limits in the builder, this screen follows automatically. That is the scope rule applied to a screen
that would otherwise be decoration.

## 8. Physical controls on the mill (unchanged, listed for completeness)

| Control | Action_Id | Owner |
|---|---|---|
| Mill PC power button (prop canvas) | `mill_pc_on` | `Startup_State_Controller` — wakes the monitor |
| ProMill 8000 main power (`kaig` part) | `mill_power_on` | `Startup_State_Controller` — tints the switch green |
| E-stop | — | Reserved for M3 (`04` §M3) |

The panel must be **dark and inert until both** `mill_pc_on` and `mill_power_on` have fired — a powered PC
with an unpowered machine can show CNCBase but must not connect. Today only the PC gates the screen; this
adds the machine-power gate, which is one boolean and a truer picture of the station.
