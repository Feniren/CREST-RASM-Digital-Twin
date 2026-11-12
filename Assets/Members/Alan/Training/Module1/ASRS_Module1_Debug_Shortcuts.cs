using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// DEBUG ONLY — shortcuts for jumping around Module 1 while testing, so you
// don't have to replay the whole intro to reach the step you're working on.
//
// Keyboard (Editor / desktop):
//   ]  next step          [  previous step        Home  restart at step 1
//   Shift+1..7  jump to a lesson section (see Jumps)  \  show/hide the help box
// Quest: click BOTH thumbsticks in at the same time = next step.
//
// A jump abandons the current step and starts the target step as if the
// lesson had just reached it (SequenceManager.DebugJumpToStep). It does not
// fake what the skipped steps would have done in the world — e.g. jumping
// back to "Load the ASRS" after loading leaves the table in the rack.
//
// Uses keys and buttons nothing else in the project binds (all face buttons,
// grips, triggers and WASD/E/F/G/X/Y/B/Tab/Space/Enter/Esc are taken).
// Off in release builds unless Enable In Release Builds is ticked.
public class ASRS_Module1_Debug_Shortcuts : MonoBehaviour
{
    [System.Serializable]
    public class Jump
    {
        public string label;
        [Tooltip("Shift + this key jumps here.")]
        public Key key;
        [Tooltip("The step's Required Action Id. '#parts' = first part-identification step (first step with marker targets); '#end' = Module Complete.")]
        public string actionId;
    }

    private const string LogTag = ASRS_Module1_Log.Root + "[Debug]";
    private const string PartsToken = "#parts";
    private const string EndToken = "#end";

    [SerializeField] private SequenceManager sequenceManager;

    [Tooltip("Off = shortcuts only work in the Editor and in Development Builds.")]
    [SerializeField] private bool enableInReleaseBuilds;

    [Header("Keyboard")]
    [SerializeField] private Key nextKey = Key.RightBracket;
    [SerializeField] private Key previousKey = Key.LeftBracket;
    [SerializeField] private Key restartKey = Key.Home;
    [SerializeField] private Key toggleHelpKey = Key.Backslash;
    [SerializeField] private bool showHelp = true;

    [Header("Quest")]
    [Tooltip("Clicking both thumbsticks in together skips to the next step.")]
    [SerializeField] private bool thumbstickChordSkips = true;

    [Header("Jumps (Shift + key)")]
    [SerializeField] private Jump[] jumps =
    {
        new Jump { label = "Material flow",       key = Key.Digit1, actionId = "M1_Flow_1" },
        new Jump { label = "Part identification", key = Key.Digit2, actionId = PartsToken },
        new Jump { label = "Inspect cells",       key = Key.Digit3, actionId = "M1_InspectCells" },
        new Jump { label = "Find the material",   key = Key.Digit4, actionId = "M1_IdentifyMaterial" },
        new Jump { label = "Load the ASRS",       key = Key.Digit5, actionId = "LoadASRS" },
        new Jump { label = "Quiz",                key = Key.Digit6, actionId = "M1_QuizPassed" },
        new Jump { label = "Module complete",     key = Key.Digit7, actionId = EndToken },
    };

    private InputAction leftStickClick;
    private InputAction rightStickClick;
    private string helpText;

    private void Awake()
    {
        if (!Debug.isDebugBuild && !enableInReleaseBuilds)
        {
            enabled = false;
            return;
        }

        if (sequenceManager == null)
            sequenceManager = FindFirstObjectByType<SequenceManager>();

        if (sequenceManager == null)
            ASRS_Module1_Log.Warn(LogTag, "No SequenceManager in the scene — debug shortcuts do nothing.", this);

        leftStickClick = MakeButton("<XRController>{LeftHand}/{Primary2DAxisClick}", "<XRController>{LeftHand}/thumbstickClicked");
        rightStickClick = MakeButton("<XRController>{RightHand}/{Primary2DAxisClick}", "<XRController>{RightHand}/thumbstickClicked");

        helpText = BuildHelpText();
    }

    private static InputAction MakeButton(string usagePath, string fallbackPath)
    {
        InputAction action = new InputAction(type: InputActionType.Button);
        action.AddBinding(usagePath);
        action.AddBinding(fallbackPath);
        return action;
    }

    private void OnEnable()
    {
        leftStickClick?.Enable();
        rightStickClick?.Enable();
    }

    private void OnDisable()
    {
        leftStickClick?.Disable();
        rightStickClick?.Disable();
    }

    private void OnDestroy()
    {
        leftStickClick?.Dispose();
        rightStickClick?.Dispose();
    }

