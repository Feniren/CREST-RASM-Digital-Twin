using UnityEngine;

// Draws a dotted trail from the trainee's body to the component they are being
// shown, for the duration of a guided identification step. The trainee is free to
// walk around the cell, and once they do, a marker glow or a part gliding into
// place tells them nothing if it is behind the machine or off-screen — the trail
// is what says "the part is over there".
//
// Unlike Part_Highlighter this deliberately does NOT ask the Step_Demonstrator to
// stay out of the way: the glide shows what a part looks like, the trail shows
// where it lives, so both are wanted on the same step.
//
// Guided only. In the quiz the trainee is scored on finding the part themselves,
// and a line pointing straight at the answer would end the test.
public class Breadcrumb_Trail : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Marker_Registry Registry;
    [SerializeField] private Material Dots;

    [Header("Trail")]
    // Dots read as round when Width matches the material's _DotSpacing * _DotLength.
    [SerializeField] private float Width = 0.025f;

    // Roughly sternum height and a hand's width in front of it, which puts the origin
    // below the trainee's field of view: the trail leaves from under the chin rather
    // than out of the centre of vision.
    [Header("Body anchor (offset from the head)")]
    [SerializeField] private float Drop = 0.45f;
    [SerializeField] private float Forward = 0.3f;

    private Component_Marker currentTarget;
    private LineRenderer line;
    private Camera cam;

    private void Awake(){
        // Built here rather than authored into the scene: scene merges on this
        // project have a history of silently dropping prefab-instance overrides,
        // and a renderer that describes itself in code has nothing to lose.
        //
        // Built before subscribing, because the step handler rebuilds the line the
        // moment it fires. Parented to the Lesson_Manager so the trail belongs to the
        // module scene and unloads with it — an unparented runtime object lands in the
        // active scene, which is Bootstrap, and would outlive the module.
        GameObject host = new GameObject("Breadcrumb_Trail_Line");
        host.transform.SetParent(transform, false);

        line = host.AddComponent<LineRenderer>();
        // sharedMaterial, not material: assigning to .material clones the asset per
        // renderer, and nothing here sets material properties from code. Sharing also
        // means tuning the dot spacing on the asset during Play mode is visible
        // immediately and survives exiting Play.
        line.sharedMaterial = Dots;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        // Tile makes the U axis the distance along the line in world metres instead
        // of 0..1, so the dots hold their spacing whether the trainee is next to the
        // mill or across the cell. textureScale stays (1,1) — it is a second UV
        // multiplier that would silently fight the material's _DotSpacing.
        line.textureMode = LineTextureMode.Tile;
        line.widthMultiplier = Width;
        // Two points: the trail runs straight from the body to the marker, so there is
        // nothing between the ends to describe.
        line.positionCount = 2;
        line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        Sequencer.Step_Changed += OnStepChanged;
        Sequencer.Lesson_Completed += OnLessonCompleted;
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        currentTarget = Sequencer.Mode == Lesson_Mode.Guided && step.Kind == Lesson_Step_Kind.Select_Component
            ? Registry.Resolve(step.Target_Marker_Id)
            : null;

        // Rebuilt here as well as in LateUpdate so the trail is never shown for a
        // frame holding the previous step's shape.
        Rebuild();
    }

    private void OnLessonCompleted(Lesson_Result result){
        currentTarget = null;
        Rebuild();
    }

    // LateUpdate so the head pose for this frame is already applied — a trail built
    // in Update trails the camera by a frame, which is very visible in a headset.
    private void LateUpdate(){
        Rebuild();
    }

    // Enabling the line only at the very end matters: until the positions are written
    // they are all zero, and a line enabled ahead of them draws a degenerate streak
    // through the world origin.
    private void Rebuild(){
        if (currentTarget == null){
            line.enabled = false;
            return;
        }

        if (cam == null){
            // Module scenes are content-only; the rig arrives with Bootstrap, and
            // the auto-run debug driver steps a whole lesson before one ever exists.
            cam = Camera.main;

            if (cam == null){
                line.enabled = false;
                return;
            }
        }

        // Straight to the marker. The trail is a sightline, not a walking route — it is
        // drawn with ZTest Always precisely so it can cut through the parts table and
        // the mill casing rather than having to go around them.
        line.SetPosition(0, Body_Anchor());
        line.SetPosition(1, currentTarget.transform.position);
        line.enabled = true;
    }

    // Anchored to the body, not the head: a trail welded to head rotation swings a
    // full arc every time the trainee glances down at the parts table, which is
    // unpleasant in VR and useless as a direction cue because the line then moves
    // faster than the world. Taking the yaw only caps that swing at Forward.
    private Vector3 Body_Anchor(){
        Vector3 flat = cam.transform.forward;
        flat.y = 0f;

        Vector3 anchor = cam.transform.position - Vector3.up * Drop;

        // Looking straight up or straight down leaves no yaw to read. The anchor sits
        // directly under the head for that frame rather than snapping to an arbitrary
        // compass direction.
        if (flat.sqrMagnitude > 0.0001f)
            anchor += flat.normalized * Forward;

        return anchor;
    }
}
