# Carving — procedural material removal

Stock is a scalar field, tools subtract from it, and the visible mesh is re-contoured whenever a
slab changes. This is the material-transformation core the Digital Twin's mill and lathe will
carve with. The design research is in `docs/Stations/01_Material_Transformation_Research.md`; the
stacked-slice marching-squares approach here was chosen over that document's Z-map recommendation
so the two could be compared.

## Layout

| Folder | Assembly | Contents |
| --- | --- | --- |
| `Scripts/` | `Carving` (asmdef) | Pure core + the MonoBehaviour wrappers + `Carving_Sandbox_Input` (generated) |
| `Editor/` | `Carving.Editor` | `Carving/7 Validate Scene`, `Carving/8 Debug - …` |
| `Tests/Editor/` | `Carving.Tests` | 25 EditMode tests against the pure core |
| `Tools/` | assets | `Tool_Definition` library shared with the NC demo |
| `Input/` | asset | `Carving_Sandbox.inputactions` (keys for the sandbox) |
| `Scenes/` | scene | `Carving_Sandbox.unity` — authored; there is no builder |
| `Materials/` | assets | stock, tool and stand materials |

The asmdef exists so the core can be tested without a scene. It cannot see Assembly-CSharp, so
anything that needs `Item_Parent`, `AxisMovement` or the training scripts lives outside this
folder (the NC demo's glue is the example).

## The pure core (no scene needed)

- `Scalar_Field_2D` — padded, clamped signed-distance grid (positive = solid). `Subtract_Disc`
  is the mill hot path; `Subtract<T>(in T sdf, Rect)` takes a struct implementing `I_Sdf` so a
  stamp allocates nothing.
- `Marching_Squares` — 16-case contour + fill tables, saddles resolved by the centre average,
  canonical edge crossings so adjacent cells share vertices (watertight seams). Scratch space is
  a per-call `stackalloc`; the class holds no mutable state.
- `Mill_Stock_Data` — N horizontal slabs, one field each; walls extruded from the contour, caps
  culled where the neighbouring slab is at least as solid. Emits flat normals itself
  (`Mesh_Util.Flat_Normal`), so no `RecalculateNormals`.
- `Lathe_Stock_Data` — one `(z, r)` field revolved; the `r < 0` row mirrors row 1 so facing to
  centre yields a fan apex.
- `Tool_Profile` — diameter, length and tip (Flat / Ball / Drill 118°) with the height→radius and
  lathe-section signed distances. `Carve_Sweep` holds the half-cell sub-step rule both stocks use.

Units are metres throughout; a tool's tip is its transform position and its axis is `transform.up`.

## Scene components

| Component | Serialized fields | Notes |
| --- | --- | --- |
| `Mill_Stock` | `Size`, `Cell`, `Pitch` | `[RequireComponent(MeshFilter, MeshRenderer)]`; the material is the renderer's. One mesh, rebuilt in `LateUpdate` from per-slab buffers when any slab is dirty. `Reset_Stock()` also recovers from a script reload during Play. |
| `Lathe_Stock` | `Length`, `Radius`, `Cell`, `Segments` | Same shape as the mill; axis = local +Z. |
| `Carving_Tool` | `Definition`, `Visual`, `Mill` / `Lathe` (exactly one) | `Carve_Step()` carves the sweep since the last step, in stock-local space so a stock riding a machine table carves the relative motion. `Bind(stock)` re-seeds so a teleport is not carved. `Sync Visual` (context menu) fits the cylinder to the profile. |
| `Tool_Jog` | `Tools`, `Mill`, `Lathe`, `Feed_Mm_S`, `Readout` | Sandbox keyboard jog + stats readout (TMP, refreshed 10×/s). |
| `Carving_Demo_Path` | `Feed_M_S`, `Steps` (tool + world target) | The authored demo cuts `Carving/8` drives. |

`Tool_Definition` (`Create > Carving > Tool Definition`) wraps one `Tool_Profile` as an asset. The
seven in `Tools/` are named by tip, diameter and length (`Flat_D10_L40`); the NC demo's rack
references the same assets.

## Running the sandbox

Open `Scenes/Carving_Sandbox.unity` and press Play. Keys (bindings in `Input/Carving_Sandbox.inputactions`,
generated wrapper `Carving_Sandbox_Input`): **WASD** move the active tool in X/Z, **E/Q** up/down
(mill tools), **Tab** next tool, **R** reset both stocks, **[ ]** halve/double the feed. Click the
Game view first so it has focus.

Editor menus:

- `Carving/7 Validate Scene` — checks every reference the components dereference at Play (run
  it after any hand edit; it replaces the deleted scene builder).
- `Carving/8 Debug - Run Demo Cuts` — runs `Carving_Demo_Path` (all six tools) in Play mode;
  usable over MCP, which cannot press keys.
- `Carving/8 Debug - Reset Stocks`.

Tests: Test Runner → EditMode → `Carving.Tests` (marching squares cases, field stamping, tool
profiles, watertight and flat-normal checks on both stocks).

## Integration into the Digital Twin

The seam is `Job_Mill_Epoxy_Penholder.OnTargetFound` (`Assets/Scripts/Job System/`): that is where
the mill swaps the plate's epoxy block for the finished penholder, so a carving pass would run there,
before the swap. The shape that already works in the NC demo:

1. The workpiece is a prefab with `Mill_Stock` + `MeshFilter` + `MeshRenderer` (see
   `NC_Demo/Prefabs/NC_Stock.prefab`), instantiated on the fixture that rides the table drives.
2. The cutter is a `Carving_Tool` with a `Tool_Definition`, parented to whatever rides the
   spindle; `Bind(stock)` after the teleport, then `Carve_Step()` once per frame (or per motion
   sample) while cutting.
3. Any motion source works — `AxisMovement`, an animation, or the NC runner — because the tool
   only samples its own transform.

## Known limits

- Remesh cost scales with dirty slabs per frame: about 9 ms for the sandbox block
  (150 × 100 mm, 1 mm cells, 50 slabs, ~300 k vertices) when jogging, up to ~50 ms for the NC
  demo block at 10× feed override. The single-mesh upload is a small part of that; the marching
  squares pass dominates. Coarser `Cell`/`Pitch` are the levers.
- Not built (deliberately): colliders, save/load of the carved field, VFX/chips, flute or shank
  geometry, lathe nose radius, Burst. The field is not serialized, so a script reload during
  Play empties it — the wrappers warn once and `Reset_Stock` rebuilds.
- Only the sandbox uses the lathe half; the NC demo and the mill station use `Mill_Stock`.
