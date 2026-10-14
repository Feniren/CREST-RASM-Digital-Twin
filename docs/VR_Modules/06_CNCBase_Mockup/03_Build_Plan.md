# 03 — Build Plan

Six phases. Each ends in a state that can be demoed and rolled back, and each has a check that fails
loudly. Every phase ends with **Training/3B Build Module2 Scene** — the scene is generated, so a phase is
not done until the builder reproduces it.

---

## P0 — Reference capture *(does not block P1)*

Terminology and layout only; the architecture does not depend on it.

- Pull the CNCBase screens from the [ProMill 8000 User Manual](https://www.manualslib.com/manual/1335769/Intelitek-Promill-8000.html)
  and, if the lab PC is reachable, take photos of the real screen.
- Drop them in `docs/VR_Modules/06_CNCBase_Mockup/references/` with a one-line note per image.
- Settle **Online vs Connect: Active** (`README.md` §7.1) and the jog step sizes.

**Verify:** `01_Screen_Spec.md` §3 and §4 label text updated, or explicitly marked "unconfirmed".

## P1 — `CNCBase_Machine_Link` + live DRO

The load-bearing phase. No new UI beyond the header.

1. Write `CNCBase_Machine_Link` (`02_Architecture.md` §5) with the three axes wired off
   `MillingAnimation`'s serialized refs in the builder.
2. Replace the header of the existing CNCBase canvas: mode line, 3-axis DRO, status line. Panel polls
   `Position_mm` at ~10 Hz.
3. Add a `Training/8 Debug` menu item that calls `Jog` and `Home` directly, so P1 is testable before any
   button exists.

**Verify:**
- `Home` → DRO flips from `---.--` to `0.00 / 0.00 / 0.00` and the axes visibly return to origin.
- Debug-jog X by +50 mm → table moves **smoothly** (not a teleport — confirms `MoveToOffset`, not
  `MoveBy`), DRO reads `50.00`.
- Jog X by +200 mm from centre → clamps at `140.00`, `Status: soft limit — X`.
- Jog Y → **only** the Y figure changes, even though the X stage rides along in world space
  (`02_Architecture.md` §3).

> This phase is where the machine↔world mapping is proven. If X and Z look swapped here, stop and fix the
> link's serialized refs — never the UI.

## P2 — Manual (jog) tab

1. New tab: X/Y/Z ± buttons, step selector (0.1 / 1 / 10 mm), spindle On/Off, doors Open/Close.
2. Jog buttons carry **no `Action_Id`** (plain `CreateButton` + runtime listener) so the action registry
   ignores them.
3. Canvas grows to 640×560 @ ~0.00075 (`02_Architecture.md` §8); bake the tuned transform back.

**Verify:**
- Step = 10 mm, X+ pressed ×3 → DRO reads `30.00`, table visibly at the new position.
- Un-homed jog → no motion, `Status: home the machine first`.
- Doors toggle from the panel and from `Door_Click_Toggle` without fighting.
- Guided M2 still completes: **Training/8 Auto Run To Completion** passes unchanged.

## P3 — Connect tab: Online / Simulation / Home

1. Rewire `cncbase_launch`, `cncbase_online`, `distractor_simulation`, `cncbase_home` to the link.
2. `Startup_State_Controller` mill cases become forwards (`02_Architecture.md` §6).
3. Panel gated on **both** `mill_pc_on` and `mill_power_on`.

**Verify:**
- Simulation selected → jog buttons respond, **nothing moves**, status explains only after the attempt.
- Online selected → the same jog moves the machine.
- Panel stays dark until PC **and** mill power are on.
- No string in `Startup_State_Controller` writes a CNCBase readout any more (grep `MillStatus` /
  `MillReadout` — both refs should be gone).

## P4 — Program tab

1. Program list, Stop, block readout, run-time clock (`01_Screen_Spec.md` §6).
2. `start_fms.nc` → wait-loop, no motion. `part_042.nc` → `MillingAnimation.Play()`.
   `calib_probe.nc` → alarm.
3. Run gated behind `Homed`.

**Verify:**
- `start_fms.nc` → `RUNNING`, clock ticks, machine still.
- `part_042.nc` → the pocket cycle runs, block readout steps, `Stop` halts it **mid-cycle** and the axes
  stay where they stopped.
- `verify_mill` still pops `Demo_Block` onto the vise.

## P5 — Reset, practice pass, and the M2 end-to-end

1. `Reset_Cold()` → `Panel.Reset()` → `Link.Reset()`.
2. Distractor scoring: Simulation and the wrong `.nc` log concept errors (`04` §M2) rather than merely
   failing to advance.
3. Full guided + practice run in the headset.

**Verify:**
- Re-entering M2 from the menu returns axes to origin, doors closed, block hidden, panel dark.
- Practice run from cold: out-of-order actions log errors and take no effect; end state still reachable.
- Score persists across an editor restart (`Save_Data_Interface` path, unchanged).
- Desktop click path still works (`Desktop_Click_Select`) — the jog buttons are world-space UI, so they
  need the `TrackedDeviceGraphicRaycaster` the canvas already has.

---

## 4. Lab verification checklist

Additions to the `03_Module2_Startup_Plan.md` checklist, specific to CNCBase:

- [ ] **Screen terminology** — `Online` vs `Connect: Active`; what the mode indicator actually reads.
- [ ] **Machine-coordinate zero** — does the lab's DRO read 0 at home, or is home at a travel extreme with
      negative machine coordinates? (Affects the DRO only, not the architecture.)
- [ ] **Jog step sizes and units** offered by the lab's CNCBase build; whether jog is step, continuous, or
      both.
- [ ] **`start_fms.nc`** — still the biggest unknown. Modeled as a no-motion wait-loop.
- [ ] **Homing** — does CNCBase home all three axes at once or sequentially, and in what order?
- [ ] **Un-homed jog** — is it actually blocked on this machine, or merely uncalibrated?
- [ ] **Spindle** — is it software-started from CNCBase, or a physical control on the machine?
- [ ] **Jog pendant / FANUC panel** — the ProMill 8000 has ports for both. If the lab uses one, a physical
      jog control is the better mockup and would consume the same `CNCBase_Machine_Link` API.
- [ ] **Soft limits** — confirm 280 / 152 / 270 mm matches the lab machine's configured envelope.

## 5. Promotion path to `Assets/Scripts/Training/`

Per decision **D3**, this ships in `Assets/Members/Colin/Training/`. Promote when — and only when — a
second module needs it.

**Trigger:** a non-Colin module (M4 robot tending, M6 full cycle) needs mill control from its own scene.

**What moves:** `CNCBase_Machine_Link` only. It has no UI dependencies and is the reusable half.
`CNCBase_Panel` stays module content — a different module will want a different screen.

**Checklist:**
1. `git mv` the script (preserves the `.meta` GUID — scene refs survive; this is how the framework split
   moved the prefabs).
2. Strip anything M2-specific (the program-name switch in `Run` becomes a serialized program table).
3. Add a builder factory in `Training_Builder_Core` mirroring `BuildActionButton`, so both modules wire it
   the same way.
4. Rebuild both modules and re-run **Training/0 Build Everything**.
5. Update `05_Module_Framework_HOWTO.md`'s "Where things live" table.

**Do not promote early.** One caller does not justify a shared file, and the HOWTO's "don't edit shared
files for module content" rule exists because this project has lost inspector wiring to merges before.

## 6. Sequencing note

P1 is the only phase with real technical risk (the axis mapping and the `MoveBy`/`MoveToOffset`
distinction). It is also the smallest. Build it first, prove the DRO tracks a debug-driven jog, and
everything after it is UI work against a known-good surface.

If the schedule tightens, **P1 + P3 alone** already deliver the module's headline improvement — Simulation
that behaves like simulation and homing that gates the readout — without the Manual tab existing at all.
