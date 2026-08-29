using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Builds the standalone interactive-quiz panel (Quiz_Manager) into whichever
// scene is currently open. Mirrors ASRS_Scorbase_Panel_Builder's conventions.
// This is a status/results/start-prompt readout only — the actual quiz
// interaction happens on the same 3D markers/panel actions SequenceManager
// already teaches with, not on this canvas. After building, reposition/
// rotate it in-editor to face the play space, set Quiz_Manager's Max Allowed
// Errors, then save the scene.
public static class Quiz_Panel_Builder
{
    private const string PanelName = "Quiz_Panel";

    [MenuItem("ASRS/Build Quiz Panel In Current Scene")]
    public static void BuildPanel()
    {
        if (GameObject.Find(PanelName) != null)
        {
            Debug.LogWarning($"Quiz_Panel_Builder: '{PanelName}' already exists in the scene — delete it first for a clean rebuild.");
            Selection.activeGameObject = GameObject.Find(PanelName);
            return;
        }

        SequenceManager sequenceManager = Object.FindFirstObjectByType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("Quiz_Panel_Builder: no SequenceManager found in the open scene. Add one (or open a scene that already has one) before running this.");
            return;
        }

        Vector3 spawnPos = FindSpawnAnchor(sequenceManager);
        Canvas canvas = CreateWorldCanvas(PanelName, new Vector2(480f, 320f), 0.0018f, spawnPos);
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        AddBackground(canvas.transform, new Color(0.05f, 0.07f, 0.12f, 0.95f));

        CreateTMP(canvas.transform, "Title", "QUIZ", 32f, new Vector2(0f, 128f), new Vector2(440f, 44f)).fontStyle = FontStyles.Bold;

        // --- Start-prompt sub-panel — shown when the walkthrough completes,
        // in place of auto-starting the quiz. ---
        GameObject startPanel = new GameObject("Quiz_Start_Panel", typeof(RectTransform));
        startPanel.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)startPanel.transform);

        CreateTMP(startPanel.transform, "Start_Text", "Ready to test what you've learned?", 22f, new Vector2(0f, 40f), new Vector2(420f, 120f));
        Button startQuizButton = CreateButton(startPanel.transform, "Start_Quiz_Button", "Start Quiz", new Vector2(0f, -80f), new Vector2(220f, 56f));

        // --- Quiz status sub-panel ---
        GameObject quizPanel = new GameObject("Quiz_Status_Panel", typeof(RectTransform));
        quizPanel.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)quizPanel.transform);

        TextMeshProUGUI statusText = CreateTMP(quizPanel.transform, "Status_Text", "Find and use the parts in the correct order.\n\nErrors: 0", 22f, new Vector2(0f, 0f), new Vector2(420f, 160f));

        // --- Results sub-panel ---
        GameObject resultsPanel = new GameObject("Quiz_Results_Panel", typeof(RectTransform));
        resultsPanel.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)resultsPanel.transform);

        TextMeshProUGUI resultsText = CreateTMP(resultsPanel.transform, "Results_Text", "Out-of-sequence clicks: 0", 22f, new Vector2(0f, 40f), new Vector2(440f, 120f));

        Button retryButton = CreateButton(resultsPanel.transform, "Retry_Button", "Retry", new Vector2(-110f, -80f), new Vector2(180f, 50f));
        Button continueButton = CreateButton(resultsPanel.transform, "Continue_Button", "Continue", new Vector2(110f, -80f), new Vector2(180f, 50f));

        canvas.gameObject.AddComponent<Hand_Attached_Panel>();

        Quiz_Manager quiz = canvas.gameObject.AddComponent<Quiz_Manager>();
        SetRef(quiz, "sequenceManager", sequenceManager);
        SetRef(quiz, "startPanel", startPanel);
        SetRef(quiz, "startQuizButton", startQuizButton);
        SetRef(quiz, "quizPanel", quizPanel);
        SetRef(quiz, "statusText", statusText);
        SetRef(quiz, "resultsPanel", resultsPanel);
        SetRef(quiz, "resultsText", resultsText);
        SetRef(quiz, "retryButton", retryButton);
        SetRef(quiz, "continueButton", continueButton);

        // Whole panel (and every sub-panel) starts hidden — Quiz_Manager.
        // Awake() deactivates all three and the canvas itself; ShowStartPrompt()
        // (called automatically by SequenceManager once its walkthrough
        // finishes, via the field just wired below) is what first reveals it.
        startPanel.SetActive(false);
        quizPanel.SetActive(false);
        resultsPanel.SetActive(false);

        SetRef(sequenceManager, "quizManager", quiz);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Quiz_Panel_Builder: panel built (start prompt + status + results) and wired to the scene's SequenceManager. Reposition/rotate it to face the play space, then save the scene.");
    }

    // Spawns near an existing SCORBASE panel if one's in the scene (keeps the
    // two related panels close together), otherwise near the SequenceManager
    // — either way this is a starting point, not a final placement.
    private static Vector3 FindSpawnAnchor(SequenceManager sequenceManager)
    {
        GameObject scorbase = GameObject.Find("ASRS_Scorbase_Panel");
        if (scorbase != null)
            return scorbase.transform.position + scorbase.transform.right * 1.2f;

        return sequenceManager.transform.position + Vector3.up * 1.3f;
    }

    // ------------------------------------------------------------------
    // UI helpers (mirrors ASRS_Scorbase_Panel_Builder / Training_Builder)
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
            Debug.LogError($"Quiz_Panel_Builder: property '{property}' not found on {component.GetType().Name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
