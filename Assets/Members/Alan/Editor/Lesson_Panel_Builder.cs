using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds the widget-styled Lesson Panel and swaps it in for the open
// scene's current info panel. Styling is taken from the existing widget
// panels (JobDetailsPanel/SystemPanel): UIBackground.mat + UIGradient
// sprite, Image_Rounded_Corners with the cyan border, LiberationSans text,
// and a ListItemButton (Widget_Button) for Next.
//
//   LessonPanel                  Canvas (world) + Lesson_Panel + InstructionDisplay + Station_Type_Card
//   ├ Background                 rounded gradient panel
//   ├ Header / Title, Progress   step heading + "Step 3 of 13"
//   ├ Divider
//   ├ Content                    HorizontalLayoutGroup
//   │ ├ MediaBox                 hidden on non-station steps, so Body widens
//   │ │ ├ StationImage           Image + AspectRatioFitter
//   │ │ ├ StationVideo           RawImage + AspectRatioFitter + VideoPlayer
//   │ │ └ Placeholder            "Image / video coming soon"
//   │ └ Body                     step text (auto-size)
//   └ NextButton                 ListItemButton: UI ray click + Interactable_Select
//
// "Build Lesson Panel In Current Scene" creates the prefab if it doesn't
// exist yet, places it where the current info panel is, rewires
// SequenceManager (infoPanel, nextButton, stationCard), and DEACTIVATES —
// never deletes — the old panel. Re-running only rewires. "Rebuild Lesson
// Panel Prefab" overwrites the prefab asset with a fresh build.
public static class Lesson_Panel_Builder
{
    private const string PrefabFolder = "Assets/Members/Alan/Training/UI";
    private const string PrefabPath = PrefabFolder + "/LessonPanel.prefab";
    private const string BackgroundMaterialPath = "Assets/Materials/UIBackground.mat";
    private const string GradientSpritePath = "Assets/Materials/Texture/UIGradient.png";
    private const string ListItemButtonPath = "Assets/Game_Objects/UserInterface/ListItemButton.prefab";

    // Canvas units; the root is scaled 0.01, so the panel is 1.5 m × 0.84 m.
    private static readonly Vector2 PanelSize = new Vector2(150f, 84f);
    private const float PanelScale = 0.01f;
    private const float Inset = 6f;
    private const float MediaWidth = 80f;

    private static readonly Color Cyan = new Color(0.3764706f, 0.8509804f, 1f, 1f);
    private static readonly Color BodyColor = new Color(0.88f, 0.93f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.62f, 0.7f, 0.8f, 1f);
    private static readonly Color MediaFill = new Color(0.03f, 0.07f, 0.12f, 0.9f);

