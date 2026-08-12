using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SCORBASE-style control panel for the ASRS-36 arm: Manual Movement (jog +
// speed), Search Home (per-axis checkmarks), and a Go-to-slot control
// addressed by the rack's own TableID. Wired directly to
// ASRSArmController/ASRSArmTester. The SequenceManager reference is
// optional — leave it unassigned to use this panel standalone; when
// assigned, a completed Search Home or a successful Go reports back to it
// via NotifyAction() so a lesson step can gate on either.
public class ASRS_Scorbase_Panel : MonoBehaviour
{
    private const float JogStep = 0.25f;

    private enum Axis { X, Y, Z }

    [Header("Arm")]
    [SerializeField] private ASRSArmController armController;
    [SerializeField] private ASRSArmTester armTester;

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

    [Header("Go To Slot")]
    [SerializeField] private TMP_InputField tableIdField;
    [SerializeField] private Button goButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Lesson (optional)")]
    [Tooltip("Leave empty to use this panel standalone with no lesson gating.")]
    [SerializeField] private SequenceManager sequenceManager;
    [SerializeField] private string searchHomeActionId = "scorbase_home";
    [SerializeField] private string goActionId = "test_move";

    private static readonly Color PendingColor = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color ArrivedColor = new Color(0.45f, 1f, 0.55f, 1f);

    private void Awake()
    {
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

        if (goButton != null)
            goButton.onClick.AddListener(OnGo);

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
            $"Z {armController.OffsetZ:F2}   Y {armController.OffsetY:F2}   X {armController.OffsetX:F2}   " +
            $"Side {armController.CurrentSide}   " +
            (armController.IsMoving ? "Moving" : "Idle");
    }

    // ------------------------------------------------------------------
    // Manual Movement
    // ------------------------------------------------------------------

    private void Jog(Axis axis, float direction)
    {
        if (armController == null)
            return;

        float step = JogStep * direction;

        switch (axis)
        {
            case Axis.Z:
                armController.MoveZ(armController.OffsetZ + step);
                break;
            case Axis.Y:
                armController.MoveY(armController.OffsetY + step);
                break;
            case Axis.X:
                armController.MoveX(armController.OffsetX + step);
                break;
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
    // Go To Slot
    // ------------------------------------------------------------------

    private void OnGo()
    {
        if (armTester == null || tableIdField == null)
            return;

        string id = tableIdField.text.Trim();

        if (armTester.TryMoveToTableId(id))
        {
            SetError(string.Empty);

            if (sequenceManager != null)
                sequenceManager.NotifyAction(goActionId);
        }
        else
        {
            SetError($"Can't reach {id} — check the ID and which side the arm is on.");
        }
    }

    private void SetError(string message)
    {
        if (errorText == null)
            return;

        errorText.text = message;
        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
