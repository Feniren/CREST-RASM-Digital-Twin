// using TMPro;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.XR.Interaction.Toolkit.UI;

// // Builds a VR-clickable numeric keypad (Numeric_Keypad) next to the
// // ASRS_Scorbase_Panel in whichever scene is currently open, wired to type
// // directly into that panel's Table ID field — a stand-in for a physical
// // keyboard's number pad, which isn't usable in VR.
// public static class Numeric_Keypad_Builder
// {
//     private const string PanelName = "Numeric_Keypad";

//     [MenuItem("ASRS/Build Numeric Keypad In Current Scene")]
//     public static void BuildKeypad()
//     {
//         ASRS_Scorbase_Panel scorbasePanel = Object.FindFirstObjectByType<ASRS_Scorbase_Panel>();
//         if (scorbasePanel == null)
//         {
//             Debug.LogError("Numeric_Keypad_Builder: no ASRS_Scorbase_Panel found in the open scene — build that first.");
//             return;
//         }

//         if (scorbasePanel.TableIdField == null)
//         {
//             Debug.LogError("Numeric_Keypad_Builder: the scene's ASRS_Scorbase_Panel has no Table Id Field assigned.");
//             return;
//         }

//         if (GameObject.Find(PanelName) != null)
//         {
//             Debug.LogWarning($"Numeric_Keypad_Builder: '{PanelName}' already exists in the scene — delete it first for a clean rebuild.");
//             Selection.activeGameObject = GameObject.Find(PanelName);
//             return;
//         }

//         Vector3 spawnPos = scorbasePanel.transform.position + scorbasePanel.transform.right * 1.4f;
//         Canvas canvas = CreateWorldCanvas(PanelName, new Vector2(300f, 460f), 0.0018f, spawnPos);
//         canvas.gameObject.AddComponent<GraphicRaycaster>();
//         canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
//         AddBackground(canvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.95f));

//         CreateTMP(canvas.transform, "Title", "KEYPAD", 28f, new Vector2(0f, 208f), new Vector2(260f, 40f)).fontStyle = FontStyles.Bold;

//         // 3x3 digit grid (7 8 9 / 4 5 6 / 1 2 3), then a bottom row of
//         // Clear / 0 / Backspace, then a full-width Enter button — standard
//         // phone/calculator keypad layout.
//         Button[] digitButtons = new Button[10];
//         float[] colX = { -90f, 0f, 90f };
//         float[] rowY = { 140f, 60f, -20f };

//         for (int row = 0; row < 3; row++)
//         {
//             for (int col = 0; col < 3; col++)
//             {
//                 int digit = 7 - row * 3 + col; // row0: 7,8,9  row1: 4,5,6  row2: 1,2,3
//                 digitButtons[digit] = CreateButton(canvas.transform, $"Digit_{digit}", digit.ToString(), new Vector2(colX[col], rowY[row]), new Vector2(76f, 60f));
//             }
//         }

//         Button clearButton = CreateButton(canvas.transform, "Clear_Button", "C", new Vector2(colX[0], -100f), new Vector2(76f, 60f));
//         digitButtons[0] = CreateButton(canvas.transform, "Digit_0", "0", new Vector2(colX[1], -100f), new Vector2(76f, 60f));
//         Button backspaceButton = CreateButton(canvas.transform, "Backspace_Button", "<-", new Vector2(colX[2], -100f), new Vector2(76f, 60f));

//         Button enterButton = CreateButton(canvas.transform, "Enter_Button", "Enter", new Vector2(0f, -180f), new Vector2(260f, 56f));

//         Numeric_Keypad keypad = canvas.gameObject.AddComponent<Numeric_Keypad>();
//         SetRef(keypad, "targetField", scorbasePanel.TableIdField);
//         SetArrayRef(keypad, "digitButtons", digitButtons);
//         SetRef(keypad, "backspaceButton", backspaceButton);
//         SetRef(keypad, "clearButton", clearButton);
//         SetRef(keypad, "enterButton", enterButton);
//         SetRef(keypad, "submitButton", scorbasePanel.GoButton);