    [MenuItem("ASRS/Build Lesson Panel In Current Scene")]
    public static void BuildInScene()
    {
        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("Lesson_Panel_Builder: no SequenceManager found in the open scene.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = BuildPrefab();
        if (prefab == null)
            return;

        Lesson_Panel panel = Object.FindFirstObjectByType<Lesson_Panel>(FindObjectsInactive.Include);
        InstructionDisplay oldDisplay = GetRef<InstructionDisplay>(sequenceManager, "infoPanel");

        if (panel == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sequenceManager.gameObject.scene);
            panel = instance.GetComponent<Lesson_Panel>();
            PlaceLikeOldPanel(instance.transform, oldDisplay);
        }

        // Retire the old panel (and the station card built under it) — kept
        // in the scene, just switched off, so it can be restored.
        if (oldDisplay != null && oldDisplay != panel.Display)
            oldDisplay.gameObject.SetActive(false);

        panel.gameObject.SetActive(true);

        SetRef(sequenceManager, "infoPanel", panel.Display);
        SetRef(sequenceManager, "nextButton", panel.NextButton.gameObject);
        SetRef(sequenceManager, "stationCard", panel.MediaCard);
        WireNextButton(panel.NextButton, sequenceManager);
        int titled = FillMissingStepTitles(sequenceManager);

        EditorSceneManager.MarkSceneDirty(sequenceManager.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;
        Debug.Log($"Lesson_Panel_Builder: Lesson Panel wired to SequenceManager ({titled} step title(s) filled in). Old info panel deactivated, not deleted. Save the scene.");
    }

    [MenuItem("ASRS/Rebuild Lesson Panel Prefab")]
    public static void RebuildPrefab()
    {
        if (BuildPrefab() != null)
            Debug.Log($"Lesson_Panel_Builder: rebuilt {PrefabPath}. Run 'ASRS/Build Lesson Panel In Current Scene' to re-wire any scene using it.");
    }

    private static GameObject BuildPrefab()
    {
        Material backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(BackgroundMaterialPath);
        Sprite gradient = AssetDatabase.LoadAssetAtPath<Sprite>(GradientSpritePath);
        GameObject listItemButton = AssetDatabase.LoadAssetAtPath<GameObject>(ListItemButtonPath);

        if (backgroundMaterial == null || listItemButton == null)
        {
            Debug.LogError($"Lesson_Panel_Builder: missing widget assets — need {BackgroundMaterialPath} and {ListItemButtonPath}.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Members/Alan/Training", "UI");

        // Root: world-space canvas, clickable by both the XR UI ray and the
        // desktop mouse, like the widget panels.
        GameObject root = new GameObject("LessonPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = PanelSize;
        rootRect.localScale = Vector3.one * PanelScale;
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

        // Background
        Image background = CreateImage("Background", rootRect, backgroundMaterial, gradient, new Color(1f, 1f, 1f, 0.8627451f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddRoundedCorners(background.gameObject, new Color(Cyan.r, Cyan.g, Cyan.b, 0.39215687f), 0.5f, 4.75f);

        // Header: title left, progress right
        RectTransform header = CreateRect("Header", rootRect);
        Stretch(header, new Vector2(0f, 1f), Vector2.one, new Vector2(Inset, -16f), new Vector2(-Inset, -4f));

        TextMeshProUGUI title = CreateText("Title", header, "ASRS Overview", 6.5f, Color.white, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        Stretch(title.rectTransform, Vector2.zero, new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI progress = CreateText("Progress", header, "Step 1 of 1", 3f, Cyan, TextAlignmentOptions.MidlineRight);
        Stretch(progress.rectTransform, new Vector2(0.65f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

        Image divider = CreateImage("Divider", rootRect, null, null, new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f));
        Stretch(divider.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(Inset, -17.35f), new Vector2(-Inset, -17f));

        // Content row: media box + body text. Hiding MediaBox lets the
        // layout group give Body the full width.
        RectTransform content = CreateRect("Content", rootRect);
        Stretch(content, Vector2.zero, Vector2.one, new Vector2(Inset, 15f), new Vector2(-Inset, -20f));
        HorizontalLayoutGroup row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 4f;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        Image mediaBox = CreateImage("MediaBox", content, backgroundMaterial, null, MediaFill);
        AddRoundedCorners(mediaBox.gameObject, new Color(Cyan.r, Cyan.g, Cyan.b, 0.3f), 0.4f, 3f);
        LayoutElement mediaLayout = mediaBox.gameObject.AddComponent<LayoutElement>();
        // Min = preferred so the long, unwrapped Body text can't squeeze the
        // box when the layout group runs short of room.
        mediaLayout.minWidth = MediaWidth;
        mediaLayout.preferredWidth = MediaWidth;
        mediaLayout.flexibleWidth = 0f;

        Image stationImage = CreateImage("StationImage", mediaBox.rectTransform, backgroundMaterial, null, Color.white);
        Stretch(stationImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(1.5f, 1.5f), new Vector2(-1.5f, -1.5f));
        AddRoundedCorners(stationImage.gameObject, new Color(0f, 0f, 0f, 0f), 0f, 2.5f);
        AspectRatioFitter imageAspect = AddFitter(stationImage.gameObject);
        stationImage.gameObject.SetActive(false);

        GameObject videoGO = new GameObject("StationVideo", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
        RectTransform videoRect = videoGO.GetComponent<RectTransform>();
        videoRect.SetParent(mediaBox.rectTransform, false);
        Stretch(videoRect, Vector2.zero, Vector2.one, new Vector2(1.5f, 1.5f), new Vector2(-1.5f, -1.5f));
        videoGO.GetComponent<RawImage>().raycastTarget = false;
        AspectRatioFitter videoAspect = AddFitter(videoGO);
        VideoPlayer player = videoGO.GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.None;
        videoGO.SetActive(false);

        TextMeshProUGUI placeholder = CreateText("Placeholder", mediaBox.rectTransform, "Image / video\ncoming soon", 3f, MutedColor, TextAlignmentOptions.Center);
        placeholder.fontStyle = FontStyles.Italic;
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        placeholder.gameObject.SetActive(false);

        TextMeshProUGUI body = CreateText("Body", content, "", 3.6f, BodyColor, TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = true;
        body.fontSizeMin = 2.4f;
        body.fontSizeMax = 3.6f;
        body.lineSpacing = 8f;
        // Preferred 0: Body takes only the leftover width (flexible), rather
        // than asking for the full single-line width of its text.
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredWidth = 0f;
        bodyLayout.flexibleWidth = 1f;

        // Next: the widget ListItemButton, plus the same Interactable_Select
        // + collider the old NextButton used, so trigger-interact still
        // works alongside the UI ray. Default layer, which the player's
        // interact raycast is masked to.
        GameObject nextGO = (GameObject)PrefabUtility.InstantiatePrefab(listItemButton, rootRect);
        nextGO.name = "NextButton";
        nextGO.layer = 0;
        RectTransform nextRect = nextGO.GetComponent<RectTransform>();
        nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(1f, 0f);
        nextRect.sizeDelta = new Vector2(28f, 8f);
        nextRect.anchoredPosition = new Vector2(-Inset, 4f);

        TextMeshProUGUI nextLabel = nextGO.GetComponentInChildren<TextMeshProUGUI>(true);
        if (nextLabel != null)
        {
            nextLabel.text = "Next  ›";
            nextLabel.fontSize = 3.6f;
        }

        Image nextBackground = nextGO.GetComponent<Image>();
        if (nextBackground != null)
            nextBackground.color = new Color(1f, 1f, 1f, 0.12f);

        SetColor(nextGO.GetComponent<Image_Rounded_Corners>(), "BorderColor", new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f));
        SetColor(nextGO.GetComponent<Widget_Button>(), "HighlightedBorderColor", Cyan);
        SetFloat(nextGO.GetComponent<Widget_Button>(), "HighlightedBackgroundOpacity", 0.3f);

        BoxCollider nextCollider = nextGO.AddComponent<BoxCollider>();
        nextCollider.size = new Vector3(28f, 8f, 0.5f);
        nextCollider.center = new Vector3(-14f, 4f, 0f);
        nextGO.AddComponent<Interactable_Select>();

        // Components on the root, wired to their parts.
        InstructionDisplay display = root.AddComponent<InstructionDisplay>();
        SetRef(display, "infoText", body);
        SetRef(display, "titleText", title);
        SetRef(display, "progressText", progress);

        Station_Type_Card card = root.AddComponent<Station_Type_Card>();
        SetRef(card, "mediaRoot", mediaBox.gameObject);
        SetRef(card, "placeholder", placeholder.gameObject);
        SetRef(card, "image", stationImage);
        SetRef(card, "imageAspect", imageAspect);
        SetRef(card, "videoSurface", videoGO.GetComponent<RawImage>());
        SetRef(card, "videoPlayer", player);
        SetRef(card, "videoAspect", videoAspect);

        Lesson_Panel lessonPanel = root.AddComponent<Lesson_Panel>();
        lessonPanel.Display = display;
        lessonPanel.MediaCard = card;
        lessonPanel.NextButton = nextGO.GetComponent<Button>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    // Puts the new panel where the old panel's canvas was, facing the same
    // way, so it lands in front of the trainee's start position.
    private static void PlaceLikeOldPanel(Transform panel, InstructionDisplay oldDisplay)
    {
        if (oldDisplay == null)
            return;

        Canvas oldCanvas = oldDisplay.GetComponentInChildren<Canvas>(true);
        Transform reference = oldCanvas != null ? oldCanvas.transform : oldDisplay.transform;
        panel.SetPositionAndRotation(reference.position, reference.rotation);
    }

    private static void WireNextButton(Button button, SequenceManager sequenceManager)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddVoidPersistentListener(button.onClick, sequenceManager.OnNextPressed);

        Interactable_Select select = button.GetComponent<Interactable_Select>();
        if (select != null)
        {
            for (int i = select.OnInteractBegin.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(select.OnInteractBegin, i);
            UnityEventTools.AddVoidPersistentListener(select.OnInteractBegin, sequenceManager.OnNextPressed);
        }

        EditorUtility.SetDirty(button);
        if (select != null)
            EditorUtility.SetDirty(select);
    }

    // Headings for Module 1's existing steps, matched on how their infoText
    // starts. Only fills titles that are still blank, so Inspector edits
    // are never overwritten; unmatched steps use the panel's default title.
    private static readonly (string infoTextStart, string title)[] KnownTitles =
    {
        ("Before working with the station", "Storage Stations"),
        ("The physical SmartCIM ASRS-36u", "This Scene: 72 Slots"),
        ("This is the slotted table buffer", "Slotted Table Buffer"),
        ("This is the RFID tag", "RFID Tag"),
        ("This is the epoxy block", "Epoxy Block"),
        ("This is the ASRS-36", "The ASRS Unit"),
        ("Please walk over to the rack", "Rack Door"),
        ("Perform the following action of loading the epoxy block", "Load the Slotted Table"),
        ("Perform the following action of loading the loaded slotted table", "Load the ASRS"),
    };

    private static int FillMissingStepTitles(SequenceManager sequenceManager)
    {
        SerializedObject so = new SerializedObject(sequenceManager);
        SerializedProperty steps = so.FindProperty("steps");
        int filled = 0;

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            SerializedProperty title = step.FindPropertyRelative("title");
            if (!string.IsNullOrEmpty(title.stringValue))
                continue;

            string infoText = step.FindPropertyRelative("infoText").stringValue;
            foreach ((string infoTextStart, string knownTitle) in KnownTitles)
            {
                if (!infoText.StartsWith(infoTextStart))
                    continue;

                title.stringValue = knownTitle;
                filled++;
                break;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return filled;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Material material, Sprite sprite, Color color)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.material = material;
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static AspectRatioFitter AddFitter(GameObject go)
    {
        AspectRatioFitter fitter = go.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        return fitter;
    }

    private static void AddRoundedCorners(GameObject go, Color borderColor, float borderWidth, float cornerRadius)
    {
        Image_Rounded_Corners corners = go.AddComponent<Image_Rounded_Corners>();
        SetColor(corners, "BorderColor", borderColor);
        SetFloat(corners, "BorderWidth", borderWidth);
        SetFloat(corners, "CornerRadius", cornerRadius);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static T GetRef<T>(Component component, string property) where T : Object
    {
        SerializedProperty prop = new SerializedObject(component).FindProperty(property);
        return prop != null ? prop.objectReferenceValue as T : null;
    }

    private static void SetRef(Component component, string property, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);

        if (prop == null)
        {
            Debug.LogError($"Lesson_Panel_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Component component, string property, Color value)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
            return;

        prop.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Component component, string property, float value)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
            return;

        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
