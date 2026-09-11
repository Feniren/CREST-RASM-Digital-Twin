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
// Visual style: classic Windows industrial control software (flat light-gray
// panels, thin dark borders, square controls, compact grouped sections) —
// deliberately NOT the earlier dark modern-dashboard look. This is a visual
// refactor only: every field this builder wires into ASRS_Scorbase_Panel is
// unchanged from before, so none of the panel's logic/state/event handling
// is affected by the restyle.
//
// See docs/VR_Modules/04_ASRS36_Module1_Plan.md for why this panel is
// deliberately standalone (not wired into any lesson/quiz system yet).
public static class ASRS_Scorbase_Panel_Builder
{
    private const string PanelName = "ASRS_Scorbase_Panel";
    private const float BorderW = 2f;

    // Classic Windows 95/2000-era industrial palette — flat, no gradients.
    private static readonly Color WindowBg = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color PanelFace = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color BorderDark = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color FieldBg = new Color(0.98f, 0.98f, 0.98f, 1f);
    private static readonly Color TextDark = new Color(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Color TitleBarBg = new Color(0.08f, 0.16f, 0.35f, 1f);
    private static readonly Color ErrorRed = new Color(0.5f, 0f, 0f, 1f);
    private static readonly Color OnlineGreen = new Color(0f, 0.35f, 0f, 1f);
    private static readonly Color OfflineGray = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color PendingColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color ArrivedColor = OnlineGreen;

    // Shared by both the Source and Target dropdowns — the three physical
    // locations in the facility. Not yet wired to different behavior per
    // selection (see ASRS_Scorbase_Panel's tooltips) — that's future work.
    private static readonly string[] DropdownOptions = { "ASRS Slot", "Conveyor Belt", "Workstation" };

    [MenuItem("ASRS/Build SCORBASE Panel In Current Scene")]
    public static void BuildPanel()
    {
        ASRSArmController armController = Object.FindFirstObjectByType<ASRSArmController>();
        ASRSArmTester armTester = Object.FindFirstObjectByType<ASRSArmTester>();
        Item_RFID_Sensor_ASRS rfidSensor = Object.FindFirstObjectByType<Item_RFID_Sensor_ASRS>();

        if (armController == null || armTester == null)
        {
            Debug.LogError("ASRS_Scorbase_Panel_Builder: no ASRSArmController/ASRSArmTester found in the open scene. Add both to the scene (or open one that already has them) before running this.");
            return;
        }

        if (rfidSensor == null)
            Debug.LogWarning("ASRS_Scorbase_Panel_Builder: no Item_RFID_Sensor_ASRS found in the open scene — the Pick and Place section's OK button won't do anything until one is assigned.");

        if (GameObject.Find(PanelName) != null)
        {
            Debug.LogWarning($"ASRS_Scorbase_Panel_Builder: '{PanelName}' already exists in the scene — delete it first for a clean rebuild.");
            Selection.activeGameObject = GameObject.Find(PanelName);
            return;
        }

        Vector3 spawnPos = armController.transform.position + Vector3.up * 1.3f + armController.transform.forward * 1.5f;
        Canvas canvas = CreateWorldCanvas(PanelName, new Vector2(640f, 780f), 0.0018f, spawnPos);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        AddBackground(canvas.transform, WindowBg);

        // --- Title bar ---
        GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform));
        titleBar.transform.SetParent(canvas.transform, false);
        RectTransform titleRect = titleBar.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(640f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, 370f);
        Image titleBg = titleBar.AddComponent<Image>();
        titleBg.color = TitleBarBg;
        TextMeshProUGUI titleText = CreateTMP(titleBar.transform, "Title", "SCORBASE  —  ASRS-36 CONTROLLER", 15f, new Vector2(-20f, 0f), new Vector2(600f, 32f));
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // --- Left column: Manual Movement ---
        Transform manualMovement = CreateGroupBox(canvas.transform, "Group_ManualMovement", "MANUAL MOVEMENT", new Vector2(-160f, 190f), new Vector2(300f, 300f));

