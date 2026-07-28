# Migration: generated module scenes → hand-authored scenes

**Decision (2026-07-27): stop generating module scenes. Commit them as authored artifacts.**
The runtime framework stays exactly as it is. Supersedes the scene-building half of
`05_Module_Framework_HOWTO.md`; that doc was rewritten around the authored-scene
model on 2026-07-28.

## Why

The builders were adopted for merge safety, reproducibility from source control, and
re-derivation after a model swap. Re-examined:

- **Merge safety does not require generation.** `00_Program_Overview.md:139` names the
  driver as *separate scenes per module* — "one scene per module keeps M1/M2/M3 as
  independent files." That is satisfied by splitting the scenes and giving each one
  owner, both already true. Nothing in the rationale requires the scenes to be built
  from code. Smart Merge is now configured in `.gitattributes` for the residual risk.
- **The authoring loop already bypasses the builder.** The working practice is to
  place things by hand in the scene and then transcribe the transforms back into the
  builder. That pays for the editor's ergonomics *and* a manual transcription step,
  and it is the sole reason the builders accumulated baked literals: the vise and
  block poses, three of the eight marker hitboxes, and every prop transform in M2
  exist only because a rebuild would otherwise wipe the hand edits.
- **Re-derivation earns less than it looks.** `Training/5` re-derives 5 of 8 markers
  (the other 3 are baked), and until 2026-07-27 it corrupted the toggle wiring every
  time it ran. Re-placing eight boxes by hand after the CAD redo is roughly half an
  hour, once.
- **Some of it is actively worse than the editor.** M2 lays out two world-space
  panels and ~20 buttons as literal `anchoredPosition` coordinates — longhand for
  what the UI editor does directly.

The runtime is already a scenes-and-controllers architecture: `Lesson_Controller` →
`Module_Loader` → `Lesson_Sequencer` → `Marker_Registry` / `Action_Button_Registry`,
all driven by serialized references and string ids. The builders only fill in
Inspector fields a human can fill in. **No runtime change is needed for this.**

## What we give up, and what replaces it

Generation guaranteed a step id and a marker id could not disagree — both came from
one constant. Hand-authored scenes can disagree, and `Lesson_Sequencer` matches ids by
string at runtime, so a mismatch is a step that can never be completed, silently.

Replaced by **validation instead of generation**: `Training/7 Validate Open Module
Scene` (`Assets/Scripts/Training/Editor/Training_Validator.cs`). It cross-checks every
step's target against the markers and action buttons actually reachable from the
registries' roots, plus duplicate ids, markers parented outside `MarkersRoot`, missing
demo controllers, and empty `Lesson_Sequencer` slots.

This is strictly better than what the builders did: it also catches drift introduced
by hand edits, which the builders never reported — they just overwrote it.

## Do not regenerate either scene

Found while auditing before the M2 deletion: **both working-tree scenes contain an
`IntellitekMill` prefab instance that no builder creates and that is not in the last
commit.** It is a hand edit, in both `Module1_Overview.unity` and
`Module2_Startup.unity`, and running `Training/3` would silently destroy it.

That inverts the original plan. Regeneration is not the safe step before deletion —
it is the destructive one. The scenes as they stand are ahead of the builders, which
is the whole argument for this migration, arriving on its own.

So: **commit the scenes as they are. Never run `Training/3` again on a scene you care
about.** Anything the builders would have contributed goes in by hand from here.

The one outstanding item is the axis-travel fix, and it turns out to be a single
field, not four — the scene already holds `XTravel: 0.14`, `ZTravel: 0.27`,
`DoorSeconds: 1`, which match the constants. Only `YTravel` is wrong:

> Select `Lesson_Manager` in `Module1_Overview.unity` → `Mill_Demo_Controller` →
> set **`YTravel` 0.04 → 0.076**. Then watch the Y sweep: it roughly doubles, so
> check the vise and demo block clear the guard doors.

## Sequence

1. ~~Regenerate the scenes~~ — superseded, see above.
2. **Commit both scenes** plus `PM8000_Training.prefab`, `Demo_Block.prefab`,
   `M1_Lesson.asset`, `M2_Lesson.asset` and `Training_Modules.asset` as authored
   artifacts. Until this is done a stray `git checkout` loses the hand edits.
