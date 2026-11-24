using System.Collections.Generic;
using UnityEngine;

// Makes a drag chain (cable carrier) bend like the real thing as the arm moves.
//
// How a real drag chain works: one end bracket is screwed to the moving
// carriage, the other to the machine frame. Between them the chain lies in two
// straight rows joined by a 180° bend. When the carriage moves, links roll off
// one row, round the bend and onto the other. The bend travels half as far as
// the carriage, links never stretch, and the number of links never changes.
//
// On the ASRS's horizontal chain the top row's end (endigus001) is screwed to
// the column (ArmZ) and the bottom row's end (endigus) to the centre of the
// ASRS frame.
//
// This is done with geometry, not physics. Every frame, after
// ASRSArmController has moved the arm, it reads where the moving end is,
// solves where every joint has to be, and places each link between its two
// joints. Rigidbody + HingeJoint chains were ruled out: the arm moves by
// setting transforms, which physics joints only follow late and with jitter,
// and two dozen jointed rigidbodies per chain is wasted cost in VR.
//
// It uses the links exactly as they came from the CAD model. Their pivots are
// at the model origin with the shape baked in place, so instead of re-pivoting
// the meshes it records each link's starting pose once (Awake, with the arm at
// home) and afterwards moves each link as a rigid piece from that pose.
//
// Measured from the model at startup: link pitch, bend radius, the plane the
// chain bends in, which side the bend is on, and each link's starting pose.
// Set by hand: where the fixed end is mounted.
//
// The links live in the prefab, already laid out with the arm at home and
// long enough for the arm's full Z travel (ASRSLimits.MinZ to MaxZ).
[DisallowMultipleComponent]
public class DragChainController : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Mounting")]
    [Tooltip("What the moving end bracket is screwed to — ArmZ for the horizontal chain. Left empty, this object's parent is used.")]
    [SerializeField] private Transform carriage;
    [Tooltip("Something that doesn't move with the carriage; the fixed end is held still relative to it. Left empty, the carriage's parent (the ASRS root) is used.")]
    [SerializeField] private Transform fixedFrame;
    [Tooltip("The local axis (of the carriage's parent) the carriage slides along. ASRSArmController moves ArmZ along local Z.")]
    [SerializeField] private Axis travelAxis = Axis.Z;
    [Tooltip("Where the fixed end is mounted, in metres along the travel axis, measured from where the moving end is when the arm is at home. 0 = straight under the arm's home position, which is the centre of the ASRS on this model (the rail's centre is 5 mm the other way: -0.005).")]
    [SerializeField] private float fixedEndOffsetFromHome = -0.05f;

    [Header("Chain parts, from the fixed end to the moving end")]
    [Tooltip("End bracket on the bottom row (endigus).")]
    [SerializeField] private Transform fixedEndBracket;
    [Tooltip("Links in order, starting next to the fixed end bracket. Right-click the component > Fill Parts From Children to fill these.")]
    [SerializeField] private List<Transform> links = new List<Transform>();
    [Tooltip("End bracket on the top row (endigus001).")]
    [SerializeField] private Transform movingEndBracket;
    [Tooltip("0 = measured from the model (half the gap between the two rows).")]
    [SerializeField, Min(0f)] private float bendRadiusOverride = 0f;

    [Header("Checks")]
    [Tooltip("Optional — used to warn when the arm can travel further than the chain reaches. Found automatically in the parents.")]
    [SerializeField] private ASRSArmController armController;
    [Tooltip("How far (m) the chain's last joint may miss the moving end before the pose counts as invalid.")]
    [SerializeField, Min(0f)] private float endpointTolerance = 0.001f;
    [SerializeField] private bool drawGizmos = true;

    [Header("Live readout (Play mode only)")]
    [SerializeField] private bool chainValid;
    [SerializeField] private float carriageOffset;
    [SerializeField] private float bendTravel;
    [SerializeField] private float bottomRowLength;
    [SerializeField] private float topRowLength;
    [SerializeField] private int linkCount;
    [SerializeField] private float linkPitch;
    [SerializeField] private float chainLength;
    [SerializeField] private float bendRadius;
    [SerializeField] private float jointAngleLimit;
    [SerializeField] private float largestJointAngle;
    [SerializeField] private float endpointError;
    [SerializeField] private float linkSpacingError;
    [Tooltip("Carriage offsets from home (m) the chain can reach.")]
    [SerializeField] private Vector2 reachableOffsets;
    [Tooltip("The arm's current travel limits from home (m), from ASRSArmController.")]
    [SerializeField] private Vector2 armTravelLimits;

    // A link or bracket's starting pose, recorded once with the arm at home.
    private struct Part
    {
        public Transform transform;
        public Vector3 restPosition;   // pivot, fixed-frame space
        public Quaternion restRotation; // fixed-frame space
        public Vector3 restCentre;     // middle of the link in chain coordinates (x, h, w)
        public float restAngle;        // direction in the bend plane, radians
    }

    private Part[] parts;
    private Part fixedPart;
    private Part movingPart;

    // Chain coordinates, all in fixed-frame space. x runs along the rail and
    // points away from the bend (the bend is always on the low-x side), h runs
    // from the bottom row to the top row, w is across the chain. The chain only
    // ever bends about w, so links can't twist or swing sideways.
    private Vector3 xAxis, hAxis, wAxis;
    private float xSign; // carriage offset = xSign * (x - home x)

    private float pitch, radius, theta, arcLength;
    private float hLow, hUp, hMid;
    private float xFixed, xMovingHome, bendAtHome;
    private Vector2 fixedJointRest, movingJointRest;
    private Vector3 movingJointOnCarriage;
    private float reachLowX, reachHighX;

    private Vector2[] joints;
    private Vector3[] goodPositions;
    private Quaternion[] goodRotations;
    private Vector3 goodFixedPosition;
    private bool hasGoodPose;
    private float solvedBend, solvedMovingX;
    private Vector3 lastMovingJoint = new Vector3(float.NaN, 0f, 0f);
    private bool ready;
    private bool warnedOutOfReach;
    private float lastArmMin = float.NaN, lastArmMax = float.NaN;

    // ───────────────────────── Setup ─────────────────────────

    private void Reset() => FillPartsFromChildren();

    // Children with a mesh, sorted by name: names starting with "end" are the
    // brackets (first = fixed, last = moving), the rest are links in order.
    // Matches the horizontal chain's naming: endigus, igus, igus001 ... igus020, endigus001.
    [ContextMenu("Fill Parts From Children")]
    private void FillPartsFromChildren()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Fill Drag Chain Parts");
