using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SCORBASE-style control panel for the ASRS-36 arm: Manual Movement (jog +
// speed), Search Home (per-axis checkmarks), and a Pick and Place section
// (Part/Source/Target ID + OK/Cancel) that retrieves the Source ID table
// from the rack and delivers it to the RFID reader/conveyor via the
// gripper. Wired directly to ASRSArmController/ASRSArmTester/
// Item_RFID_Sensor_ASRS. The SequenceManager reference is optional — leave
// it unassigned to use this panel standalone; when assigned, a completed
// Search Home or a successful pick-and-place reports back to it via
// NotifyAction() so a lesson step can gate on either.
public class ASRS_Scorbase_Panel : MonoBehaviour
{
    private const float JogStep = 0.25f;

    private enum Axis { X, Y, Z }

    [Header("Arm")]
    [SerializeField] private ASRSArmController armController;
    [SerializeField] private ASRSArmTester armTester;

    [Header("Go Online")]
    [Tooltip("SCORBASE must be brought online before it will accept manual movement, homing, or Go commands — matches the real software's Go Online step.")]
    [SerializeField] private Button goOnlineButton;
    [SerializeField] private TextMeshProUGUI onlineStatusText;

    [Header("Manual Movement")]
    [SerializeField] private TMP_InputField speedField;
    [SerializeField] private Button zPlusButton;
    [SerializeField] private Button zMinusButton;
    [SerializeField] private Button yPlusButton;
    [SerializeField] private Button yMinusButton;
    [SerializeField] private Button xPlusButton;
    [SerializeField] private Button xMinusButton;
    [SerializeField] private Button rotateButton;

    [Header("Search Home")]
    [SerializeField] private Button searchHomeButton;
    [SerializeField] private TextMeshProUGUI zCheck;
    [SerializeField] private TextMeshProUGUI yCheck;
    [SerializeField] private TextMeshProUGUI xCheck;
    [SerializeField] private TextMeshProUGUI rotateCheck;
    [SerializeField] private TextMeshProUGUI robotCheck;

    [Header("Pick and Place")]
    [Tooltip("Retrieves the table addressed by Source Index from the rack and carries it to the RFID reader/conveyor via the gripper — the only real destination this simulation delivers to.")]
    [SerializeField] private Item_RFID_Sensor_ASRS rfidSensor;
    [Tooltip("Defaults to TEMPLATE — the slotted table itself is 'the part' in this simulation, there's no separate part-type model to validate against yet.")]
    [SerializeField] private TMP_InputField partIdField;
    [Tooltip("Read-only display of the resolved rack TableID (e.g. 070001) — filled in automatically from Source Index when OK is pressed, not typed independently.")]
    [SerializeField] private TMP_InputField sourceIdField;
    [Tooltip("Which system Source Index refers to (ASRS Slot / Conveyor Belt / Workstation) — not yet wired to different behavior per selection; Source Index currently always addresses an ASRS rack slot.")]
    [SerializeField] private TMP_Dropdown sourceDropdown;
    [Tooltip("The real address: a plain rack slot number, 1-72 (ASRSArmTester.TotalSlots). Converted to the rack's row/column TableID format and used directly to run the pick.")]
    [SerializeField] private TMP_InputField sourceIndexField;
    [Tooltip("Not currently used — there's no arbitrary target-slot routing yet (Item_ASRS.SlotInsert always returns a table to its own home slot), so this stays a plain field for now.")]
    [SerializeField] private TMP_InputField targetIdField;
    [Tooltip("Which system Target Index refers to (ASRS Slot / Conveyor Belt / Workstation) — not yet wired to different behavior per selection, that's future work once arbitrary-target routing is designed.")]
    [SerializeField] private TMP_Dropdown targetDropdown;
    [Tooltip("Validated as a slot number (1-72, matching ASRSArmTester.TotalSlots) before OK proceeds, but doesn't drive any routing yet — the only real destination is the conveyor/RFID reader, which every pick already delivers to regardless of this value.")]
    [SerializeField] private TMP_InputField targetIndexField;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Lesson (optional)")]
    [Tooltip("Leave empty to use this panel standalone with no lesson gating.")]
    [SerializeField] private SequenceManager sequenceManager;
    [SerializeField] private string goOnlineActionId = "scorbase_online";
    [SerializeField] private string searchHomeActionId = "scorbase_home";
    [SerializeField] private string goActionId = "test_move";
    [Tooltip("Fired the first time any jog button is used while online — the digital-twin stand-in for 'navigated to View > Manual Movement and used it'.")]
    [SerializeField] private string manualMovementActionId = "manual_movement_used";

    // Matches the classic-industrial light-gray panel background — the old
    // translucent-white/neon-green pair was tuned for a dark panel and was
    // barely legible once the panel moved to a light background.
    private static readonly Color PendingColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color ArrivedColor = new Color(0f, 0.35f, 0f, 1f);

    private bool manualMovementNotified;

    // Read-only access so a companion tool (e.g. a VR-friendly numeric
    // keypad) can find and type into these without needing its own
    // duplicate reference wired up separately. Points at the Pick and
    // Place section's Source ID field / OK button — the numeric keypad's
    // job (type an ID, confirm) maps directly onto typing a Source ID and
    // pressing OK.
    public TMP_InputField TableIdField => sourceIdField;
    public Button GoButton => okButton;

    // Matches real SCORBASE: nothing else works until you've gone online.
    public bool IsOnline { get; private set; }

    private void Awake()
    {
        if (goOnlineButton != null) goOnlineButton.onClick.AddListener(OnGoOnline);
        SetOnlineStatus(false);

        if (zPlusButton != null) zPlusButton.onClick.AddListener(() => Jog(Axis.Z, 1f));
        if (zMinusButton != null) zMinusButton.onClick.AddListener(() => Jog(Axis.Z, -1f));
        if (yPlusButton != null) yPlusButton.onClick.AddListener(() => Jog(Axis.Y, 1f));
        if (yMinusButton != null) yMinusButton.onClick.AddListener(() => Jog(Axis.Y, -1f));
        if (xPlusButton != null) xPlusButton.onClick.AddListener(() => Jog(Axis.X, 1f));
        if (xMinusButton != null) xMinusButton.onClick.AddListener(() => Jog(Axis.X, -1f));

        if (rotateButton != null)
        {
            rotateButton.onClick.AddListener(() =>
            {
                if (armController == null) return;
                bool isU = armController.CurrentSide == ASRSArmController.Side.U;
                armController.RotateY(isU ? 0f : 180f);
            });
        }

        if (speedField != null)
        {
            speedField.onEndEdit.AddListener(OnSpeedEdited);
            if (armController != null)
                speedField.text = armController.MoveSpeed.ToString("F2");
        }

        if (searchHomeButton != null)
            searchHomeButton.onClick.AddListener(OnSearchHome);

        if (okButton != null)
            okButton.onClick.AddListener(OnOk);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        if (armController != null)
        {
            armController.AxisArrived += OnAxisArrived;
            armController.AllAxesHomed += OnAllAxesHomed;
        }

        ClearChecks();
        SetError(string.Empty);
    }

    private void OnDestroy()
    {
        if (armController == null)
            return;

        armController.AxisArrived -= OnAxisArrived;
        armController.AllAxesHomed -= OnAllAxesHomed;
    }

    private void Update()
    {
        if (statusText == null || armController == null)
            return;

        statusText.text =
            $"Z:  {armController.OffsetZ:F2}\n" +
            $"Y:  {armController.OffsetY:F2}\n" +
            $"X:  {armController.OffsetX:F2}\n" +
            $"Side:  {armController.CurrentSide}\n" +
            $"State:  {(armController.IsMoving ? "Moving" : "Idle")}";
    }

    // ------------------------------------------------------------------
    // Go Online
    // ------------------------------------------------------------------

    private void OnGoOnline()
    {
        SetOnlineStatus(true);
        SetError(string.Empty);

        if (sequenceManager != null)
            sequenceManager.NotifyAction(goOnlineActionId);
    }

    private void SetOnlineStatus(bool online)
    {
        IsOnline = online;

        if (onlineStatusText != null)
        {
            onlineStatusText.text = online ? "ONLINE" : "OFFLINE";
            onlineStatusText.color = online ? ArrivedColor : PendingColor;
        }

        if (goOnlineButton != null)
            goOnlineButton.gameObject.SetActive(!online);
    }

    // ------------------------------------------------------------------
    // Manual Movement
    // ------------------------------------------------------------------

    private void Jog(Axis axis, float direction)
    {
        if (armController == null)
            return;

        if (!IsOnline)
        {
            SetError("Go Online first.");
            return;
        }

        float step = JogStep * direction;
        bool withinLimits;

        switch (axis)
        {
            case Axis.Z:
                withinLimits = armController.MoveZ(armController.OffsetZ + step);
                break;
            case Axis.Y:
                withinLimits = armController.MoveY(armController.OffsetY + step);
                break;
            case Axis.X:
                withinLimits = armController.MoveX(armController.OffsetX + step);
                break;
            default:
                return;
        }

        SetError(withinLimits ? string.Empty : $"{axis} axis is at its travel limit — can't move further.");

        // Fired once, the first time any jog control is actually used while
        // online — the digital-twin stand-in for "navigated to View >
        // Manual Movement and used it".
        if (!manualMovementNotified)
        {
            manualMovementNotified = true;
            if (sequenceManager != null)
                sequenceManager.NotifyAction(manualMovementActionId);
        }
    }

    private void OnSpeedEdited(string text)
    {
        if (armController == null)
            return;

        if (float.TryParse(text, out float speed) && speed > 0f)
            armController.MoveSpeed = speed;
        else if (speedField != null)
            speedField.text = armController.MoveSpeed.ToString("F2");
    }

    // ------------------------------------------------------------------
    // Search Home
    // ------------------------------------------------------------------

    private void OnSearchHome()
    {
        if (armController == null)
            return;

        if (!IsOnline)
        {
            SetError("Go Online first.");
            return;
        }

        ClearChecks();
        armController.HomeAll();
    }

    private void OnAxisArrived(string axis)
    {
        TextMeshProUGUI check = null;

        if (axis == "Z") check = zCheck;
        else if (axis == "Y") check = yCheck;
        else if (axis == "X") check = xCheck;
        else if (axis == "Rotate") check = rotateCheck;

        if (check != null)
            check.color = ArrivedColor;
    }

    private void OnAllAxesHomed()
    {
        if (robotCheck != null)
            robotCheck.color = ArrivedColor;

        if (sequenceManager != null)
            sequenceManager.NotifyAction(searchHomeActionId);
    }

    private void ClearChecks()
    {
        if (zCheck != null) zCheck.color = PendingColor;
        if (yCheck != null) yCheck.color = PendingColor;
        if (xCheck != null) xCheck.color = PendingColor;
        if (rotateCheck != null) rotateCheck.color = PendingColor;
        if (robotCheck != null) robotCheck.color = PendingColor;
    }

    // ------------------------------------------------------------------
    // Pick and Place
    // ------------------------------------------------------------------

    private void OnOk()
    {
        if (rfidSensor == null)
            return;

        if (!IsOnline)
        {
            SetError("Go Online first.");
            return;
        }

        if (armController == null || !armController.IsHomed)
        {
            SetError("Search Home first.");
            return;
        }

        if (sourceIndexField == null)
        {
            Debug.LogError("[ASRS_Scorbase_Panel] Source Index field is not assigned — can't resolve a rack address.", this);
            return;
        }

        // Source Index (1-72) is the real address now — it's converted into
        // the rack's own row/column TableID format and that's what actually
        // drives the pick. Target Index is validated the same way but
        // doesn't route anywhere yet — the only real destination this
        // simulation delivers to is the conveyor/RFID reader, which is
        // already where every pick lands regardless of Target.
        if (!TryValidateIndex(sourceIndexField, "Source Index", out int sourceIndex))
            return;

        if (!TryValidateIndex(targetIndexField, "Target Index", out _))
            return;

        string sourceId = ASRSArmTester.IndexToTableId(sourceIndex);

        // Reflects the resolved rack address back for the trainee to see —
        // Source ID is now a read-out, not something typed independently.
        if (sourceIdField != null)
            sourceIdField.text = sourceId;

        if (rfidSensor.TryManualPickAndPlace(sourceId))
        {
            SetError(string.Empty);

            if (sequenceManager != null)
                sequenceManager.NotifyAction(goActionId);
        }
        else
        {
            SetError($"Can't pick slot {sourceIndex} ({sourceId}) — check the index and that the arm isn't already busy.");
        }
    }

    // Optional fields (Target Index) pass with index=0 when unassigned —
    // required fields (Source Index) are null-checked by the caller before
    // this runs, so a null 'field' here always means "optional and unused".
    private bool TryValidateIndex(TMP_InputField field, string label, out int index)
    {
        index = 0;

        if (field == null)
            return true;

        string text = field.text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            SetError($"{label} is required (1-{ASRSArmTester.TotalSlots}).");
            return false;
        }

        if (!int.TryParse(text, out index) || index < 1 || index > ASRSArmTester.TotalSlots)
        {
            SetError($"{label} must be between 1 and {ASRSArmTester.TotalSlots}.");
            return false;
        }

        return true;
    }

    private void OnCancel()
    {
        if (partIdField != null) partIdField.text = string.Empty;
        if (sourceIdField != null) sourceIdField.text = string.Empty;
        if (sourceIndexField != null) sourceIndexField.text = string.Empty;
        if (targetIdField != null) targetIdField.text = string.Empty;
        if (targetIndexField != null) targetIndexField.text = string.Empty;
        SetError(string.Empty);
    }

    private void SetError(string message)
    {
        if (errorText == null)
            return;

        errorText.text = message;
        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
