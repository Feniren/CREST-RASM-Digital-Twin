*How to use: paste everything below as your message in the desktop Claude app. Unlike
`MEETING_REPORT_PROMPT.md`, this prompt is **self-contained** — all source facts are embedded; nothing needs
to be attached.*

---

# Task

Produce **one Microsoft PowerPoint deck (`.pptx`)** introducing our CNC mill station and its VR training
program to **new engineering transfer students** at the university. Deliver an actual `.pptx` file, not
Markdown. Add clearly marked **placeholders for images/screenshots** throughout (we'll drop in real photos
and VR captures before presenting).

# Audience & framing

- **Audience:** engineering transfer students new to the university — technically literate, but assume **no
  prior CNC or robotics experience**. Define every acronym on first use (CNC, NC program, CIM, ATC).
- **Purpose:** a general introduction — after the talk, a student should be able to say what the ProMill 8000
  is, name its main parts, describe what it can do, and explain how the VR training program will teach them
  to run it.
- **Register:** welcoming and visual, not a spec dump. Short bullets, one idea per slide, concrete examples.
  Specs go in compact tables on a reference slide, not prose.
- **Length:** ~12–16 content slides, a few minutes each.
- **Speaker notes:** write 2–4 sentences of presenter notes per slide (what to say, not a repeat of the
  bullets).

# Grounding rules

- The facts below are the **only** source material. Do not invent numbers, features, or module content.
- Facts marked **[Intelitek]** come from official Intelitek documentation; facts marked **[project]** come
  from our project's docs. Keep that spec table sourced as "manufacturer specifications."
- Items marked **(to be confirmed)** must be presented as such or omitted — never stated as fact.
- The VR training program is **in development** — say "will teach / is being built," never claim modules are
  finished or validated.

# Source facts

## The machine — Intelitek ProMill 8000

**What it is [project]:** a 3-axis CNC (Computer Numerical Control) milling machine — the machining station
of the Intelitek SmartCIM 4.0 manufacturing cell in our lab. A computer drives the machine's motions to cut
material from a workpiece automatically.

**Main parts (the six components every trainee learns) [project]:**
1. **Spindle motor** — the motor on top of the head; it spins the spindle and cutting tool.
2. **Spindle head** — holds the rotating spindle and cutting tool; moves up and down in Z.
3. **Vise** — clamps the workpiece to the table during cutting.
4. **Guard door** — the perspex shield that must be closed while the spindle is cutting.
5. **Emergency stop button** — the red button on the front; press to immediately stop the machine.
6. **Electronics cabinet** — the lower cabinet housing the machine's drive and control electronics.

**Other controls & accessories [project]:** main power switch (always the first control used at start-up);
door-unlock control (releases the guard-door interlock for loading/unloading); right-side connection panel
(power, Ethernet, coolant, jog-pendant ports); optional handheld jog pendant.

**Axes & motion [project, travels cross-confirmed by Intelitek]:** three axes taught with the right-hand
rule —

| Axis | Motion | Travel |
|------|--------|--------|
| X | table left–right | 280 mm |
| Y | table forward–back | 152 mm |
| Z | spindle up–down | 270 mm |

**What it can do — the 5 milling operations [project]:**
- **Face** — flatten the top surface
- **Pocket** — hollow out a recess
- **Contour** — cut an outside profile
- **Drill** — plunge holes
- **Slot** — cut channels

**Manufacturer specifications [Intelitek — official product page]:**

| Spec | Value |
|------|-------|
| Spindle speed | 100–5000 RPM |
| Spindle motor | brushless, 1000 W (1.34 hp) |
| Spindle taper | ISO20 |
| Work table | 550 × 160 mm, 3 T-slots (12 mm), max load 35 kg |
| Feed rate | rapid 5000 mm/min; cutting 2540 mm/min |
| Position accuracy / repeatability | 0.038 mm / 0.010 mm |
| Max tool diameter | 10 mm |
| Programming | standard G & M code (EIA RS274-D), Fanuc-compatible, CAD/CAM-compatible |

**Tools [Intelitek]:** cutting tools mount in ISO20 tool holders; a 4-tool holder package is standard, and an
automatic tool changer (ATC — 12-tool carousel or 4-station table mount) is an available option. Our lab
machine's exact tooling configuration is **(to be confirmed)** — present tooling generically.

**Safety features [project]:** emergency stop button; guard-door interlock (door must be closed while
cutting); soft travel limits enforced in the control software; machine control software can also run in
Simulation mode where nothing physically moves.

**Its place in the cell [project]:** the SmartCIM 4.0 cell has two stations, each with its own PC — an **arm
station** (Yaskawa Motoman GP8 robot arm + controller, run by SCORBASE software) and the **mill station**
(ProMill 8000, run by CNCBase software). In production, a conveyor delivers raw stock on pallets, the robot
arm loads the workpiece into the mill's vise, the mill cuts it, and the arm returns the finished part to the
conveyor.

