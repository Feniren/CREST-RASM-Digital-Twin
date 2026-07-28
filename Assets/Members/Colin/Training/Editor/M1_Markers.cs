using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Training_Builder_Core;

// Module 1 — the clickable component markers on the mill, and the model nodes their
// hitboxes are derived from. This is the only place a ProMill 8000 node path is
// written, so a model re-export edits each one once.
public partial class M1_Module_Builder{
    // Marker ids are the contract between the lesson steps, the scene markers, the
    // parts table and the debug menu. Lesson_Sequencer matches them by string at
    // runtime, so a typo produces a step that can never be completed — with no compile
    // error and no warning.
    private const string MarkerSpindleMotor = "spindle_motor";
    private const string MarkerSpindleHead = "spindle_head";
    private const string MarkerVice = "vice";
    private const string MarkerGuardDoor = "guard_door";
    private const string MarkerEmergencyStop = "emergency_stop";
    private const string MarkerPowerOn = "power_on";
    private const string MarkerDoorUnlock = "door_unlock";
    private const string MarkerElectronicsCabinet = "electronics_cabinet";

    private struct MarkerDef{
        public string Id;
        public string Display;
        public string NodePath;
    }

    // Component list follows the official Intelitek ProMill 8000 Quick Start guide
    // (34-0000-8000 Rev-C) "Machine components" plus teaching-critical parts (guard
    // door). Markers without a NodePath (the vise) use the live vice prefab instead.
    // The \X2\...\X0\ names are Unity's escaping of non-ASCII FBX node names — they
    // cannot be checked by eye, which is why nothing else in the project retypes them.
    private static readonly MarkerDef[] Markers = {
        new MarkerDef{ Id = MarkerSpindleMotor, Display = "Spindle Motor", NodePath = "SpindleBase/SpindleMotor/SM_Rotating" },
        new MarkerDef{ Id = MarkerSpindleHead, Display = "Spindle Head", NodePath = "SpindleBase/SpindleMotor/SM_Static" },
        new MarkerDef{ Id = MarkerVice, Display = "Vise" },
        new MarkerDef{ Id = MarkerGuardDoor, Display = "Guard Door", NodePath = "ProMill8000Body/PB_Column_Structure/PB_Column_Static/doors" },
        new MarkerDef{ Id = MarkerEmergencyStop, Display = "Emergency Stop Button", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/\\X2\\59276025505C\\X0\\" },
        new MarkerDef{ Id = MarkerPowerOn, Display = "Power On", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/kaig" },
        new MarkerDef{ Id = MarkerDoorUnlock, Display = "Door Unlock", NodePath = "ProMill8000Body/PB_Knee_Table/PB_Knee_Static/\\X2\\630994AE\\X0\\" },
        new MarkerDef{ Id = MarkerElectronicsCabinet, Display = "Electronics Cabinet", NodePath = "ProMill8000Body/PB_Knee_Table" },
    };

    // The model node a marker's bounds come from, or null for markers with no backing
    // node. Also how the parts table and the state-toggle wiring reach a node path
    // without retyping it.
    private static string MarkerNodePath(string id){
        foreach (MarkerDef def in Markers)
            if (def.Id == id)
                return def.NodePath;

        return null;
    }

    private static Transform ResolveMarkerNode(string id, Transform mill){
        string path = MarkerNodePath(id);
        return path != null ? mill.Find(path) : null;
    }

    private static void BuildMarkers(Transform group, Transform mill, Transform vice){
        foreach (MarkerDef def in Markers){
            Bounds bounds;

            if (def.Id == MarkerVice && vice != null){
                bounds = RendererBounds(vice);
            }
            else{
                Transform node = def.NodePath != null ? mill.Find(def.NodePath) : null;

                if (node == null){
                    Debug.LogWarning($"M1_Markers: node not found for marker '{def.Id}' ({def.NodePath}) — skipped.");
                    continue;
                }

                bounds = RendererBounds(node);
            }

            // Hand-tuned hitboxes captured in-editor and pinned to absolute world
            // bounds — raw node bounds would overlap or over-cover, so Training/5
            // would otherwise undo the manual placement. Re-bake from the scene if
            // the mill is ever re-placed.
            //  - spindle motor (SM_Rotating) / head (SM_Static): nested geometry,
            //    boxes shrunk, split apart vertically, then nudged in X by hand.
            //  - electronics cabinet (PB_Knee_Table): tightened around the panel.
            if (def.Id == MarkerSpindleMotor)
                bounds = new Bounds(new Vector3(-0.0748f, 1.7883f, 1.8837f), new Vector3(0.16f, 0.16f, 0.14f));
            else if (def.Id == MarkerSpindleHead)
                bounds = new Bounds(new Vector3(-0.0662f, 1.5937f, 1.9063f), new Vector3(0.16f, 0.16f, 0.14f));
            else if (def.Id == MarkerElectronicsCabinet)
                bounds = new Bounds(new Vector3(0.082f, 0.5079f, 1.961f), new Vector3(0.7211f, 0.8047f, 1.1194f));

            BuildComponentMarker(group, def.Id, def.Display, bounds);
        }
    }

    // ------------------------------------------------------------------
    // Marker placement (re-run after a model swap)
    // ------------------------------------------------------------------

    [MenuItem("Training/5 Reposition Markers In Open Scene")]
    public static void RepositionMarkersInOpenScene(){
        GameObject wrapper = GameObject.Find(Colin_Training_Paths.MillWrapperName);
        GameObject group = GameObject.Find(Colin_Training_Paths.MarkerGroupName);

        if (wrapper == null || group == null){
            Debug.LogError($"M1_Markers: {Colin_Training_Paths.MillWrapperName} or {Colin_Training_Paths.MarkerGroupName} not found in the open scene.");
            return;
        }

        Transform mill = wrapper.transform.GetChild(0);
        Transform vice = FindChild(wrapper.transform, Colin_Training_Paths.ViceName);

        // Record which marker each listener is bound to BEFORE the markers are
        // destroyed. The Lesson_Manager carries two Door_Click_Toggles (guard door and
        // door unlock) and two Marker_State_Toggles (power on and emergency stop), so
        // type lookup alone cannot tell them apart, and once the markers are gone the
        // references are fake-null and the pairing is unrecoverable.
        List<KeyValuePair<Component, string>> doorBindings = SnapshotMarkerBindings<Door_Click_Toggle>("DoorMarker");
        List<KeyValuePair<Component, string>> stateBindings = SnapshotMarkerBindings<Marker_State_Toggle>("Marker");

        foreach (Transform child in new List<Transform>(group.transform.GetComponentsInChildren<Transform>()))
            if (child != null && child.parent == group.transform)
                Object.DestroyImmediate(child.gameObject);

        BuildMarkers(group.transform, mill, vice);

        RestoreMarkerBindings(doorBindings, group, "DoorMarker");
        RestoreMarkerBindings(stateBindings, group, "Marker");

        EditorSceneManager.MarkSceneDirty(group.scene);
        Debug.Log($"M1_Markers: markers repositioned from current model bounds; {doorBindings.Count + stateBindings.Count} listener references restored.");
    }

    // Each component of type T paired with the Marker_Id its serialized marker field
    // currently points at.
    private static List<KeyValuePair<Component, string>> SnapshotMarkerBindings<T>(string markerField) where T : Component{
        List<KeyValuePair<Component, string>> bindings = new List<KeyValuePair<Component, string>>();

        foreach (T component in Object.FindObjectsByType<T>(FindObjectsSortMode.None)){
            SerializedProperty prop = new SerializedObject(component).FindProperty(markerField);
            Component_Marker marker = prop != null ? prop.objectReferenceValue as Component_Marker : null;

            if (marker != null)
                bindings.Add(new KeyValuePair<Component, string>(component, marker.Marker_Id));
        }

        return bindings;
    }

    private static void RestoreMarkerBindings(List<KeyValuePair<Component, string>> bindings, GameObject group, string markerField){
        foreach (KeyValuePair<Component, string> binding in bindings){
            Component_Marker marker = FindMarker(group, binding.Value);

            if (marker == null){
                // Door_Click_Toggle.OnEnable dereferences its marker without a null
                // check, so leaving one empty is a play-mode NullReferenceException.
                Debug.LogError($"M1_Markers: marker '{binding.Value}' was not rebuilt — {binding.Key.GetType().Name} left unbound.");
                continue;
            }

            SetRef(binding.Key, markerField, marker);
        }
    }
}
