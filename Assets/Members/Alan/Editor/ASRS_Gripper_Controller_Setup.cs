using UnityEditor;
using UnityEngine;

// Adds ASRS_Gripper_Controller to the current scene's rack — needed for
// Pick and Place, which the SCORBASE panel's OK button refuses to run
// without one. Placed on "RackHand" (the same GameObject it's attached to
// in ASRSModule2.unity), whose own transform doubles as the gripper's
// rackHand field. Leaves conveyorBelt/rfidSensor unassigned if none exist
// in this scene (e.g. an isolated rack-testing scene) — they're only
// needed for the ASRSSlot -> ConveyorBelt path, not ASRSSlot -> ASRSSlot.
public static class ASRS_Gripper_Controller_Setup
{
    [MenuItem("ASRS/Add Gripper Controller To Current Scene")]
    public static void AddGripperController()
    {
        if (Object.FindFirstObjectByType<ASRS_Gripper_Controller>() != null)
        {
            Debug.LogWarning("ASRS_Gripper_Controller_Setup: a gripper controller already exists in this scene — skipping.");
            return;
        }

        ASRSArmController armController = Object.FindFirstObjectByType<ASRSArmController>();
        ASRSArmTester armTester = Object.FindFirstObjectByType<ASRSArmTester>();
        Item_ASRS rack = Object.FindFirstObjectByType<Item_ASRS>();

        if (armController == null || armTester == null || rack == null)
        {
            Debug.LogError("ASRS_Gripper_Controller_Setup: need an ASRSArmController, ASRSArmTester, and Item_ASRS in the open scene first.");
            return;
        }

        GameObject rackHandObj = GameObject.Find("RackHand");
        if (rackHandObj == null)
        {
            Debug.LogError("ASRS_Gripper_Controller_Setup: couldn't find a 'RackHand' GameObject in the scene (expected under the arm's ArmX).");
            return;
        }

        Item_Conveyor_Belt conveyorBelt = Object.FindFirstObjectByType<Item_Conveyor_Belt>();
        Item_RFID_Sensor_ASRS rfidSensor = Object.FindFirstObjectByType<Item_RFID_Sensor_ASRS>();

        ASRS_Gripper_Controller gripper = Undo.AddComponent<ASRS_Gripper_Controller>(rackHandObj);

        SerializedObject so = new SerializedObject(gripper);
        so.FindProperty("armController").objectReferenceValue = armController;
        so.FindProperty("armTester").objectReferenceValue = armTester;
        so.FindProperty("rackHand").objectReferenceValue = rackHandObj.transform;
        so.FindProperty("rack").objectReferenceValue = rack;

        if (conveyorBelt != null)
            so.FindProperty("conveyorBelt").objectReferenceValue = conveyorBelt;
        else
            Debug.LogWarning("ASRS_Gripper_Controller_Setup: no Item_Conveyor_Belt found — ASRSSlot -> ConveyorBelt won't work here, but ASRSSlot -> ASRSSlot doesn't need one.");

        if (rfidSensor != null)
            so.FindProperty("rfidSensor").objectReferenceValue = rfidSensor;

        so.ApplyModifiedPropertiesWithoutUndo();

        ASRS_Scorbase_Panel panel = Object.FindFirstObjectByType<ASRS_Scorbase_Panel>();
        if (panel != null)
        {
            SerializedObject panelSo = new SerializedObject(panel);
            SerializedProperty gripperProp = panelSo.FindProperty("gripper");
            if (gripperProp != null && gripperProp.objectReferenceValue == null)
            {
                gripperProp.objectReferenceValue = gripper;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("ASRS_Gripper_Controller_Setup: also wired the new gripper into the existing SCORBASE panel.");
            }
        }

        EditorUtility.SetDirty(rackHandObj);
        Debug.Log("ASRS_Gripper_Controller_Setup: added and wired ASRS_Gripper_Controller on 'RackHand'. Save the scene.");
    }
}