#endif
        if (carriage == null)
            carriage = transform.parent;
        if (armController == null)
            armController = GetComponentInParent<ASRSArmController>();

        var meshes = new List<Transform>();
        foreach (Transform child in transform)
            if (child.GetComponent<MeshFilter>() != null)
                meshes.Add(child);
        meshes.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        List<Transform> ends = meshes.FindAll(t => t.name.StartsWith("end"));
        if (ends.Count >= 2)
        {
            fixedEndBracket = ends[0];
            movingEndBracket = ends[ends.Count - 1];
        }
        links = meshes.FindAll(t => !t.name.StartsWith("end"));
    }

    private void Awake()
    {
        if (!Initialise())
        {
            enabled = false;
            return;
        }

        Debug.Log($"[DragChain] {name}: {parts.Length} links, pitch {pitch * 1000f:F1} mm, bend radius {radius * 1000f:F1} mm, " +
                  $"joint limit {jointAngleLimit:F1}°. Reaches carriage offsets {reachableOffsets.x:F3} to {reachableOffsets.y:F3} m from home.", this);
    }

    // Measures the chain from the links' current pose, which must be the
    // arm-at-home pose (true at startup, and in the prefab).
    private bool Initialise()
    {
        if (carriage == null)
            carriage = transform.parent;
        if (fixedFrame == null && carriage != null)
            fixedFrame = carriage.parent;
        if (armController == null)
            armController = GetComponentInParent<ASRSArmController>();

        ready = Calibrate();
        if (ready)
            PrepareForLinkCount();
        return ready;
    }

    // Sizes the working arrays and works out the reach for the current
    // number of links.
    private void PrepareForLinkCount()
    {
        joints = new Vector2[parts.Length + 1];
        goodPositions = new Vector3[parts.Length];
        goodRotations = new Quaternion[parts.Length];

        linkCount = parts.Length;
        linkPitch = pitch;
        chainLength = parts.Length * pitch;
        bendRadius = radius;
        jointAngleLimit = theta * Mathf.Rad2Deg;

        ComputeReach();
        Solve(xMovingHome, out bendAtHome);
    }

    private bool Calibrate()
    {
        if (carriage == null || carriage.parent == null || fixedFrame == null)
            return Fail("needs a Carriage with a parent, and a Fixed Frame.");
        if (fixedEndBracket == null || movingEndBracket == null)
            return Fail("needs both end brackets assigned (right-click the component > Fill Parts From Children).");
        if (links.Count < 2 || links.Contains(null))
            return Fail("needs at least 2 links and no empty entries in Links.");

        int n = links.Count;
        Vector3 travel = fixedFrame.InverseTransformDirection(carriage.parent.TransformDirection(AxisVector(travelAxis))).normalized;

        var centres = new Vector3[n];
        for (int i = 0; i < n; i++)
            centres[i] = CentreInFrame(links[i]);
        Vector3 fixedCentre = CentreInFrame(fixedEndBracket);
        Vector3 lastCentre = centres[n - 1];

        // Bottom row -> top row direction: the gap between the fixed bracket
        // and the last link, with the along-the-rail part removed.
        Vector3 across = lastCentre - fixedCentre;
        across -= Vector3.Dot(across, travel) * travel;
        if (across.magnitude < 1e-4f)
            return Fail("the fixed end bracket and the last link are on the same row — check the part order.");
        hAxis = across.normalized;

        float hFixed = Vector3.Dot(fixedCentre, hAxis);
        float hTop = Vector3.Dot(lastCentre, hAxis);

        // Which side the bend is on: the link halfway up the bend sits beyond
        // both ends of the chain.
        int middle = 0;
        float closest = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            float d = Mathf.Abs(Vector3.Dot(centres[i], hAxis) - 0.5f * (hFixed + hTop));
            if (d < closest) { closest = d; middle = i; }
        }
        float sMiddle = Vector3.Dot(centres[middle], travel);
        float sFixed = Vector3.Dot(fixedCentre, travel);
        float sLast = Vector3.Dot(lastCentre, travel);
        float bendSide = sMiddle < Mathf.Min(sFixed, sLast) ? -1f
                       : sMiddle > Mathf.Max(sFixed, sLast) ? 1f
                       : Mathf.Sign(sMiddle - 0.5f * (sFixed + sLast));
        xSign = -bendSide;
        xAxis = travel * xSign;
        wAxis = Vector3.Cross(xAxis, hAxis); // AngleAxis(+a, wAxis) turns xAxis towards hAxis

        // Bend radius = half the gap between the rows (centreline to centreline).
        radius = bendRadiusOverride > 0f ? bendRadiusOverride : 0.5f * (hTop - hFixed);
        hUp = hTop;
        hLow = hUp - 2f * radius;
        hMid = hUp - radius;

        // Pitch: on a straight row, neighbouring link centres are exactly one
        // pitch apart; round the bend they're slightly closer. So it's the
        // largest centre-to-centre distance.
        pitch = 0f;
        for (int i = 1; i < n; i++)
            pitch = Mathf.Max(pitch, Vector2.Distance(ToPlane2(centres[i]), ToPlane2(centres[i - 1])));
        if (pitch <= 1e-5f)
            return Fail("couldn't measure a link pitch — do the links have meshes?");
        if (radius <= pitch * 0.5f)
            return Fail($"bend radius {radius * 1000f:F1} mm is too tight for a {pitch * 1000f:F1} mm pitch.");

        // Each joint can turn by at most theta, the angle one pitch spans on
        // the bend circle: R = p / (2 sin(theta / 2)).
        theta = 2f * Mathf.Asin(pitch / (2f * radius));
        arcLength = Mathf.PI * radius;

        if (Mathf.Abs(Vector3.Dot(centres[n - 1] - centres[n - 2], hAxis)) > 0.002f)
            Debug.LogWarning($"[DragChain] {name}: the last two links aren't on a straight row — the moving end should be on a straight run in the model.", this);

        // Link directions, worked back from the top row: neighbouring links
        // share a joint, and each joint is half a pitch from its link's middle.
        var direction = new Vector2[n];
        direction[n - 1] = Vector2.right;
        for (int i = n - 2; i >= 0; i--)
        {
            Vector2 sharedJoint = ToPlane2(centres[i + 1]) - direction[i + 1] * (pitch * 0.5f);
            direction[i] = (sharedJoint - ToPlane2(centres[i])).normalized;
        }
        fixedJointRest = ToPlane2(centres[0]) - direction[0] * (pitch * 0.5f);
        movingJointRest = ToPlane2(centres[n - 1]) + direction[n - 1] * (pitch * 0.5f);

        parts = new Part[n];
        for (int i = 0; i < n; i++)
            parts[i] = MakePart(links[i], centres[i], Mathf.Atan2(direction[i].y, direction[i].x));
        fixedPart = MakePart(fixedEndBracket, fixedCentre, Mathf.PI); // bottom row runs towards the bend (-x)
        movingPart = MakePart(movingEndBracket, CentreInFrame(movingEndBracket), 0f);

        // The moving end's joint rides on the carriage.
        Vector3 movingJoint = FromPlane(movingJointRest.x, movingJointRest.y, Vector3.Dot(lastCentre, wAxis));
        movingJointOnCarriage = carriage.InverseTransformPoint(fixedFrame.TransformPoint(movingJoint));

        xMovingHome = movingJointRest.x;
        xFixed = xMovingHome + xSign * fixedEndOffsetFromHome;
        return true;
    }

    private bool Fail(string reason)
    {
        Debug.LogError($"[DragChain] {name}: {reason} The chain won't move.", this);
        return false;
    }

    private Part MakePart(Transform t, Vector3 centre, float angle)
    {
        return new Part
        {
            transform = t,
            restPosition = fixedFrame.InverseTransformPoint(t.position),
            restRotation = Quaternion.Inverse(fixedFrame.rotation) * t.rotation,
            restCentre = ToPlane(centre),
            restAngle = angle
        };
    }

    // The middle of a link: its mesh bounds' centre. The CAD shape is baked
    // into the mesh, so this is where the link really is, not its pivot.
    private Vector3 CentreInFrame(Transform t)
    {
        MeshFilter filter = t.GetComponent<MeshFilter>();
        Vector3 local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.center : Vector3.zero;
        return fixedFrame.InverseTransformPoint(t.TransformPoint(local));
    }

    // ───────────────────────── Every frame ─────────────────────────

    // LateUpdate: ASRSArmController moves the arm in Update, so the chain
    // reads the arm's position for this frame, with no one-frame lag.
    private void LateUpdate()
    {
        CheckArmTravel();

        Vector3 movingJoint = ToPlane(fixedFrame.InverseTransformPoint(carriage.TransformPoint(movingJointOnCarriage)));
        if ((movingJoint - lastMovingJoint).sqrMagnitude < 1e-12f)
            return; // arm hasn't moved — nothing to do
        lastMovingJoint = movingJoint;

        float xMoving = movingJoint.x;
        carriageOffset = xSign * (xMoving - xMovingHome);

        bool valid = Solve(xMoving, out float bend);
        if (valid)
        {
            Walk(bend, joints);
            Measure(new Vector2(xMoving, movingJoint.y));
            valid = endpointError <= endpointTolerance && largestJointAngle <= jointAngleLimit + 0.01f;
        }

        if (valid)
        {
            solvedBend = bend;
            solvedMovingX = xMoving;
            bendTravel = xSign * (bend - bendAtHome);
            bottomRowLength = xFixed - bend;
            topRowLength = xMoving - bend;
            PlaceLinks();
            warnedOutOfReach = false;
        }
        else
        {
            // Don't stretch or teleport anything to hide it: the links stay
            // where they last fitted and the moving end visibly pulls away.
            HoldLastGoodPose();
            if (!warnedOutOfReach)
            {
                warnedOutOfReach = true;
                Debug.LogWarning($"[DragChain] {name}: the arm is at {carriageOffset:F3} m from home, outside what the chain reaches " +
                                 $"({reachableOffsets.x:F3} to {reachableOffsets.y:F3} m). Add Extra Links or move the fixed end.", this);
            }
        }
        chainValid = valid;

        // The moving bracket always stays on the carriage.
        Vector3 movingPos = movingPart.restPosition
                          + xAxis * (xMoving - movingJointRest.x)
                          + hAxis * (movingJoint.y - movingJointRest.y);
        movingPart.transform.SetPositionAndRotation(fixedFrame.TransformPoint(movingPos), fixedFrame.rotation * movingPart.restRotation);
    }

    // Each link sits between its two joints: rotate it about w from its
    // starting direction to the new one, and move its middle onto the
    // midpoint of the joints. Brackets only slide; they never tilt.
    private void PlaceLinks()
    {
        Quaternion frameRotation = fixedFrame.rotation;
        for (int i = 0; i < parts.Length; i++)
        {
            Vector2 a = joints[i];
            Vector2 b = joints[i + 1];
            Part part = parts[i];

            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x);
            Quaternion turn = Quaternion.AngleAxis((angle - part.restAngle) * Mathf.Rad2Deg, wAxis);
            Vector3 middle = FromPlane(0.5f * (a.x + b.x), 0.5f * (a.y + b.y), part.restCentre.z);

            Vector3 position = middle + turn * (part.restPosition - FromPlane(part.restCentre));
            Quaternion rotation = turn * part.restRotation;
            part.transform.SetPositionAndRotation(fixedFrame.TransformPoint(position), frameRotation * rotation);

            goodPositions[i] = position;
            goodRotations[i] = rotation;
        }

        goodFixedPosition = fixedPart.restPosition + FromPlane2(joints[0] - fixedJointRest);
        fixedPart.transform.SetPositionAndRotation(fixedFrame.TransformPoint(goodFixedPosition), frameRotation * fixedPart.restRotation);
        hasGoodPose = true;
    }

    private void HoldLastGoodPose()
    {
        if (!hasGoodPose)
            return;

        Quaternion frameRotation = fixedFrame.rotation;
        for (int i = 0; i < parts.Length; i++)
            parts[i].transform.SetPositionAndRotation(fixedFrame.TransformPoint(goodPositions[i]), frameRotation * goodRotations[i]);
        fixedPart.transform.SetPositionAndRotation(fixedFrame.TransformPoint(goodFixedPosition), frameRotation * fixedPart.restRotation);
    }

    private void Measure(Vector2 movingTarget)
    {
        endpointError = Vector2.Distance(joints[parts.Length], movingTarget);

        linkSpacingError = 0f;
        largestJointAngle = 0f;
        float previous = Mathf.PI; // fixed bracket lies along the bottom row (-x)
        for (int i = 0; i < parts.Length; i++)
        {
            Vector2 d = joints[i + 1] - joints[i];
            linkSpacingError = Mathf.Max(linkSpacingError, Mathf.Abs(d.magnitude - pitch));
            float angle = Mathf.Atan2(d.y, d.x);
            largestJointAngle = Mathf.Max(largestJointAngle, Mathf.Abs(Mathf.DeltaAngle(previous * Mathf.Rad2Deg, angle * Mathf.Rad2Deg)));
            previous = angle;
        }
        largestJointAngle = Mathf.Max(largestJointAngle, Mathf.Abs(Mathf.DeltaAngle(previous * Mathf.Rad2Deg, 0f)));
    }

    private void CheckArmTravel()
    {
        if (armController == null)
            return;

        float min = travelAxis == Axis.X ? armController.MinX : travelAxis == Axis.Y ? armController.MinY : armController.MinZ;
        float max = travelAxis == Axis.X ? armController.MaxX : travelAxis == Axis.Y ? armController.MaxY : armController.MaxZ;
        if (min == lastArmMin && max == lastArmMax)
            return;
        lastArmMin = min;
        lastArmMax = max;
        armTravelLimits = new Vector2(min, max);

        // Each extra link lengthens both straight rows' combined length by
        // one pitch, which adds one pitch of reach on each side.
        float shortfall = Mathf.Max(reachableOffsets.x - min, max - reachableOffsets.y);
        if (shortfall > 1e-4f)
        {
            int needed = Mathf.CeilToInt(shortfall / pitch - 1e-3f);
            Debug.LogWarning($"[DragChain] {name}: the arm can travel {min:F3} to {max:F3} m from home, but the chain only reaches " +
                             $"{reachableOffsets.x:F3} to {reachableOffsets.y:F3} m — add {needed} more link(s) to the prefab.", this);
        }
        else
        {
            Debug.Log($"[DragChain] {name}: covers the arm's full travel ({min:F3} to {max:F3} m from home).", this);
        }
    }

    // ───────────────────────── The chain geometry ─────────────────────────
    //
    // The chain's centreline (side view, bend on the left):
    //
    //            B                         xMoving
    //       ╭────●─────────────────────────●   top row, height hUp
    //      (  R  ● bend centre (B, hMid)
    //       ╰────●────────────●                bottom row, height hLow
    //                         xFixed
    //
    // Total length L is constant: (xFixed - B) + πR + (xMoving - B) = L,
    // so B = (xFixed + xMoving - (L - πR)) / 2. When the arm moves by d,
    // the bend moves by d/2. Both rows must have a length of zero or more,
    // so the arm can reach from xFixed - (L - πR) to xFixed + (L - πR).
    //
    // That formula treats the bend as a smooth curve. Real links are straight
    // chords on the bend circle, which use up the arc slightly faster, so the
    // exact B comes from a short search: lay the joints out one pitch apart
    // from the fixed end and adjust B until the last joint lands exactly on
    // the moving end. That keeps every link exactly one pitch long.

    // False if the chain is too short to reach xMoving.
    private bool Solve(float xMoving, out float bend)
    {
        // The bend can't pass either end: that would mean a row with negative length.
        float high = Mathf.Min(xFixed, xMoving);
        bend = high;
        if (Mismatch(high, xMoving) < -1e-6f)
            return false;

        // Far enough back that the path is longer than the chain.
        float low = high - parts.Length * pitch - radius;
        for (int i = 0; i < 60 && high - low > 1e-7f; i++)
        {
            float mid = 0.5f * (low + high);
            if (Mismatch(mid, xMoving) < 0f) low = mid;
            else high = mid;
        }
        bend = 0.5f * (low + high);
        return true;
    }

    // Path length the chain's joints use up, minus the path length to the
    // moving end. Zero when the last joint lands exactly on the moving end.
    private float Mismatch(float bend, float xMoving)
    {
        float used = Walk(bend, null);
        float path = (xFixed - bend) + arcLength + (xMoving - bend);
        return used - path;
    }

    // Lays the joints out along the path, each exactly one pitch (straight
    // line) from the last. Returns the last joint's distance along the path.
    private float Walk(float bend, Vector2[] output)
    {
        float bottom = xFixed - bend; // bottom row length
        float along = 0f;
        if (output != null)
            output[0] = PathPoint(0f, bottom, bend);

        for (int i = 1; i <= parts.Length; i++)
        {
            along = NextJoint(along, bottom, bend);
            if (output != null)
                output[i] = PathPoint(along, bottom, bend);
        }
        return along;
    }

    // Point at distance `along` the path from the fixed end: bottom row
    // (heading -x), then the bend, then the top row (heading +x).
    private Vector2 PathPoint(float along, float bottom, float bend)
    {
        if (along <= bottom)
            return new Vector2(xFixed - along, hLow);
        if (along <= bottom + arcLength)
        {
            float phi = (along - bottom) / radius;
            return new Vector2(bend - radius * Mathf.Sin(phi), hMid - radius * Mathf.Cos(phi));
        }
        return new Vector2(bend + (along - bottom - arcLength), hUp);
    }

    // Distance along the path of the next joint, one pitch away in a
    // straight line from the joint at `along`.
    private float NextJoint(float along, float bottom, float bend)
    {
        const float eps = 1e-7f;

        if (along < bottom - eps)
        {
            if (along + pitch <= bottom)
                return along + pitch;
            // Next joint is on the bend: where a pitch-radius circle round
            // this joint crosses the bend circle.
            Vector2 joint = new Vector2(xFixed - along, hLow);
            return bottom + radius * BendAngleOnePitchFrom(joint, bend);
        }

        if (along < bottom + arcLength - eps)
        {
            // On the bend, consecutive joints are exactly theta apart.
            float phi = (along - bottom) / radius + theta;
            if (phi <= Mathf.PI)
                return bottom + radius * phi;
            // Next joint is on the top row.
            Vector2 joint = PathPoint(along, bottom, bend);
            float dy = hUp - joint.y;
            float x = joint.x + Mathf.Sqrt(Mathf.Max(0f, pitch * pitch - dy * dy));
            return bottom + arcLength + Mathf.Max(0f, x - bend);
        }

        return along + pitch;
    }

    // Angle round the bend (0 = bottom, π = top) of the point on the bend
    // circle that's exactly one pitch from `joint`.
    private float BendAngleOnePitchFrom(Vector2 joint, float bend)
    {
        Vector2 centre = new Vector2(bend, hMid);
        Vector2 toCentre = centre - joint;
        float distance = toCentre.magnitude;
        if (distance < 1e-6f)
            return theta;

        Vector2 dir = toCentre / distance;
        float along = (pitch * pitch - radius * radius + distance * distance) / (2f * distance);
        float offset = Mathf.Sqrt(Mathf.Max(0f, pitch * pitch - along * along));
        Vector2 foot = joint + dir * along;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);

        float best = float.MaxValue;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 q = foot + perpendicular * (offset * s);
            float phi = Mathf.Atan2(bend - q.x, hMid - q.y);
            if (phi >= -1e-4f && phi < best)
                best = phi;
        }
        return best == float.MaxValue ? theta : Mathf.Max(0f, best);
    }

    // Furthest the moving end can go each way before a row would need a
    // negative length. Done once at startup.
    private void ComputeReach()
    {
        float span = parts.Length * pitch;
        reachHighX = Boundary(xFixed, xFixed + span);
        reachLowX = Boundary(xFixed, xFixed - span);

        float a = xSign * (reachLowX - xMovingHome);
        float b = xSign * (reachHighX - xMovingHome);
        reachableOffsets = new Vector2(Mathf.Min(a, b), Mathf.Max(a, b));
    }

    // Edge between `reachable` and `unreachable` moving-end positions.
    private float Boundary(float reachable, float unreachable)
    {
        for (int i = 0; i < 50; i++)
        {
            float mid = 0.5f * (reachable + unreachable);
            if (Mismatch(Mathf.Min(xFixed, mid), mid) >= -1e-6f) reachable = mid;
            else unreachable = mid;
        }
        return reachable;
    }

    // ───────────────────────── Coordinates ─────────────────────────

    private static Vector3 AxisVector(Axis axis) =>
        axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;

    private Vector3 ToPlane(Vector3 p) => new Vector3(Vector3.Dot(p, xAxis), Vector3.Dot(p, hAxis), Vector3.Dot(p, wAxis));
    private Vector2 ToPlane2(Vector3 p) => new Vector2(Vector3.Dot(p, xAxis), Vector3.Dot(p, hAxis));
    private Vector3 FromPlane(Vector3 p) => xAxis * p.x + hAxis * p.y + wAxis * p.z;
    private Vector3 FromPlane(float x, float h, float w) => xAxis * x + hAxis * h + wAxis * w;
    private Vector3 FromPlane2(Vector2 p) => xAxis * p.x + hAxis * p.y;

    // ───────────────────────── Gizmos ─────────────────────────
    //
    // Select the chain in Play mode: green/red centreline (valid/invalid),
    // yellow joints, green cube = fixed end, cyan cube = moving end, magenta
    // = bend centre and circle, white = the two straight rows, orange = the
    // furthest the moving end can reach each way.
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !ready || joints == null || fixedFrame == null)
            return;

        float w = parts[parts.Length - 1].restCentre.z;
        Gizmos.matrix = fixedFrame.localToWorldMatrix;

        Gizmos.color = chainValid ? Color.green : Color.red;
        for (int i = 0; i < parts.Length; i++)
            Gizmos.DrawLine(FromPlane(joints[i].x, joints[i].y, w), FromPlane(joints[i + 1].x, joints[i + 1].y, w));

        Gizmos.color = Color.yellow;
        for (int i = 0; i <= parts.Length; i++)
            Gizmos.DrawSphere(FromPlane(joints[i].x, joints[i].y, w), pitch * 0.1f);

        Gizmos.color = Color.green;
        Gizmos.DrawCube(FromPlane(xFixed, hLow, w), Vector3.one * pitch * 0.4f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(FromPlane(solvedMovingX, hUp, w), Vector3.one * pitch * 0.4f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(FromPlane(solvedBend, hMid, w), pitch * 0.15f);
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * i / segments, b = Mathf.PI * (i + 1) / segments;
            Gizmos.DrawLine(FromPlane(solvedBend - radius * Mathf.Sin(a), hMid - radius * Mathf.Cos(a), w),
                            FromPlane(solvedBend - radius * Mathf.Sin(b), hMid - radius * Mathf.Cos(b), w));
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(FromPlane(xFixed, hLow, w), FromPlane(solvedBend, hLow, w));
        Gizmos.DrawLine(FromPlane(solvedBend, hUp, w), FromPlane(solvedMovingX, hUp, w));

        Gizmos.color = new Color(1f, 0.5f, 0f);
        foreach (float x in new[] { reachLowX, reachHighX })
            Gizmos.DrawLine(FromPlane(x, hUp - radius * 0.5f, w), FromPlane(x, hUp + radius * 0.5f, w));
    }
}
