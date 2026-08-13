using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds the standalone SCORBASE-style control panel (ASRS_Scorbase_Panel)
// into whichever scene is currently open, wiring it to that scene's own
// ASRSArmController/ASRSArmTester — works for ASRSModule1.unity or
// ASRSModule2.unity (or any other scene) as long as those two components
// already exist there. After building, reposition/rotate the panel in-editor
// to face the play space and save the scene — placement can't be scripted
// blind, it has to be seen.
//
// See docs/VR_Modules/04_ASRS36_Module1_Plan.md for why this panel is
// deliberately standalone (not wired into any lesson/quiz system yet).
public static class ASRS_Scorbase_Panel_Builder
{
    private const string PanelName = "ASRS_Scorbase_Panel";
    private static readonly Color PendingColor = new Color(1f, 1f, 1f, 0.35f);

    [MenuItem("ASRS/Build SCORBASE Panel In Current Scene")]
    public static void BuildPanel()
    {
        ASRSArmController armController = Object.FindFirstObjectByType<ASRSArmController>();
        ASRSArmTester armTester = Object.FindFirstObjectByType<ASRSArmTester>();

        if (armController == null || armTester == null)
        {
            Debug.LogError("ASRS_Scorbase_Panel_Builder: no ASRSArmController/ASRSArmTester found in the open scene. Add both to the scene (or open one that already has them) before running this.");
            return;
        }

        if (GameObject.Find(PanelName) != null)
        {
            Debug.LogWarning($"ASRS_Scorbase_Panel_Builder: '{PanelName}' already exists in the scene — delete it first for a clean rebuild.");
            Selection.activeGameObject = GameObject.Find(PanelName);
            return;
        }

        // Scale is the panel's only size control — nothing else overrides it,
        // so it's safe to tune this (or the Transform's Scale directly on an
        // already-built instance) up or down freely.
        Vector3 spawnPos = armController.transform.position + Vector3.up * 1.3f + armController.transform.forward * 1.5f;
        Canvas canvas = CreateWorldCanvas(PanelName, new Vector2(520f, 640f), 0.0018f, spawnPos);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        AddBackground(canvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.95f));

        CreateTMP(canvas.transform, "Title", "SCORBASE", 32f, new Vector2(0f, 288f), new Vector2(480f, 44f)).fontStyle = FontStyles.Bold;

        // --- Manual Movement ---
        Label(canvas.transform, "Section_Movement", "MANUAL MOVEMENT", new Vector2(-160f, 244f));

        TextMeshProUGUI speedLabel = CreateTMP(canvas.transform, "Speed_Label", "Speed", 16f, new Vector2(20f, 244f), new Vector2(70f, 28f));
        speedLabel.alignment = TextAlignmentOptions.Right;
        TMP_InputField speedField = CreateInputField(canvas.transform, "Speed_Field", "0.50", new Vector2(150f, 244f), new Vector2(110f, 34f));

        Button zPlus = CreateButton(canvas.transform, "Z_Plus", "Z +", new Vector2(-150f, 198f), new Vector2(90f, 46f));
        Button zMinus = CreateButton(canvas.transform, "Z_Minus", "Z -", new Vector2(-50f, 198f), new Vector2(90f, 46f));
        Button yPlus = CreateButton(canvas.transform, "Y_Plus", "Y +", new Vector2(50f, 198f), new Vector2(90f, 46f));
        Button yMinus = CreateButton(canvas.transform, "Y_Minus", "Y -", new Vector2(150f, 198f), new Vector2(90f, 46f));
        Button xPlus = CreateButton(canvas.transform, "X_Plus", "X +", new Vector2(-50f, 144f), new Vector2(90f, 46f));
        Button xMinus = CreateButton(canvas.transform, "X_Minus", "X -", new Vector2(50f, 144f), new Vector2(90f, 46f));
        Button rotate = CreateButton(canvas.transform, "Rotate_Button", "Rotate Side", new Vector2(0f, 90f), new Vector2(210f, 46f));

        // --- Search Home ---
        Label(canvas.transform, "Section_Home", "SEARCH HOME", new Vector2(-160f, 40f));
        Button searchHome = CreateButton(canvas.transform, "Search_Home_Button", "Search Home", new Vector2(150f, 40f), new Vector2(170f, 42f));

        GameObject checksRow = new GameObject("Axis_Checks", typeof(RectTransform));
        checksRow.transform.SetParent(canvas.transform, false);
        RectTransform checksRect = (RectTransform)checksRow.transform;
        checksRect.anchorMin = checksRect.anchorMax = checksRect.pivot = new Vector2(0.5f, 0.5f);
        checksRect.anchoredPosition = new Vector2(0f, -6f);
        checksRect.sizeDelta = new Vector2(480f, 30f);