        TextMeshProUGUI speedLabel = CreateTMP(manualMovement, "Speed_Label", "Speed", 12f, new Vector2(-85f, 110f), new Vector2(70f, 26f));
        speedLabel.alignment = TextAlignmentOptions.Right;
        speedLabel.color = TextDark;
        TMP_InputField speedField = CreateClassicInputField(manualMovement, "Speed_Field", "0.50", new Vector2(35f, 110f), new Vector2(110f, 30f));

        Button zPlus = CreateClassicButton(manualMovement, "Z_Plus", "Z +", new Vector2(-70f, 65f), new Vector2(120f, 36f));
        Button zMinus = CreateClassicButton(manualMovement, "Z_Minus", "Z -", new Vector2(70f, 65f), new Vector2(120f, 36f));
        Button yPlus = CreateClassicButton(manualMovement, "Y_Plus", "Y +", new Vector2(-70f, 20f), new Vector2(120f, 36f));
        Button yMinus = CreateClassicButton(manualMovement, "Y_Minus", "Y -", new Vector2(70f, 20f), new Vector2(120f, 36f));
        Button xPlus = CreateClassicButton(manualMovement, "X_Plus", "X +", new Vector2(-70f, -25f), new Vector2(120f, 36f));
        Button xMinus = CreateClassicButton(manualMovement, "X_Minus", "X -", new Vector2(70f, -25f), new Vector2(120f, 36f));
        Button rotate = CreateClassicButton(manualMovement, "Rotate_Button", "Rotate Side", new Vector2(0f, -80f), new Vector2(260f, 36f));

        // --- Left column: Pick and Place (replaces the old Go To Slot / GP tab) ---
        // Grid columns: Label (right-aligned) | numeric input | dropdown
        // (ID rows only — Part ID and the two Index rows have no dropdown,
        // per "a dropdown for all the ID besides Part ID").
        Transform pickAndPlace = CreateGroupBox(canvas.transform, "Group_PickAndPlace", "PICK AND PLACE", new Vector2(-160f, -140f), new Vector2(300f, 340f));

        const float labelX = -95f, inputX = -15f, dropdownX = 75f;
        const float labelW = 70f, inputW = 75f, dropdownW = 85f;

        TextMeshProUGUI partIdLabel = CreateTMP(pickAndPlace, "PartId_Label", "Part ID", 12f, new Vector2(labelX, 110f), new Vector2(labelW, 26f));
        partIdLabel.alignment = TextAlignmentOptions.Right;
        partIdLabel.color = TextDark;
        TMP_InputField partIdField = CreateClassicInputField(pickAndPlace, "PartId_Field", string.Empty, new Vector2(inputX, 110f), new Vector2(inputW, 30f));
        // Real default value (not just a placeholder hint) — the slotted
        // table itself is "the part" in this simulation, so TEMPLATE is
        // always what's being handled, not an example the trainee overwrites.
        partIdField.text = "TEMPLATE";

        TextMeshProUGUI sourceIdLabel = CreateTMP(pickAndPlace, "SourceId_Label", "Source ID", 12f, new Vector2(labelX, 68f), new Vector2(labelW, 26f));
        sourceIdLabel.alignment = TextAlignmentOptions.Right;
        sourceIdLabel.color = TextDark;
        // Read-only display — ASRS_Scorbase_Panel.OnOk() fills this in from
        // Source Index, it isn't typed independently.
        TMP_InputField sourceIdField = CreateClassicInputField(pickAndPlace, "SourceId_Field", "(from Source Index)", new Vector2(inputX, 68f), new Vector2(inputW, 30f));
        sourceIdField.interactable = false;
        TMP_Dropdown sourceDropdown = CreateClassicDropdown(pickAndPlace, "Source_Dropdown", new Vector2(dropdownX, 68f), new Vector2(dropdownW, 30f), DropdownOptions);

