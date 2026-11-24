// Fixed numbers for the ASRS model (Assets/Prefabs/ASRS Variant 1.prefab),
// so nothing has to press Play to find them. All distances are in metres.
//
// Arm travel is an offset from home, the same way ASRSArmController's
// MoveZ / MoveY / MoveX take it. Home is the arm in front of slot 090004
// (SlotsB index 15), which is also where Safe Position parks it.
//
// Z and Y come from the rack's slot positions in the prefab. Each side's
// range runs to the furthest of its 36 slots, so every slot can be reached
// without the arm being clamped short. The two racks aren't exact mirror
// images, so each side has its own Z range.
//
// Measured once, offline, from the prefab (2026-11-24). If the rack is
// moved in the prefab, these need measuring again.
public static class ASRSLimits
{
    // ── Arm Z: along the rack (the column sliding on its rail) ──

    // U side: rows 07-12, SlotsB. The rig starts facing this side.
    public const float UMinZ = -0.5877f;
    public const float UMaxZ = 0.3979f;

    // Non-U side: rows 01-06, SlotsA.
    public const float NonUMinZ = -0.5947f;
    public const float NonUMaxZ = 0.3910f;

    // Both sides together: the furthest the column ever travels. The
    // horizontal drag chain has to cover this whole range.
    public const float MinZ = NonUMinZ;
    public const float MaxZ = UMaxZ;

    // ── Arm Y: lift. The same on both sides: bottom row to top row ──
    public const float MinY = -0.2866f;
    public const float MaxY = 0.4361f;

    // ── Arm X: reach into a slot ──
    // Not measured yet: these are the ±2 m the prefab already had, which
    // never limits anything in practice. Set them once the real safe reach
    // is known.
    public const float MinX = -2f;
    public const float MaxX = 2f;

    // ── Rack layout ──
    public const float RowSpacingY = 0.1445f;    // height between rows (average)
    public const float ColumnSpacingZ = 0.1964f; // distance between columns (average)

    // ── Horizontal drag chain (ArmZ/HorizontalDragChain) ──
    // Measured from the CAD links. DragChainController measures these
    // itself at startup; they're here for reference.
    public const float ChainPitch = 0.0320f;      // joint to joint, one link
    public const float ChainBendRadius = 0.07125f; // centreline, half the gap between the rows
    public const float ChainJointLimitDegrees = 25.95f; // from R = p / (2 sin(θ/2))

    // Z range for whichever side the arm faces. Safe Position isn't a rack
    // side, so it gets the range covering both.
    public static float MinZFor(ASRSArmController.Side side) =>
        side == ASRSArmController.Side.U ? UMinZ :
        side == ASRSArmController.Side.NonU ? NonUMinZ : MinZ;

    public static float MaxZFor(ASRSArmController.Side side) =>
        side == ASRSArmController.Side.U ? UMaxZ :
        side == ASRSArmController.Side.NonU ? NonUMaxZ : MaxZ;
}
