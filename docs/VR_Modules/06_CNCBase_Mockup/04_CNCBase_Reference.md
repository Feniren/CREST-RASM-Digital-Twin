# 04 — CNCBase Reference Capture

Closes **P0 — Reference capture** in `03_Build_Plan.md`. This document records what Intelitek's own
documentation says CNCBase *actually is*, and then does the uncomfortable half: it lists where
`01_Screen_Spec.md` and `02_Architecture.md` guessed, and where they guessed wrong.

**Nothing here was captured from the lab PC.** Every claim is sourced to a public Intelitek document or to
the product-page screenshot, and every claim carries a confidence rating. Rows marked 🔴 still require
someone standing at the machine.

---

## 0. Source list and how far to trust it

| # | Source | What it gives | Trust |
|---|---|---|---|
| **S1** | [ProMill 8x00 User Guide (`User_Guide_ProMill_8x00_C.pdf`)](https://downloads.intelitek.com/Manuals/CNC/Machines/User_Guide_ProMill_8x00_C.pdf) | Full CNCBase main-menu tree, panel descriptions, homing, jog, E-stop, interlocks | **High** — Intelitek's own manual for this exact machine |
| **S2** | [BenchMill 6x00 User Guide](https://downloads.intelitek.com/Manuals/CNC/Machines/User_Guide_BenchMill%206x00_C.pdf) | Same software chapter, differently worded; used to cross-check S1 | **High** for software, **not applicable** for machine specs |
| **S3** | [CNCBase & CNCMotion datasheet (`35-1007-3200`)](https://www.intelitek.com/resources/pdf/35-1007-3200_DS_SW_CNCB-M_Ver_F.pdf) | Marketing feature list; the source `README.md` §3 was already built on | **High** for feature existence, **low** for UI detail |
| **S4** | [CNCBASE® product page](https://intelitek.com/cncbase/) + its embedded screenshot | The only actual picture of the running UI | **High** for layout, **⚠ see §1 on vintage** |
| **S5** | [ProMill 8000 hardware datasheet `35-1006-7600_DS_HW_CNC8000_Ver_M.pdf`](https://www.intelitek.com/resources/pdf/35-1006-7600_DS_HW_CNC8000_Ver_M.pdf) | The current machine spec table, from Intelitek | **Highest** — primary, and the newest revision of anything here |
| **S5b** | [Labora Teknika](https://labts.co.id/product/promill-8000-cnc-machining-center/) · [Uruhu Highlands](https://www.uruhuhighlands.net/product-page/promill-8000-cnc-machining-center-intelitek) | Two independent distributor mirrors of S5, agreeing with it | **High** — corroboration only |
| **S6** | [iCNC download index](https://downloads.intelitek.com/Software/CNC/Control_Software/) | Current installer version and OS support | **High** |
| **S7** | [CNC Milling Technology with ProMill 8000 curriculum](https://www.intelitek.com/resources/pdf/35-1007-7700_DS_EL_CNC-Milling8000-virtual-lab_Ver_F.pdf) | Intelitek's own module ordering for this machine | **High** |

> **The one document we could not get is the CNCBase software manual itself.** Intelitek's public tree
> (`downloads.intelitek.com/Manuals/CNC/`) carries machine user guides and an iCNC silent-install PDF, but no
> standalone CNCBase reference. S1's "Using the Control Software" chapter is the closest thing that exists
> publicly, and it is what most of this document rests on.

## 1. ⚠ Read this before using §3

The product-page screenshot (**S4**) is titled **"CNCBase for Benchman VMC Machining Center"**, runs under a
Windows 98/2000-era shell, and shows a machine Intelitek no longer sells. It is genuine CNCBase and its
*panel model* is corroborated by S1 and S2 — but it is not a picture of what is on the lab PC.

The current shipping product is **iCNC v6.1.4.23**, released 2025-05-22, Windows 10/11 only, and CNCBase and
CNCMotion install together from one package (**S6**). Treat §3's pixel-level details as *the shape of the
thing*, not as a skinning target — which is fine, because `README.md` §2 already rules out pixel fidelity.

**Consequence for the build:** none of P1–P5 changes. The corrections in §6 are about *behavior model* and
*terminology*, and those are corroborated by S1, not by the screenshot.

## 2. What CNCBase is

The control application for Intelitek's four benchtop CNC machines (BenchMill 6100, BenchTurn 7100,
ProMill 8000, ProTurn 9000). It accepts Fanuc-compatible EIA RS274D G&M code, and it includes a
**FANUC 21i emulator** — replicating the FANUC 21i controller and its 16i/18i subset — switchable "with a
mouse click" (**S3**).

CNCBase is *included* with the machine. **CNCMotion**, the 3D simulation half, is a **separately-priced
SKU** even though the two install from one package (**S3**, **S6**). Do not assume the lab has CNCMotion.

Two operating modes, and this is Intelitek's exact wording (**S3**):

- **Online** — "CNCBase communicates with the controller"
- **Simulation (Offline)** — "you can simulate the machining process with graphic verification and simulated machining"

The ProMill 8000 talks to the PC over **Ethernet**, configured with a "Machine IP Configuration utility"
(ProMill quick-start guide). It is not a serial link. Worth knowing if the module ever wants to show *why*
Online can fail.

## 3. The real UI

### 3.1 It is an MDI app with floating panels, not a tabbed panel

This is the single biggest structural difference from `01_Screen_Spec.md`. CNCBase is a classic Win32 MDI
application: a menu bar, toolbars, an NC-editor document window, and a set of **independently toggleable
floating panels** that the user shows or hides from the **View** menu. There is no tab strip.

Observed layout (**S4**), transcribed rather than reproduced:

```
CNCBase for <machine>                                        [_][□][X]
File  Edit  View  Program  Tools  Setup  Window  Help
┌────────────────────────────────────────────────────────┐
│              Verifying 'SUPERMAN_MET.NC'               │   ← activity banner
├────────────────────────────────────────────────────────┤
│ [toolbar: new/open/save · print · ● ● ● status lamps]  │
├──────────────────────────────────┬─────────────────────┤
│ NC editor (colour-coded G-code)  │  Operator Panel  ▣ │
│   N  ; THIS FILE FOR PLM-1000... │  Jog Control     ▣ │
│   N  G00 Z2.5; TOOL ABOVE ...    │  Position        ▣ │
│                                  │  Verify          ▣ │
│  Machine Info ▣                  │                     │
└──────────────────────────────────┴─────────────────────┘
 Press CTRL-SPACE at any time to perform a keyboard Stop.
 [Homed][CAP][NUM][ 1 : 22 ][LOCK][MOD][5:19 PM]
```

### 3.2 Main menu

Confirmed independently in both S1 and S2, and against S4's visible menu bar.

| Menu | Items |
|---|---|
| **File** | New, Open, Close, Save, Save As, Print, Print setup, **Choose machine**, Save a copy of current configuration, Exit |
| **Edit** | Undo, Redo, Cut, Copy, Paste, Clear, Delete Line, Find, Replace, Goto Line, Renumber, Lock, Select Font |
| **View** | **Actual Position, Absolute Position, Machine Info, Jog Control, Operator Panel, Verify Window**, Toolbars |
| **Program** | **Run/Continue, Verify, Estimate Runtime, Pause, Feedhold, Stop** |
| **Tool** | Setup Library, Select Tool Wizard, Select Tool, Configure ATC, Operate ATC |
| **Setup** | **On-line, Simulation**, Set Position, Zero Position, **Jog Settings**, Run Settings, Verify Settings, **Set/Check Home**, Goto Position, Units, **Coordinate Systems**, Offsets, Spindle, Backlash, **Soft Limits**, Preferences |
| **Window** | Run and Edit Screen, Verify Screen, Program Screen, Close all windows |
| **Help** | Help, Tip of the day, About |

Bolded items are the ones the mockup touches. **Every** capability the mockup claims to model exists here
under a specific name — there is no need to invent a single label.

⚠ **The menu hyphenates it: `On-line`, not `Online`.** Intelitek's marketing datasheet (S3) writes
"Online"; the machine manuals write "On-line". The mockup should use whatever is on the lab screen — see
§7.1. Note also that the menu is singular **`Tool`**, not `Tools`.

### 3.3 Operator Panel — `View | Operator Panel`

| Control | Real behavior (S1/S2) | Observed in S4 |
|---|---|---|
| **Run/Continue** | Starts, or resumes after a pause | round button, labelled `Cycle Start` on the older skin |
| **Stop** | Halts the running NC program immediately | large red round button |
| **Feedhold** | Pauses immediately; **spindle keeps turning** | green round button |
| **Optional Skip** | Execute or ignore `M00` | toggle w/ LED, `Opt. Skip` |
| **Optional Stop** | Execute or ignore `M01` | toggle w/ LED, `Opt. Stop` |
| **Single Step** | Pause after each block executes | toggle w/ LED, `Sing. Step` |
| **Feed Rate Override** | actual = programmed × % | **continuous dial, 0 – 200 %**, at 100 |
| **Spindle Speed Override** | actual = programmed × % | **continuous dial, 50 – 150 %**, at 100 |

Both overrides multiply the **programmed** rate. Neither one affects jog speed — that is a separate control
(§3.4). `01_Screen_Spec.md` §4 conflates them.

⚠ The S4 screenshot is 465 × 350 px. Panel *titles*, dial *labels* and dial *ranges* read cleanly at
magnification; the round-button captions (`Cycle Start`, `Feed Hold`) are 4–5 px tall and were read at the
edge of legibility. Treat the round-button captions as probable, not certain — the **behavior** column is
from S1 and is not in doubt.

### 3.4 Jog Control — `View | Jog Control`

| Element | Real behavior (S1) | Observed in S4 |
|---|---|---|
| Axis buttons | X, Y, Z, positive and negative | arrow-shaped pad, `X` / `Y` labelled, `Z` below the crop |
| **Step size** | "Sets the step size, in inches or mm"; presets configured in `Setup \| Jog Settings` | **named preset slots `A` / `B` / `C`** (a 4th is partly visible) with the value under each: **0.025 · 0.254 · 2.54** |
| **Speed** | Presets in in/min or mm/min; also from `Setup \| Jog Settings` | named buttons — `Slow` (selected, red LED) and `Med` visible |
| **Step vs continuous** | Step: one press = one motion. Continuous: **hold** the button and the axis moves continuously | mode selector below the crop |
| **Keypad** | "Click to allow the arrow keys on the keyboard to control jog motion" | `Keypad` toggle, top of panel |
| **Handwheel** | "Click on the green button to activate the handwheel. **The arrow buttons will disappear**" | not visible on this skin |

Two things fall out of this table:

1. **`0.025 / 0.254 / 2.54` is `0.001" / 0.01" / 0.1"` expressed in mm.** The shipped defaults are imperial
   decades, not the metric `0.1 / 1 / 10` proposed in `01_Screen_Spec.md` §4. They are user-configurable, so
   the lab may well have metric decades loaded — but the *presentation* is a labelled slot (`A`, `B`, `C`)
   with the value beneath, not a value-labelled button.
2. **The handwheel is a real, first-class CNCBase feature**, not a VR embellishment. It has a dedicated
   activation control and it *replaces* the software jog buttons when engaged. This materially strengthens
   the `04_Interaction_Ideas_Backlog.md` §2 `Turn_Knob` idea: modelling a handwheel is reproducing shipped
   behavior, and the "arrow buttons disappear" detail is a free, high-authenticity interaction.

### 3.5 Position displays — `View | Actual Position` / `Absolute Position`

Four columns, not one (**S1**):

| Column | Meaning |
|---|---|
| **Absolute** | tool position in the current coordinate system |
| **Relative** | tool position relative to **Work** coordinates |
| **Machine** | tool position relative to the **machine's home position** |
| **Dist to go** | distance remaining to the end of the current line of code |

Units follow `Setup | Units` (Inch or Metric). Right-clicking the display offers **Set Position, Zero
position, Goto, Hide**. The compact floating `Position` box in S4 shows a single set of `X / Y / Z … mm`
values — so a reduced one-column DRO is a shape CNCBase itself ships.

### 3.6 Machine Info — `View | Machine Info`

Transcribed from S4, and matching S1's field list:

```
Tool    00        TDiam    3.175        X   28.883 mm
Feed    200.00    Spindle  1500.0       Y   25.935 mm
Pass    001       Coord    Work         Z   -0.500 mm
Block   17        of       22
──────────────────────────────────────────────────────
 16  G03 X28 Y26 I28 J32
 17  G02 X22 Y20 I28 J20      ← current block, highlighted
 18  G00 Z2.5; RETRACT TOOL
```

S1 adds **parts count** to this list. Three details matter for the mockup:

- **`Block 17 of 22`** plus a **scrolling three-line block window with the current line highlighted** — the
  real display is a *list with a cursor*, not the single-line readout in `01_Screen_Spec.md` §6.
- **`Coord: Work`** — the active coordinate system is displayed here, always.
- **`TDiam`** is tool *diameter*, shown next to tool number. `3.175` mm is a ⅛" cutter.

### 3.7 Status bar

Left pane: context help for the current function. In S4 it reads
**"Press CTRL-SPACE at any time to perform a keyboard Stop."**

Right panes (**S1**, confirmed visually in S4): **`Homed`** · `CAP` · `NUM` · `line : total` · `LOCK` ·
`MOD` · clock. S1 additionally documents performance panes **FR** (feed rate), **SS** (spindle speed),
**SL** (spindle load), **AP** (air pressure), **QS** (queue status).

**`Homed` is a permanent, always-visible status indicator in real CNCBase.** That is direct support for
making homing state persistent and prominent in the mockup header.

## 4. Procedures, verbatim

### 4.1 Homing — `Setup | Set/Check Home`

Also `Ctrl-H`, or the Home button on the Standard Toolbar. Opens the **Machine Home/Reference Point** window
with two choices (**S1**):

- **Home** — "send the machine to the home position at regular speed (**recommended**)"
- **Quick Home** — "send the tool to the home position at a rapid speed"

Nothing in S1 or S2 states that jog or run is *blocked* before homing. See §6, row **C4**.

### 4.2 Online ⇄ Simulation — `Setup | Online` / `Setup | Simulation`

Both sit at the top of the **Setup** menu; the active one is check-marked. To switch (**S1**):

> "click the unchecked mode. A confirmation message is displayed. Click Yes."

**The software then restarts in the selected mode.** This is not a live toggle.

### 4.3 Startup sequence (S1)

1. Machine power switch (right side panel) → ON.
2. **Safety door closed and Emergency Stop released** *before* launching in Online mode.
3. Start → All Programs → CNCBase/Motion folder → CNCBase/Motion.
4. Select **Online** or **Simulation**.
5. `Setup | Set/Check Home` → **Home**.

### 4.4 Stopping (S1) — two mechanisms, deliberately different

| Mechanism | Effect |
|---|---|
| **Hardware E-stop** (red cap, front panel; twist clockwise to release) | Immediately disconnects power. **The software loses position tracking** — re-homing required. |
| **`Ctrl` + `Space`** | Software Stop. "does not cause the software to lose track of the tool position" |

Intelitek's own instruction: press the hardware E-stop "whenever changing tools or mounting or removing a
workpiece."

This distinction is a complete, self-contained teaching point and it is currently in **no** module.
Recommend it to `04_Interaction_Ideas_Backlog.md` for M3: *why the red button costs you your reference and
the keyboard one does not.*

### 4.5 Safety interlock (S1)

> "magnetic shield interlock switch prevents the machine from operating with the shield open"

The guard door is not advisory. See §6, row **C6**.

### 4.6 Spindle (S1)

Speed comes from the `S` code in the program, or from `Setup | Spindle`. The **Outputs toolbar** carries
**Spindle Output** (on/off) and **Spindle Direction** ("only select when spindle is stopped"). So the
software *does* start the spindle — this closes the `03_Build_Plan.md` §4 checklist item.

## 5. ProMill 8000 machine specifications — verified

From Intelitek's current hardware datasheet (**S5**, Ver M), corroborated by two independent distributor
mirrors (**S5b**):

| Spec | Value | vs. the Unity scene |
|---|---|---|
| **X travel** | **280 mm** / 11″ | ✅ `±0.14 m` = 280 mm — **exact match** |
| **Y travel** | **152 mm** / 6″ | ✅ `±0.076 m` = 152 mm — **exact match** |
| **Z travel** | **270 mm** / 10.625″ | ✅ `−0.27…0 m` = 270 mm — **exact match** |
| Table | 550 × 160 mm, 3 T-slots @ 12 mm | not modelled |
| Spindle speed | 100 – 5 000 RPM | nothing spins today (`01` §5) |
| Feed rate | 2 540 mm/min (100 ipm) | — |
| Rapid traverse | 5 000 mm/min (197 ipm) | — |
| Spindle taper | ISO 20 | — |
| Max tool Ø | 10 mm | `01` §7 assumes a Ø6 mm end mill — plausible, unverified |
| Axis motors | AC servo — X 400 W, Y 400 W, Z 750 W | — |
| Throat | 232 mm / 9.130″ | — |

### 5.1 ⚠ The machine's own user guide states a different envelope

This is the most important thing in this document, and it is **not** resolved.

`User_Guide_ProMill_8x00_C.pdf` (**S1**), §2.1 *Standard milling specifications*, states:

> "y-axis travel of 6 inches (152 mm) / X-axis travel of 10.24 inches (260mm) / Z-axis travel of 7.09
> inches (180mm) / Feed rates up to 20 IPM (500mm/min) (rapid traverse up to 79 IPM (2000mm/min))"

So Intelitek publishes **two different envelopes for the same machine**:

| | Datasheet Ver M (S5) | User Guide Rev C (S1) | Unity scene |
|---|---|---|---|
| X | 280 mm | **260 mm** | 280 mm |
| Y | 152 mm | 152 mm | 152 mm |
| Z | 270 mm | **180 mm** | 270 mm |
| Feed | 2540 mm/min | **500 mm/min** | — |
| Rapid | 5000 mm/min | **2000 mm/min** | — |
| Motors | AC servo | **3-axis stepper** | — |

The same S1 page also calls the drives "3-axis stepper motors", which the datasheet contradicts with AC
servos. That pattern — stepper drives, shorter travels, 20 IPM feed — describes an **earlier revision of the
machine**, and the datasheet is the newer document (Ver M vs Rev C). The most likely reading is that the
user guide's spec block is stale and `8x00` covers a hardware generation the lab may or may not have.

**Do not treat this as settled.** The honest position:

- The Unity scene's 280 / 152 / 270 matches the **current published machine spec**. That is a good default.
- It does **not** match the manual that ships with the machine, and the delta on Z is 90 mm — a third of the
  axis. If the lab's mill is an earlier unit, jogging Z to −270 mm in VR teaches a travel the real machine
  does not have.
- `Setup | Soft Limits` lets the lab narrow the envelope further, independently of either figure.

`03_Build_Plan.md` §4's soft-limit checklist item therefore **stays open, and gets more specific**: read the
lab machine's serial plate and its `Setup | Soft Limits` dialog, not just a spec sheet. This is exactly the
kind of conflict the checklist existed to catch, and it would have been missed by trusting one source.

### 5.2 Curriculum alignment (S7)

Intelitek's own ProMill 8000 course orders its modules: *Introduction and Safety → **CNC Control Software**
→ Mounting the Workpiece → Tooling → **Reference Positions** → Verifying a Program → Running a Program →
Fundamentals of NC Programming*, then seven project modules.

Two things worth noting: **"CNC Control Software" is module 2 there too**, and **"Reference Positions" is a
module in its own right** — external validation that M2's shape, and the weight this plan puts on homing,
match how Intelitek itself sequences the material.

## 6. Corrections to `01_Screen_Spec.md` and `02_Architecture.md`

Ordered by how much they cost to fix. **C1 and C2 are the ones worth arguing about.**

| # | The spec says | The sources say | Verdict |
|---|---|---|---|
| **C1** | `Connect: Simulation` → "commands are accepted and **do nothing**", and this is the module's headline teaching moment (`README` §5.1) | Simulation is "simulate the machining process **with graphic verification and simulated machining**" (S3). It is not inert — it moves the *picture*. | **Substantive.** See below. |
| **C2** | A 4-tab strip: `Connect / Manual / Program / Setup`, and `README` §2 claims the mockup keeps CNCBase's "control grouping" | MDI app; independently toggled floating panels off the **View** menu; no tabs; no "Connect" anything | **Divergence to declare, not a bug.** See below. |
| **C3** | Jog step `0.1 / 1 / 10 mm` | Named preset slots **A/B/C(/D)**, values configured in `Setup \| Jog Settings`; observed defaults 0.025 / 0.254 / 2.54 mm | Change the *presentation* to slot + value. 🔴 lab confirms the values. |
| **C4** | Jog and Run are **rejected** until `Homed` — "this is real CNC behavior" | No source states the block. `Homed` **is** a permanent status-bar indicator. | **Reclassify as a deliberate pedagogical gate**, not as fidelity. Keep it; stop justifying it as realism. 🔴 |
| **C5** | Feed override = 3-position `25 / 50 / 100 %` on the **Manual (jog)** tab | Feed Rate Override is a **continuous 0–200 % dial** applying to the **programmed** feed. Jog speed is separate: named presets (`Slow / Med / …`). | Two different controls. Move override to the Program screen; give jog its own speed presets. |
| **C6** | Doors surfaced on Manual so "jogging with the guard open is **visible**" | "magnetic shield interlock switch **prevents the machine from operating** with the shield open" | The door should **gate motion**, not just be observable. One boolean in `CNCBase_Machine_Link`'s gate chain. |
| **C7** | Block readout = one canned line, `N040 G01 X-50.0 F120` | `Block 17 of 22` + a scrolling window with the current block **highlighted** | Cheap upgrade, much more legible in VR. |
| **C8** | DRO = one row of X/Y/Z mm | Absolute / Relative / **Machine** / Dist to go, plus a `Coord: Work` field | Keep one row; **add the `Coord` label**. It costs a `TextMeshPro` and it is the honest answer to `README` §7.3. |
| **C9** | `Mode: OFFLINE / ONLINE / SIMULATION`, switchable in-session | `Setup \| Online` / `Setup \| Simulation`, check-marked, **switching restarts the app** | Terminology confirmed ✅. Mid-session switching is a divergence — call it out in-panel or accept it. |
| **C10** | Setup tab shows `Tool T1 (Ø6 mm end mill)` | Machine Info shows `Tool` **and** `TDiam`; ProMill max tool Ø is 10 mm | Split into two fields to match. Ø6 mm is fine but 🔴 unverified. |
| **C11** | Spindle "has no counterpart yet… may be cut" (`01` §5) | `Setup \| Spindle` + Outputs-toolbar **Spindle Output / Spindle Direction** — software-controlled, 100–5000 RPM | **Build it.** `03_Build_Plan.md` §4's "is the spindle software-started?" is now answered: yes. |
| **C12** | E-stop "reserved for M3", no behavior defined | Hardware E-stop **loses position tracking**; `Ctrl+Space` does not | Free, real, excellent M3 content. Add to the backlog. |
| **C13** | — (not mentioned) | ProMill talks over **Ethernet**, IP set by a config utility | Optional authenticity detail for a future "why won't it go Online" step. |

### C1 in full — the Simulation problem

`README.md` §5.1 argues the whole live-panel design pays for itself because "the trainee picks Simulation,
presses jog, and *the mill does not move* — the concept teaches itself."

Half of that is right. In real CNCBase, choosing Simulation means the machine does not move **and the
software animates the cut anyway**, in the Verify window. The trainee's mistake is not "nothing happened",
it is "the screen is machining and the metal isn't." That is a *sharper* lesson, and it is the accurate one.

The problem: `README.md` §3 explicitly cut graphic tool-path verification as **OUT** — "in VR the real mill
*is* the verification." That reasoning is sound for the Program screen and wrong for Simulation mode,
because in Simulation the graphic is the *only* output the software produces.

Three options, in order of preference:

1. **Minimal Verify pane.** A small always-2D panel that draws the canned `part_042.nc` toolpath as a
   polyline and animates a dot along it. No second render target, no `RenderTexture`, no camera — a
   `LineRenderer` on a UI canvas or a pre-baked sprite sequence. Simulation then produces a *visible,
   wrong-place* result, which is the real teaching point, and Online produces both. Cost: small; it is a
   drawing, not a simulator.
2. **Accept the divergence, and say so in-panel.** Keep Simulation inert, but have the status line read
   `SIMULATION — motion is simulated on screen only` so the trainee is not taught that Simulation does
   nothing. Cost: zero. Weakness: the lesson lands as a rule, not as an observation, which is exactly the
   failure mode `04` §1 warns about.
3. Leave as specified. **Not recommended** — it teaches a false model of Simulation mode, on the module's
   own headline point.

This does not change `02_Architecture.md`. `CNCBase_Machine_Link` still drops motion commands when
`!Online`; the panel additionally runs its own animation. The link stays the only thing touching the mill.

### C2 in full — the tab strip

CNCBase has no tabs. The mockup's `Connect / Manual / Program / Setup` strip is an invention, and there is
no "Connect" concept in the product at all — mode lives under **Setup**, and launching is a Start-menu action.

**Keep the tabs.** Four floating, overlapping, independently-closable Win32 panels is a terrible thing to
put on a world-space canvas two metres from someone's face, and `Panel_Tab_Group` already exists and already
integrates with the guided highlight. This is the right call for VR.

But `README.md` §2's claim that the mockup preserves CNCBase's "terminology, mode model, and **control
grouping**" should be corrected — the grouping is *ours*. Two concrete cleanups that cost nothing:

- Rename the **`Connect`** tab to **`Setup`**, matching the menu that actually holds Online / Simulation /
  Set-Check Home / Soft Limits. Every control on that tab lives under Setup in the real product.
- Rename the current **`Setup`** (read-only) tab to **`Machine Info`**, matching the panel it is imitating —
  and move `Tool`, `TDiam`, `Feed`, `Spindle`, `Coord`, `Pass`, `Block n of m` onto it, which is what
  `View | Machine Info` actually shows.

`Action_Id`s are unaffected; these are label changes in `M2_Module_Builder.BuildScene()`.

## 7. `README.md` §7 open questions — status

| # | Question | Answer |
|---|---|---|
| **7.1** | `Connect: Active` vs `Online`? | **Not `Connect: Active`** — that phrase appears nowhere in CNCBase. The concept is a check-marked mode at the top of the **Setup** menu, paired with `Simulation`. Spelling differs by document: the machine manuals write **`On-line`** (S1, S2), the marketing datasheet writes **`Online`** (S3). Rename the lesson text; keep the `Action_Id`s. 🔴 One glance at the lab's Setup menu settles the hyphen. **Closed enough to build.** |
| **7.2** | Jog step sizes? | Presets in **named slots** set via `Setup \| Jog Settings`; shipped defaults look like imperial decades (0.025 / 0.254 / 2.54 mm). Change the presentation now; 🔴 the lab's own values still need reading off the screen. **Partly closed.** |
| **7.3** | Machine-coordinate zero — at home, or at a travel extreme? | Wrong dichotomy. CNCBase carries **both** simultaneously: `Machine` = relative to home, `Relative` = relative to Work, with the active system shown as `Coord`. So machine coordinates read **0 at home**, and the interesting display question is *which system the DRO is showing*. Add the `Coord` label (**C8**). **Closed.** |
| **7.4** | `start_fms.nc` behavior? | 🔴 **Still open.** Nothing public describes it — it is a CIM/FMS cell integration file, almost certainly local to the lab's setup. The no-motion wait-loop model remains the cheapest correct guess. |
| **7.5** | Jog pendant / FANUC panel? | The **handwheel is a documented CNCBase accessory** with dedicated UI (green activation button; software jog arrows disappear when engaged) — S1. A **FANUC 21i emulator** (with 16i/18i subset) ships in the software (S3), switchable "with a mouse click". 🔴 Whether *this lab* has the physical handwheel is still a lab question, but the interaction is now a documented feature rather than a proposal. **Upgraded.** |

## 8. Revised lab checklist

Replaces `03_Build_Plan.md` §4. Struck items are answered above.

- ~~Screen terminology — `Online` vs `Connect: Active`~~ → the Setup-menu mode pair; 🔴 hyphen only (§7.1)
- ~~Machine-coordinate zero~~ → Machine coords are home-relative; both systems shown (§7.3)
- ~~Is the spindle software-started?~~ → **Yes**, `Setup | Spindle` + Outputs toolbar (§4.6)
- [ ] **🔺 Soft limits — 280 / 152 / 270 or 260 / 152 / 180?** *Escalated, not closed.* Intelitek's
      datasheet and the machine's own user guide disagree, and the Z delta is 90 mm (§5.1). Read the serial
      plate **and** open `Setup | Soft Limits` on the lab machine. **Highest-value item on this list** — it
      is the only one that can make P1's verify criteria wrong.
- [ ] **Jog step and speed preset values** actually loaded in `Setup | Jog Settings` — and whether the
      lab teaches step or continuous jog
- [ ] **`start_fms.nc`** — read the file. Still the largest unknown.
- [ ] **Homing** — does `Home` drive all three axes at once or sequentially, and in what order? Does the
      lab use `Home` or `Quick Home`?
- [ ] **Un-homed jog** — is it actually refused on this machine, or merely uncalibrated? (**C4**)
- [ ] **iCNC version** on the lab PC (`Help | About`) — confirms how far §3's layout has drifted
- [ ] **Units** — is `Setup | Units` set to Metric or Inch in the lab?
- [ ] **Coordinate system** — which one is displayed by default, and is a Work offset set?
- [ ] **Shield interlock** — confirm the machine refuses to move with the door open (**C6**)
- [ ] **Handwheel** — is one fitted? (**§7.5**)
- [ ] **Tool** — actual tool number and diameter loaded for the lab's demo part (**C10**)

## 9. Recommended edits, by cost

**Free (label changes in `M2_Module_Builder.BuildScene()`):**
`Connect` → `Setup`; `Setup` → `Machine Info`; `Connect: Active` → `Online`; add a `Coord: Work` field;
step buttons rendered as slot + value; split `Tool` / `TDiam`.

**Cheap (inside `CNCBase_Machine_Link`, no new systems):**
Door interlock added to the gate chain (**C6**); block readout becomes `n of m` + a 3-line highlighted
window (**C7**); jog speed presets separated from feed override (**C5**); build the spindle spin (**C11**).

**Blocking P1's verify criteria:**
The soft-limit conflict (**§5.1**). `03_Build_Plan.md` P1 verifies "jog X by +200 mm from centre → clamps at
`140.00`". If the lab machine is a 260/180 unit, that number is wrong and so is the Z envelope the whole
Manual tab jogs within. Settle it before writing P1's test, not after.

**Worth a decision before P2:**
The Simulation Verify pane (**C1**) — option 1 or option 2. Deciding late means rebuilding the Manual and
Program tab layouts to make room.

**Backlog, not this build:**
Handwheel jog consuming the same `CNCBase_Machine_Link` API (**§7.5**); the E-stop vs `Ctrl+Space`
position-tracking lesson for M3 (**C12**); an Ethernet/Online failure step (**C13**).

---

**Sources:**
[ProMill 8x00 User Guide](https://downloads.intelitek.com/Manuals/CNC/Machines/User_Guide_ProMill_8x00_C.pdf) ·
[BenchMill 6x00 User Guide](https://downloads.intelitek.com/Manuals/CNC/Machines/User_Guide_BenchMill%206x00_C.pdf) ·
[CNCBase & CNCMotion datasheet 35-1007-3200](https://www.intelitek.com/resources/pdf/35-1007-3200_DS_SW_CNCB-M_Ver_F.pdf) ·
[CNCBASE® product page](https://intelitek.com/cncbase/) ·
[ProMill 8000 hardware datasheet 35-1006-7600 Ver M](https://www.intelitek.com/resources/pdf/35-1006-7600_DS_HW_CNC8000_Ver_M.pdf) ·
[ProMill 8000 product page](https://intelitek.com/promill-8000-cnc-machining-center/) ·
[ProMill 8000 spec mirror — Labora Teknika](https://labts.co.id/product/promill-8000-cnc-machining-center/) ·
[ProMill 8000 spec mirror — Uruhu Highlands](https://www.uruhuhighlands.net/product-page/promill-8000-cnc-machining-center-intelitek) ·
[iCNC control-software downloads](https://downloads.intelitek.com/Software/CNC/Control_Software/) ·
[CNC Milling Technology with ProMill 8000 curriculum](https://www.intelitek.com/resources/pdf/35-1007-7700_DS_EL_CNC-Milling8000-virtual-lab_Ver_F.pdf) ·
[Intelitek machine manuals index](https://downloads.intelitek.com/Manuals/CNC/Machines/)