        TextMeshProUGUI sourceIndexLabel = CreateTMP(pickAndPlace, "SourceIndex_Label", "Source Index", 12f, new Vector2(labelX, 26f), new Vector2(labelW, 26f));
        sourceIndexLabel.alignment = TextAlignmentOptions.Right;
        sourceIndexLabel.color = TextDark;
        TMP_InputField sourceIndexField = CreateClassicInputField(pickAndPlace, "SourceIndex_Field", "1-72", new Vector2(inputX, 26f), new Vector2(inputW, 30f));

        TextMeshProUGUI targetIdLabel = CreateTMP(pickAndPlace, "TargetId_Label", "Target ID", 12f, new Vector2(labelX, -16f), new Vector2(labelW, 26f));
        targetIdLabel.alignment = TextAlignmentOptions.Right;
        targetIdLabel.color = TextDark;
        // Same as Source ID — no placeholder/default value yet.
        TMP_InputField targetIdField = CreateClassicInputField(pickAndPlace, "TargetId_Field", string.Empty, new Vector2(inputX, -16f), new Vector2(inputW, 30f));
        TMP_Dropdown targetDropdown = CreateClassicDropdown(pickAndPlace, "Target_Dropdown", new Vector2(dropdownX, -16f), new Vector2(dropdownW, 30f), DropdownOptions);

        TextMeshProUGUI targetIndexLabel = CreateTMP(pickAndPlace, "TargetIndex_Label", "Target Index", 12f, new Vector2(labelX, -58f), new Vector2(labelW, 26f));
        targetIndexLabel.alignment = TextAlignmentOptions.Right;
        targetIndexLabel.color = TextDark;
        TMP_InputField targetIndexField = CreateClassicInputField(pickAndPlace, "TargetIndex_Field", "1-72", new Vector2(inputX, -58f), new Vector2(inputW, 30f));

        Button okButton = CreateClassicButton(pickAndPlace, "OK_Button", "OK", new Vector2(-70f, -110f), new Vector2(110f, 36f));
        Button cancelButton = CreateClassicButton(pickAndPlace, "Cancel_Button", "Cancel", new Vector2(70f, -110f), new Vector2(110f, 36f));

        // --- Right column: Connection (Go Online) ---
        Transform connection = CreateGroupBox(canvas.transform, "Group_Connection", "CONNECTION", new Vector2(165f, 295f), new Vector2(300f, 90f));

        Button goOnline = CreateClassicButton(connection, "Go_Online_Button", "Go Online", new Vector2(-70f, 0f), new Vector2(140f, 36f));
        TextMeshProUGUI onlineStatusText = CreateTMP(connection, "Online_Status_Text", "OFFLINE", 14f, new Vector2(90f, 0f), new Vector2(110f, 32f));
        onlineStatusText.fontStyle = FontStyles.Bold;
        onlineStatusText.color = OfflineGray;

        // --- Right column: Search Home ---
        Transform searchHomeGroup = CreateGroupBox(canvas.transform, "Group_SearchHome", "SEARCH HOME", new Vector2(165f, 150f), new Vector2(300f, 180f));

        Button searchHome = CreateClassicButton(searchHomeGroup, "Search_Home_Button", "Search Home", new Vector2(0f, 55f), new Vector2(200f, 36f));

        TextMeshProUGUI zCheck = CreateTMP(searchHomeGroup, "Check_Z", "Z", 12f, new Vector2(-105f, 5f), new Vector2(50f, 24f));
        TextMeshProUGUI yCheck = CreateTMP(searchHomeGroup, "Check_Y", "Y", 12f, new Vector2(-50f, 5f), new Vector2(50f, 24f));
        TextMeshProUGUI xCheck = CreateTMP(searchHomeGroup, "Check_X", "X", 12f, new Vector2(5f, 5f), new Vector2(50f, 24f));
        TextMeshProUGUI rotateCheck = CreateTMP(searchHomeGroup, "Check_Rotate", "Rot", 12f, new Vector2(65f, 5f), new Vector2(60f, 24f));
        TextMeshProUGUI robotCheck = CreateTMP(searchHomeGroup, "Check_Robot", "ROBOT", 13f, new Vector2(0f, -35f), new Vector2(160f, 28f));
        robotCheck.fontStyle = FontStyles.Bold;
        zCheck.color = yCheck.color = xCheck.color = rotateCheck.color = robotCheck.color = PendingColor;