        TextMeshProUGUI zCheck = CreateTMP(checksRow.transform, "Check_Z", "Z", 16f, new Vector2(-190f, 0f), new Vector2(56f, 28f));
        TextMeshProUGUI yCheck = CreateTMP(checksRow.transform, "Check_Y", "Y", 16f, new Vector2(-124f, 0f), new Vector2(56f, 28f));
        TextMeshProUGUI xCheck = CreateTMP(checksRow.transform, "Check_X", "X", 16f, new Vector2(-58f, 0f), new Vector2(56f, 28f));
        TextMeshProUGUI rotateCheck = CreateTMP(checksRow.transform, "Check_Rotate", "Rot", 16f, new Vector2(20f, 0f), new Vector2(64f, 28f));
        TextMeshProUGUI robotCheck = CreateTMP(checksRow.transform, "Check_Robot", "Robot", 16f, new Vector2(110f, 0f), new Vector2(90f, 28f));
        zCheck.color = yCheck.color = xCheck.color = rotateCheck.color = robotCheck.color = PendingColor;

        // --- Go To Slot ---
        Label(canvas.transform, "Section_Goto", "GO TO SLOT (TABLE ID)", new Vector2(-130f, -50f));
        TMP_InputField tableIdField = CreateInputField(canvas.transform, "TableId_Field", "070001", new Vector2(-70f, -96f), new Vector2(160f, 42f));
        Button goButton = CreateButton(canvas.transform, "Go_Button", "Go", new Vector2(90f, -96f), new Vector2(110f, 42f));

        TextMeshProUGUI errorText = CreateTMP(canvas.transform, "Error_Text", string.Empty, 15f, new Vector2(0f, -144f), new Vector2(460f, 40f));
        errorText.color = new Color(1f, 0.45f, 0.4f, 1f);
        errorText.gameObject.SetActive(false);

        // --- Status ---
        TextMeshProUGUI status = CreateTMP(canvas.transform, "Status_Text", "Z 0.00   Y 0.00   X 0.00   Side U   Idle", 16f, new Vector2(0f, -196f), new Vector2(480f, 32f));

        ASRS_Scorbase_Panel panel = canvas.gameObject.AddComponent<ASRS_Scorbase_Panel>();
        SetRef(panel, "armController", armController);
        SetRef(panel, "armTester", armTester);
        SetRef(panel, "speedField", speedField);
        SetRef(panel, "zPlusButton", zPlus);
        SetRef(panel, "zMinusButton", zMinus);
        SetRef(panel, "yPlusButton", yPlus);
        SetRef(panel, "yMinusButton", yMinus);
        SetRef(panel, "xPlusButton", xPlus);
        SetRef(panel, "xMinusButton", xMinus);
        SetRef(panel, "rotateButton", rotate);
        SetRef(panel, "searchHomeButton", searchHome);
        SetRef(panel, "zCheck", zCheck);
        SetRef(panel, "yCheck", yCheck);
        SetRef(panel, "xCheck", xCheck);
        SetRef(panel, "rotateCheck", rotateCheck);
        SetRef(panel, "robotCheck", robotCheck);
        SetRef(panel, "tableIdField", tableIdField);
        SetRef(panel, "goButton", goButton);
        SetRef(panel, "errorText", errorText);
        SetRef(panel, "statusText", status);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("ASRS_Scorbase_Panel_Builder: panel built next to the arm. Reposition/rotate it to face the play space, then save the scene.");
    }

    // ------------------------------------------------------------------
    // UI helpers (mirrors the CreateWorldCanvas/CreateTMP/CreateButton
    // conventions in Assets/Members/Colin/Training/Editor/Training_Builder.cs)
    // ------------------------------------------------------------------

    private static Canvas CreateWorldCanvas(string name, Vector2 sizePx, float scale, Vector3 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = sizePx;
        rect.localScale = Vector3.one * scale;
        rect.position = position;
        return canvas;
    }

    private static Image AddBackground(Transform parent, Color color)
    {
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(parent, false);
        bg.transform.SetAsFirstSibling();
        Stretch(bg.GetComponent<RectTransform>());
        Image image = bg.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Label(Transform parent, string name, string text, Vector2 anchoredPos)
    {
        TextMeshProUGUI tmp = CreateTMP(parent, name, text, 16f, anchoredPos, new Vector2(260f, 26f));
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(1f, 1f, 1f, 0.7f);
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.22f, 0.45f, 0.75f, 1f);
        Button button = go.AddComponent<Button>();
        TextMeshProUGUI tmp = CreateTMP(go.transform, "Text", label, Mathf.Min(size.y * 0.42f, 22f), Vector2.zero, size);
        tmp.raycastTarget = false;
        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholderText, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.08f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        Stretch((RectTransform)textArea.transform);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMP(textArea.transform, "Placeholder", placeholderText, 18f, Vector2.zero, size);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        Stretch(placeholder.rectTransform);

        TextMeshProUGUI text = CreateTMP(textArea.transform, "Text", string.Empty, 18f, Vector2.zero, size);
        text.alignment = TextAlignmentOptions.Left;
        Stretch(text.rectTransform);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = (RectTransform)textArea.transform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = string.Empty;
        return input;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRef(Component component, string property, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null)
        {
            Debug.LogError($"ASRS_Scorbase_Panel_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
