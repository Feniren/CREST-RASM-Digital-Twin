// using TMPro;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEngine;
// using UnityEngine.UI;

// // Builds a UI-based label panel next to ControlPanel (on the ASRS
// // Controller-USB console) for the E-Button — a World Space Canvas with a
// // background Image sized to actually contain the title text within its
// // edges, not a solid 3D mesh block with text floating in its middle (the
// // first version of this copied ControlPanel's own MeshFilter/MeshRenderer,
// // which looked wrong for exactly that reason).
// //
// // Runs directly against whatever scene is currently open — build it on the
// // ASRS Variant 1 prefab instance there (e.g. in ASRSModule2.unity), check it
// // looks right, then use Unity's own "Apply as Override" / Inspector
// // Overrides > Apply All on that prefab instance to push it into the source
// // prefab asset. "ControlPanel" is also the name of an unrelated UI canvas
// // prefab used elsewhere in this project, so this checks the object it finds
// // isn't that (a Canvas) before trusting it.
// public static class EButton_Panel_Builder
// {
//     private const string PanelName = "EButtonPanel";
//     private const string TitleText = "EMERGENCY STOP";

//     // EButtPanel's last-known local position before it was deleted — reused
//     // so this panel lands in the same spot, already right next to
//     // ControlPanel, without re-guessing placement.
//     private static readonly Vector3 PanelLocalPosition = new Vector3(0.126f, 0.031f, -0.5388f);

//     [MenuItem("ASRS/Build E-Button Panel Next To Control Panel")]
//     public static void BuildPanel()
//     {
//         GameObject controlPanel = GameObject.Find("ControlPanel");
//         if (controlPanel == null)
//         {
//             Debug.LogError("EButton_Panel_Builder: couldn't find 'ControlPanel' in the open scene.");
//             return;
//         }

//         if (controlPanel.GetComponent<Canvas>() != null)
//         {
//             Debug.LogError("EButton_Panel_Builder: found a 'ControlPanel' with a Canvas on it — that's the unrelated UI canvas prefab, not the physical console panel. Make sure the ASRS console (ASRS Variant 1 prefab instance) is present in the open scene.");
//             return;
//         }

//         if (GameObject.Find(PanelName) != null)
//         {
//             Debug.LogWarning($"EButton_Panel_Builder: '{PanelName}' already exists — delete it first for a clean rebuild.");
//             Selection.activeGameObject = GameObject.Find(PanelName);
//             return;
//         }

//         // --- Canvas: the panel itself, background + text together ---
//         GameObject canvasObj = new GameObject(PanelName, typeof(RectTransform));
//         Undo.RegisterCreatedObjectUndo(canvasObj, "Build E-Button Panel");
//         canvasObj.transform.SetParent(controlPanel.transform.parent, false);
//         canvasObj.transform.localPosition = PanelLocalPosition;
//         // Faces the same way ControlPanel does — matches the console face.
//         canvasObj.transform.localRotation = controlPanel.transform.localRotation;

//         Canvas canvas = canvasObj.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.WorldSpace;

//         RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
//         canvasRect.sizeDelta = new Vector2(220f, 80f);
//         canvasRect.localScale = Vector3.one * 0.001f;

//         // --- Background, stretched to fill the canvas — the actual visible
//         // panel boundary the text sits within, instead of floating in the
//         // middle of an oversized 3D block. ---
//         GameObject backgroundObj = new GameObject("Background", typeof(RectTransform));
//         Undo.RegisterCreatedObjectUndo(backgroundObj, "Build E-Button Panel");
//         backgroundObj.transform.SetParent(canvasObj.transform, false);
//         RectTransform backgroundRect = backgroundObj.GetComponent<RectTransform>();
//         backgroundRect.anchorMin = Vector2.zero;
//         backgroundRect.anchorMax = Vector2.one;
//         backgroundRect.offsetMin = Vector2.zero;
//         backgroundRect.offsetMax = Vector2.zero;
//         Image background = backgroundObj.AddComponent<Image>();
//         background.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

//         // --- Title text, sized to sit within the background's edges with
//         // some padding, instead of an arbitrary size unrelated to the
//         // panel. ---
//         GameObject titleObj = new GameObject("Title", typeof(RectTransform));
//         Undo.RegisterCreatedObjectUndo(titleObj, "Build E-Button Panel");
//         titleObj.transform.SetParent(canvasObj.transform, false);

//         RectTransform titleRect = titleObj.GetComponent<RectTransform>();
//         titleRect.anchorMin = Vector2.zero;
//         titleRect.anchorMax = Vector2.one;
//         titleRect.offsetMin = new Vector2(10f, 8f); // padding from the background's edges
//         titleRect.offsetMax = new Vector2(-10f, -8f);

//         // Matches the existing "Power Button" label already built on USBPower
//         // in this scene: its Canvas has zero rotation, but the Text itself is
//         // rotated -90 degrees around Y relative to its parent — reused here
//         // so this title faces the same direction.
//         titleRect.localRotation = Quaternion.Euler(0f, -90f, 0f);

//         TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
//         title.text = TitleText;
//         title.fontSize = 32f;
//         title.fontStyle = FontStyles.Bold;
//         title.alignment = TextAlignmentOptions.Center;
//         title.color = Color.red;
//         title.textWrappingMode = TextWrappingModes.Normal;

//         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
//         Selection.activeGameObject = canvasObj;
//         Debug.Log($"EButton_Panel_Builder: built '{PanelName}' next to ControlPanel as a UI panel with a '{TitleText}' title. Check it in the scene, then use the prefab instance's Overrides > Apply All (or right-click EButtonPanel > Prefab > Apply as Override) to push it into ASRS Variant 1.prefab.");
//     }
// }