//         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
//         Selection.activeGameObject = canvas.gameObject;
//         Debug.Log("Numeric_Keypad_Builder: keypad built next to the SCORBASE panel, wired to its Table Id Field and Go button. Reposition/rotate it to face the play space, then save the scene.");
//     }

//     // ------------------------------------------------------------------
//     // UI helpers (mirrors ASRS_Scorbase_Panel_Builder / Quiz_Panel_Builder)
//     // ------------------------------------------------------------------

//     private static Canvas CreateWorldCanvas(string name, Vector2 sizePx, float scale, Vector3 position)
//     {
//         GameObject go = new GameObject(name, typeof(RectTransform));
//         Canvas canvas = go.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.WorldSpace;
//         RectTransform rect = go.GetComponent<RectTransform>();
//         rect.sizeDelta = sizePx;
//         rect.localScale = Vector3.one * scale;
//         rect.position = position;
//         return canvas;
//     }

//     private static Image AddBackground(Transform parent, Color color)
//     {
//         GameObject bg = new GameObject("Background", typeof(RectTransform));
//         bg.transform.SetParent(parent, false);
//         bg.transform.SetAsFirstSibling();
//         Stretch(bg.GetComponent<RectTransform>());
//         Image image = bg.AddComponent<Image>();
//         image.color = color;
//         return image;
//     }

//     private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
//     {
//         GameObject go = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         RectTransform rect = go.GetComponent<RectTransform>();
//         rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
//         rect.sizeDelta = size;
//         rect.anchoredPosition = anchoredPos;
//         TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
//         tmp.text = text;
//         tmp.fontSize = fontSize;
//         tmp.alignment = TextAlignmentOptions.Center;
//         tmp.color = Color.white;
//         return tmp;
//     }

//     private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
//     {
//         GameObject go = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         RectTransform rect = go.GetComponent<RectTransform>();
//         rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
//         rect.sizeDelta = size;
//         rect.anchoredPosition = anchoredPos;
//         Image image = go.AddComponent<Image>();
//         image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
//         image.type = Image.Type.Sliced;
//         image.color = new Color(0.22f, 0.45f, 0.75f, 1f);
//         Button button = go.AddComponent<Button>();
//         TextMeshProUGUI tmp = CreateTMP(go.transform, "Text", label, Mathf.Min(size.y * 0.42f, 26f), Vector2.zero, size);
//         tmp.raycastTarget = false;
//         return button;
//     }

//     private static void Stretch(RectTransform rect)
//     {
//         rect.anchorMin = Vector2.zero;
//         rect.anchorMax = Vector2.one;
//         rect.offsetMin = Vector2.zero;
//         rect.offsetMax = Vector2.zero;
//     }

//     private static void SetRef(Component component, string property, Object value)
//     {
//         SerializedObject so = new SerializedObject(component);
//         SerializedProperty prop = so.FindProperty(property);

//         if (prop == null)
//         {
//             Debug.LogError($"Numeric_Keypad_Builder: property '{property}' not found on {component.GetType().Name}");
//             return;
//         }

//         prop.objectReferenceValue = value;
//         so.ApplyModifiedPropertiesWithoutUndo();
//     }

//     private static void SetArrayRef(Component component, string property, Object[] values)
//     {
//         SerializedObject so = new SerializedObject(component);
//         SerializedProperty prop = so.FindProperty(property);

//         if (prop == null)
//         {
//             Debug.LogError($"Numeric_Keypad_Builder: property '{property}' not found on {component.GetType().Name}");
//             return;
//         }

//         prop.arraySize = values.Length;
//         for (int i = 0; i < values.Length; i++)
//             prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

//         so.ApplyModifiedPropertiesWithoutUndo();
//     }
// }