        // --- Right column: Position readout ---
        Transform positionGroup = CreateGroupBox(canvas.transform, "Group_Position", "POSITION", new Vector2(165f, -100f), new Vector2(300f, 300f));

        TextMeshProUGUI status = CreateTMP(positionGroup, "Status_Text", "Z:  0.00\nY:  0.00\nX:  0.00\nSide:  U\nState:  Idle", 13f, new Vector2(0f, 20f), new Vector2(260f, 220f));
        status.alignment = TextAlignmentOptions.TopLeft;
        status.color = TextDark;

        // --- Footer status bar ---
        GameObject footer = new GameObject("Footer", typeof(RectTransform));
        footer.transform.SetParent(canvas.transform, false);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = footerRect.anchorMax = footerRect.pivot = new Vector2(0.5f, 0.5f);
        footerRect.sizeDelta = new Vector2(640f, 40f);
        footerRect.anchoredPosition = new Vector2(0f, -370f);
        Image footerBorder = footer.AddComponent<Image>();
        footerBorder.color = BorderDark;
        GameObject footerFace = new GameObject("Footer_Face", typeof(RectTransform));
        footerFace.transform.SetParent(footer.transform, false);
        RectTransform footerFaceRect = footerFace.GetComponent<RectTransform>();
        footerFaceRect.anchorMin = Vector2.zero;
        footerFaceRect.anchorMax = Vector2.one;
        footerFaceRect.offsetMin = new Vector2(0f, BorderW);
        footerFaceRect.offsetMax = new Vector2(0f, 0f);
        Image footerFaceImg = footerFace.AddComponent<Image>();
        footerFaceImg.color = PanelFace;

        TextMeshProUGUI errorText = CreateTMP(footerFace.transform, "Error_Text", string.Empty, 12f, new Vector2(-30f, 0f), new Vector2(440f, 30f));
        errorText.alignment = TextAlignmentOptions.Left;
        errorText.color = ErrorRed;
        errorText.gameObject.SetActive(false);

        TextMeshProUGUI controllerLabel = CreateTMP(footerFace.transform, "Controller_Label", "CONTROLLER-USB", 10f, new Vector2(240f, 0f), new Vector2(150f, 24f));
        controllerLabel.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        controllerLabel.alignment = TextAlignmentOptions.Right;