    private void Update()
    {
        if (sequenceManager == null)
            return;

        if (thumbstickChordSkips && leftStickClick != null
            && leftStickClick.IsPressed() && rightStickClick.IsPressed()
            && (leftStickClick.WasPressedThisFrame() || rightStickClick.WasPressedThisFrame()))
        {
            JumpTo(sequenceManager.CurrentStepIndex + 1, "next (thumbsticks)");
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (Pressed(keyboard, toggleHelpKey))
            showHelp = !showHelp;

        bool shift = keyboard.shiftKey.isPressed;

        if (shift)
        {
            foreach (Jump jump in jumps)
            {
                if (jump == null || !Pressed(keyboard, jump.key))
                    continue;

                int index = Resolve(jump.actionId);
                if (index < 0)
                    ASRS_Module1_Log.Warn(LogTag, $"'{jump.label}': no step with Required Action Id '{jump.actionId}' in this scene.", this);
                else
                    JumpTo(index, jump.label);
                return;
            }
            return;
        }

        if (Pressed(keyboard, nextKey))
            JumpTo(sequenceManager.CurrentStepIndex + 1, "next");
        else if (Pressed(keyboard, previousKey))
            JumpTo(Mathf.Max(0, sequenceManager.CurrentStepIndex - 1), "previous");
        else if (Pressed(keyboard, restartKey))
            JumpTo(0, "restart");
    }

    private static bool Pressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    private int Resolve(string actionId)
    {
        SequenceManager.Step[] steps = sequenceManager.Steps;
        if (steps == null)
            return -1;

        if (actionId == EndToken)
            return steps.Length;

        if (actionId == PartsToken)
        {
            for (int i = 0; i < steps.Length; i++)
                if (steps[i].targets != null)
                    foreach (GameObject target in steps[i].targets)
                        if (target != null)
                            return i;
            return -1;
        }

        return sequenceManager.FindStepIndex(actionId);
    }

    private void JumpTo(int index, string reason)
    {
        int count = sequenceManager.Steps != null ? sequenceManager.Steps.Length : 0;
        if (count == 0)
            return;

        index = Mathf.Clamp(index, 0, count);
        if (index == sequenceManager.CurrentStepIndex)
            return;

        ASRS_Module1_Log.Info(LogTag, $"{reason} → step {index + 1} of {count}.", this);
        sequenceManager.DebugJumpToStep(index);
    }

    private string BuildHelpText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Module 1 debug</b>");
        sb.AppendLine($"{KeyName(previousKey)} / {KeyName(nextKey)}   previous / next step");
        sb.AppendLine($"{KeyName(restartKey)}   restart at step 1");
        foreach (Jump jump in jumps)
            if (jump != null && jump.key != Key.None)
                sb.AppendLine($"Shift+{KeyName(jump.key)}   {jump.label}");
        if (thumbstickChordSkips)
            sb.AppendLine("Quest: both thumbsticks in   next step");
        sb.Append($"{KeyName(toggleHelpKey)}   hide this");
        return sb.ToString();
    }

    private static string KeyName(Key key)
    {
        switch (key)
        {
            case Key.LeftBracket: return "[";
            case Key.RightBracket: return "]";
            case Key.Backslash: return "\\";
            default:
                string name = key.ToString();
                return name.StartsWith("Digit") ? name.Substring(5) : name;
        }
    }

    private GUIStyle helpStyle;

    // Desktop / Game view only — not visible inside the headset.
    private void OnGUI()
    {
        if (!showHelp || helpText == null)
            return;

        if (helpStyle == null)
            helpStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 13, padding = new RectOffset(8, 8, 6, 6) };

        GUIContent content = new GUIContent(helpText + CurrentStepLine());
        Vector2 size = helpStyle.CalcSize(content);
        GUI.Box(new Rect(10, 10, size.x, size.y), content, helpStyle);
    }

    private string CurrentStepLine()
    {
        if (sequenceManager == null || sequenceManager.Steps == null)
            return "";

        int count = sequenceManager.Steps.Length;
        int index = sequenceManager.CurrentStepIndex;
        SequenceManager.Step step = sequenceManager.CurrentStep;
        string name = step == null ? "Module Complete"
            : step.stationType != null ? step.stationType.displayName
            : !string.IsNullOrEmpty(step.title) ? step.title
            : !string.IsNullOrEmpty(step.requiredActionId) ? step.requiredActionId
            : "(untitled)";
        return $"\n<i>Now: step {Mathf.Min(index + 1, count)} of {count} — {name}</i>";
    }
}
