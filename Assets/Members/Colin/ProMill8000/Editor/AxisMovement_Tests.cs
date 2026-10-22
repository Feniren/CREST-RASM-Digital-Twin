using NUnit.Framework;
using ProMill8000;
using UnityEditor;
using UnityEngine;

// EditMode tests: AxisMovement.Awake never runs here, so the origin is 0 and MoveBy is exercised
// directly. The rig mirrors the Intellitek mill: a root rotated Y 270 with the drive under it.
public class AxisMovement_Tests
{
    GameObject root;
    AxisMovement drive;
    Transform rider;

    [SetUp]
    public void Rig()
    {
        root = new GameObject("root");
        root.transform.rotation = Quaternion.Euler(0, 270, 0);
        drive = new GameObject("WB_XAxis_Drive").AddComponent<AxisMovement>();
        drive.transform.SetParent(root.transform, false);
        rider = new GameObject("rider").transform;
        rider.position = new Vector3(1, 2, 3);

        var so = new SerializedObject(drive);
        so.FindProperty("axis").enumValueIndex = (int)MovementAxis.X;
        var deps = so.FindProperty("dependents");
        deps.arraySize = 1;
        deps.GetArrayElementAtIndex(0).objectReferenceValue = rider;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void Strip()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(rider.gameObject);
    }

    [Test]
    public void Moves_Along_Its_Local_Axis_Not_The_World_Axis()
    {
        Near(drive.WorldDirection, Vector3.forward, "local +X under a Y 270 root is world +Z");

        drive.MoveBy(0.5f);

        Near(drive.transform.position, new Vector3(0, 0, 0.5f), "the drive slid along world Z");
        Near(rider.position, new Vector3(1, 2, 3.5f), "the dependent rode along the same direction");
        Assert.That(drive.CurrentPosition, Is.EqualTo(0.5f).Within(1e-5f));
        Assert.That(drive.OffsetFromOrigin, Is.EqualTo(0.5f).Within(1e-5f));
    }

    [Test]
    public void Limits_Clamp_Along_The_Local_Axis()
    {
        var so = new SerializedObject(drive);
        so.FindProperty("enableLimits").boolValue = true;
        so.FindProperty("minOffset").floatValue = -0.1f;
        so.FindProperty("maxOffset").floatValue = 0.1f;
        so.ApplyModifiedPropertiesWithoutUndo();

        drive.MoveBy(0.5f);

        Near(drive.transform.position, new Vector3(0, 0, 0.1f), "clamped to maxOffset along world Z");
    }

    static void Near(Vector3 actual, Vector3 expected, string why)
    {
        Assert.That((actual - expected).magnitude, Is.LessThan(1e-5f), why + " — expected " + expected.ToString("F6") + " got " + actual.ToString("F6"));
    }
}