## The VR training program [project]

**What it is:** a VR training program (Meta Quest 3 headset, PC VR) being built in Unity that teaches
students to understand and operate the ProMill 8000 before they ever touch the real machine. Trainees stand
in a virtual copy of the lab cell and interact with a full-scale digital twin of the mill.

**How every module works — two phases:**
1. **Guided phase** — the trainee is walked through the machine step by step: the current target part glows,
   a floating prompt explains it, and the trainee must perform the correct interaction to advance. Wrong
   actions don't advance the lesson.
2. **Practice phase** — the same tasks with **no help**: highlights and prompts are off, the trainee's
   actions are scored, and a pass-gate must be met to complete the module.

A wrist-mounted display shows progress and a timer throughout.

**Module 1 — CNC Milling: What & Why** (~8 minutes)
- Learn the machine's role in the SmartCIM 4.0 cell.
- Tour the six major components — each highlights and is explained in text and speech.
- Watch the axis-motion demo (X/Y/Z travels with the right-hand rule) and a milling demo (a cutting sequence
  on a see-through perspex block).
- **Assessment:** 6-part component-identification quiz with no highlights; pass at ≥ 4/6.

**Module 2 — System Startup & Program Execution**
- Perform the full cold-start of the two-station cell, in three phases:
  1. **Power** — arm-station PC → robot controller → mill-station PC → ProMill 8000 main power.
  2. **Launch** — start SCORBASE (arm) and CNCBase (mill); screens stay dark until their PC is powered.
  3. **Activate & verify** — bring the arm active in SCORBASE (Control On → home all axes → standalone mode
     → test move), then the mill in CNCBase (Connect Active → machine home → open and run the `start_fms.nc`
     program → confirm it's running).
- Software steps use simplified in-VR panels with real SCORBASE/CNCBase terminology — trainees **run** NC
  programs; they never write G-code.
- **Assessment:** the full startup sequence unaided; out-of-order actions count as errors; pass = everything
  active and verified.

Further modules (safety, and more) are planned; M1 and M2 are the current focus.

# Suggested slide outline (adapt as needed)

1. **Title** — "The ProMill 8000 CNC Mill & VR Training" + university/program name (placeholder).
2. **What is CNC milling?** — one-slide plain-language intro.
3. **Our lab: the SmartCIM 4.0 cell** — the two stations and the material flow (conveyor → arm → mill).
4. **Meet the ProMill 8000** — hero image + what-it-is summary.
5. **Main parts** — the six components on a labeled photo/diagram.
6. **Controls you'll use** — power switch, guard door + interlock, E-stop.
7. **How it moves** — X/Y/Z axes, right-hand rule, travel table.
8. **What it can make** — the 5 milling operations.
9. **Tools** — ISO20 tool holders, tool changing (keep generic; lab config TBC).
10. **By the numbers** — the manufacturer spec table (reference slide).
11. **Learning it in VR** — the training program: headset, digital twin, guided → practice format.
12. **Module 1 — CNC Milling: What & Why** — tour, demos, quiz.
13. **Module 2 — System Startup** — the three-phase cold start, software panels, unaided practice.
14. **What you'll be able to do** — recap of skills after M1+M2; more modules coming.
15. **Questions** — contact/course info (placeholder).

# Image placeholders (required)

Wherever a photo, diagram, or VR screenshot belongs, insert a clearly visible placeholder shape (a shaded
rectangle sized like the intended image) containing, on separate lines:
- **`Figure N. <short caption>`**
- *`‹IMAGE PLACEHOLDER — <what to capture/show>›`*

Number figures sequentially and keep the literal token **`‹IMAGE PLACEHOLDER›`** in every one so all
placeholders are findable with a single search.

Suggested captures: the ProMill 8000 in the lab (hero); the labeled six components; the guard door and
E-stop close-ups; the axis directions overlaid on the machine; the SmartCIM cell layout; a VR screenshot of
a glowing highlighted part with its prompt; the wrist display; the simplified SCORBASE and CNCBase panels;
the milling demo on the perspex block.

# Formatting

- Clean, consistent template; large readable text (students at the back of a lecture room).
- One idea per slide; prefer a visual + a few bullets over paragraphs.
- Real PowerPoint tables for the travel and spec tables.
- Consistent slide titles; slide numbers on.

# Before finishing

- Confirm every claim traces to the **Source facts** section (nothing invented, nothing overstated).
- Confirm all placeholders use the `‹IMAGE PLACEHOLDER›` token and are numbered sequentially.
- Confirm speaker notes exist on every content slide.
- Output the final `.pptx`.