        ASRS_Scorbase_Panel panel = canvas.gameObject.AddComponent<ASRS_Scorbase_Panel>();
        SetRef(panel, "armController", armController);
        SetRef(panel, "armTester", armTester);
        SetRef(panel, "goOnlineButton", goOnline);
        SetRef(panel, "onlineStatusText", onlineStatusText);
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
        SetRef(panel, "rfidSensor", rfidSensor);
        SetRef(panel, "partIdField", partIdField);
        SetRef(panel, "sourceIdField", sourceIdField);
        SetRef(panel, "sourceDropdown", sourceDropdown);
        SetRef(panel, "sourceIndexField", sourceIndexField);
        SetRef(panel, "targetIdField", targetIdField);
        SetRef(panel, "targetDropdown", targetDropdown);
        SetRef(panel, "targetIndexField", targetIndexField);
        SetRef(panel, "okButton", okButton);
        SetRef(panel, "cancelButton", cancelButton);
        SetRef(panel, "errorText", errorText);
        SetRef(panel, "statusText", status);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("ASRS_Scorbase_Panel_Builder: panel built (classic industrial style) next to the arm. Reposition/rotate it to face the play space, then save the scene.");
    }

    // ------------------------------------------------------------------
    // UI helpers — classic Windows industrial look: flat colors, thin
    // borders via nested Image rects (no sprite/rounded corners), no
    // gradients or shadows.
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

    // A bordered "group box" — thin dark border, flat gray face, bold title
    // strip at the top — matching classic Windows grouped panels. Returns
    // the face Transform; position children relative to its own center.
    private static Transform CreateGroupBox(Transform parent, string name, string title, Vector2 anchoredPos, Vector2 size)
    {
        GameObject border = new GameObject(name, typeof(RectTransform));
        border.transform.SetParent(parent, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = size;
        borderRect.anchoredPosition = anchoredPos;
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = BorderDark;

        GameObject face = new GameObject(name + "_Face", typeof(RectTransform));
        face.transform.SetParent(border.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(BorderW, BorderW);
        faceRect.offsetMax = new Vector2(-BorderW, -BorderW);
        Image faceImg = face.AddComponent<Image>();
        faceImg.color = PanelFace;

        TextMeshProUGUI titleTmp = CreateTMP(face.transform, "Title", title, 11f,
            new Vector2(0f, size.y / 2f - 16f), new Vector2(size.x - 20f, 20f));
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = TextDark;

        // Thin separator line under the title, echoing a classic GroupBox.
        GameObject rule = new GameObject("TitleRule", typeof(RectTransform));
        rule.transform.SetParent(face.transform, false);
        RectTransform ruleRect = rule.GetComponent<RectTransform>();
        ruleRect.anchorMin = ruleRect.anchorMax = ruleRect.pivot = new Vector2(0.5f, 0.5f);
        ruleRect.sizeDelta = new Vector2(size.x - 12f, 1f);
        ruleRect.anchoredPosition = new Vector2(0f, size.y / 2f - 28f);
        Image ruleImg = rule.AddComponent<Image>();
        ruleImg.color = BorderDark;

        return face.transform;
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

    // Flat gray button, thin dark border, square corners — no sprite/rounded
    // skin, no accent color, no gradient.
    private static Button CreateClassicButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image border = go.AddComponent<Image>();
        border.color = BorderDark;

        GameObject face = new GameObject("Face", typeof(RectTransform));
        face.transform.SetParent(go.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(BorderW, BorderW);
        faceRect.offsetMax = new Vector2(-BorderW, -BorderW);
        Image faceImg = face.AddComponent<Image>();
        faceImg.color = new Color(0.82f, 0.82f, 0.82f, 1f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = faceImg;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        TextMeshProUGUI tmp = CreateTMP(face.transform, "Text", label, Mathf.Min(size.y * 0.4f, 13f), Vector2.zero, size);
        tmp.color = TextDark;
        tmp.raycastTarget = false;
        return button;
    }

    // Flat white/inset-look text field, thin dark border — reads as a
    // classic Windows text box rather than a rounded modern input.
    private static TMP_InputField CreateClassicInputField(Transform parent, string name, string placeholderText, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image border = go.AddComponent<Image>();
        border.color = BorderDark;

        GameObject face = new GameObject("Face", typeof(RectTransform));
        face.transform.SetParent(go.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(BorderW, BorderW);
        faceRect.offsetMax = new Vector2(-BorderW, -BorderW);
        Image faceImg = face.AddComponent<Image>();
        faceImg.color = FieldBg;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(face.transform, false);
        RectTransform textAreaRect = (RectTransform)textArea.transform;
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(6f, 0f);
        textAreaRect.offsetMax = new Vector2(-6f, 0f);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMP(textArea.transform, "Placeholder", placeholderText, 13f, Vector2.zero, size);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        Stretch(placeholder.rectTransform);

        TextMeshProUGUI text = CreateTMP(textArea.transform, "Text", string.Empty, 13f, Vector2.zero, size);
        text.alignment = TextAlignmentOptions.Left;
        text.color = TextDark;
        Stretch(text.rectTransform);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = faceImg;
        input.textViewport = (RectTransform)textArea.transform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = string.Empty;
        return input;
    }

    // A minimal but fully functional TMP_Dropdown, built to match Unity's own
    // runtime template contract (template/captionText/itemText/options) —
    // styled flat/square like the rest of this panel instead of the default
    // rounded skin. Old-Windows combo boxes don't animate/fade; this doesn't
    // either.
    private static TMP_Dropdown CreateClassicDropdown(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string[] options)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        Image border = go.AddComponent<Image>();
        border.color = BorderDark;

        GameObject face = new GameObject("Face", typeof(RectTransform));
        face.transform.SetParent(go.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(BorderW, BorderW);
        faceRect.offsetMax = new Vector2(-BorderW, -BorderW);
        Image faceImg = face.AddComponent<Image>();
        faceImg.color = FieldBg;

        TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = faceImg;

        TextMeshProUGUI label = CreateTMP(face.transform, "Label", string.Empty, 11f, new Vector2(-8f, 0f), new Vector2(size.x - 26f, size.y - 4f));
        label.alignment = TextAlignmentOptions.Left;
        label.color = TextDark;
        label.raycastTarget = false;

        TextMeshProUGUI arrow = CreateTMP(face.transform, "Arrow", "▼", 8f, new Vector2(size.x / 2f - 12f, 0f), new Vector2(16f, size.y - 4f));
        arrow.color = TextDark;
        arrow.raycastTarget = false;

        // Template: hidden until the dropdown is clicked open.
        GameObject template = new GameObject("Template", typeof(RectTransform));
        template.transform.SetParent(go.transform, false);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, 30f * Mathf.Max(options.Length, 1) + 4f);
        Image templateImg = template.AddComponent<Image>();
        templateImg.color = FieldBg;
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 30f * Mathf.Max(options.Length, 1));

        GameObject item = new GameObject("Item", typeof(RectTransform));
        item.transform.SetParent(content.transform, false);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 30f);
        itemRect.anchoredPosition = Vector2.zero;

        GameObject itemBg = new GameObject("Item Background", typeof(RectTransform));
        itemBg.transform.SetParent(item.transform, false);
        Stretch((RectTransform)itemBg.transform);
        Image itemBgImg = itemBg.AddComponent<Image>();
        itemBgImg.color = FieldBg;

        Toggle itemToggle = item.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBgImg;
        itemToggle.isOn = true;

        GameObject itemCheck = new GameObject("Item Checkmark", typeof(RectTransform));
        itemCheck.transform.SetParent(item.transform, false);
        RectTransform itemCheckRect = itemCheck.GetComponent<RectTransform>();
        itemCheckRect.anchorMin = itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
        itemCheckRect.pivot = new Vector2(0.5f, 0.5f);
        itemCheckRect.sizeDelta = new Vector2(14f, 14f);
        itemCheckRect.anchoredPosition = new Vector2(10f, 0f);
        Image itemCheckImg = itemCheck.AddComponent<Image>();
        itemCheckImg.color = TextDark;
        itemToggle.graphic = itemCheckImg;

        GameObject itemLabelGo = new GameObject("Item Label", typeof(RectTransform));
        itemLabelGo.transform.SetParent(item.transform, false);
        RectTransform itemLabelRect = itemLabelGo.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(24f, 1f);
        itemLabelRect.offsetMax = new Vector2(-6f, -1f);
        TextMeshProUGUI itemLabelTmp = itemLabelGo.AddComponent<TextMeshProUGUI>();
        itemLabelTmp.text = "Option";
        itemLabelTmp.fontSize = 11f;
        itemLabelTmp.color = TextDark;
        itemLabelTmp.alignment = TextAlignmentOptions.Left;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        template.SetActive(false);

        dropdown.template = templateRect;
        dropdown.captionText = label;
        dropdown.itemText = itemLabelTmp;

        dropdown.options.Clear();
        foreach (string option in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));

        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
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