3. Apply the `YTravel` edit above.
4. Run `Training/7` on both module scenes — establishes the clean baseline.
5. **M2 — done (2026-07-27).** `BuildModule2Scene`, `BuildPowerCanvas`, the
   `MonitorGuid`/`ComputerGuid` consts and `Training/3B` deleted;
   `M2_Module_Builder.cs` is 278 → 69 lines and now only writes the lesson asset and
   calls `RegisterModule` (the registry still drives the Bootstrap menu, the
   build-settings list and the play redirect). Edit the scene by hand for a week.
6. If that holds, do M1: delete `M1_Mill_Rig`, `M1_Markers`, `M1_Parts_Table`,
   `BuildModule1Scene`, `WireLessonManager`, `Training/3`, `Training/5`. Note
   `Training/3` also writes `PM8000_Training.prefab`, which **both** scenes now hold
   an instance of — the prefab becomes a normal authored asset at that point.
7. Strip `Training_Builder_Core` to what still runs (see below), and rewrite
   `05_Module_Framework_HOWTO.md` and `Module_Builder_Template.cs.txt` around
   "author a scene, register it, validate it." *(HOWTO rewritten 2026-07-28;
   the core strip and the template rewrite remain.)*

## What stays and what goes

| Goes (~800 lines) | Stays (~250 lines) |
|---|---|
| `M1_Mill_Rig`, `M1_Markers`, `M1_Parts_Table` | `M1_Lesson_Content` — 2 KB of prose diffs far better in C# than as `[TextArea]` fields in YAML |
| `BuildModule1Scene`, `WireLessonManager`, `BuildModule2Scene`, `BuildPowerCanvas` | `Training/1` framework assets, `Training/2` Bootstrap, `Training/4` build settings — module-agnostic, no visual tuning |
| `Training_Builder_Core`: UI factories (`CreateWorldCanvas`, `CreateTMP`, `CreateButton`, `AddBackground`, `Stretch`, `CreateIndicator`, `CreateFloor`), `BuildComponentMarker`, `BuildPartAction`, `BuildActionButton`, `BuildTabContent`, `WireStateToggle`, `BuildLessonScaffold`, `NewModuleScene` | `LoadOrCreateRegistry`, `RegisterModule`, `LoadOrCreateLesson`, the step factories (`Info` / `Select` / `PanelAction`) |
| The `SetRef` / `SetVal` / `SetRefArray` / `AppendRefArray` string layer — the seam where a runtime field rename fails silently | `Training_Validator`, `Training_Debug`, `M1_Debug_Menu`, `Training_Play_Redirect` |
| `Training/3`, `Training/3B`, `Training/5` | `Colin_Training_Paths` — shrinks to the lesson and scene paths |

Lesson content keeps being generated. It is content, not scene structure, and the
prose is the one place code genuinely beats the Inspector for reviewability.

## Verification

Since nothing is regenerated, there is no artifact diff to check. Verify by running:

- `Training/7` on both module scenes — expect no errors. On `Module2_Startup` the
  three `distractor_*` actions are exempt from the unused-id warning by name.
- `Training/0 Build Everything` — now writes framework assets, both lesson assets, the
  Bootstrap scene and the build settings, and rebuilds only the M1 scene. Console must
  be clean.
- Play-mode: `Training/8 Debug - Start Module 2` then `Auto Run To Completion` reaches
  `completed=true` with 16 steps — this is the real check that the hand-authored M2
  scene still satisfies every step in the lesson.
- Module 1: `Training/8 Debug - Start Module 1` → `Auto Run To Completion`; milling
  demo opens doors → plunge → square → retract → closes; then `Training/5` followed by
  all four marker clicks (this used to throw a NullReferenceException or silently kill
  Power On and Emergency Stop).

Once M1's builder is deleted in step 6, `Training/7` plus the two auto-run drivers are
the entire regression suite.

## Open item

`Startup_State_Controller` drives M2's visual state from a 14-case switch on
`Action_Id`. That is the real "controller manages module state" monolith, and it is
unaffected by this migration. Worth converting to data (an ordered list of
action → state changes) once the scene work settles.
