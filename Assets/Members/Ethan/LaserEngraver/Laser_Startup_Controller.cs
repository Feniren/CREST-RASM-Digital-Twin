using UnityEngine;
using UnityEngine.UI;

// Applies LM2 scene state from Lesson_Sequencer events.
// Whenever the quest system is in, step id maps to Quest_Step.
public class Laser_Startup_Controller : MonoBehaviour
{
    public const string Step_Intro = "intro_startup";
    public const string Step_Set_Manual = "laser_set_manual";
    public const string Step_Download_File = "laser_download_file";
    public const string Step_Place_Gauge = "laser_place_gauge";
    public const string Step_Set_Zero = "laser_set_zero";
    public const string Step_Run = "laser_run";

    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Action_Button_Registry Registry;

    [SerializeField] private Button Continue_Button;

    [Header("Detectors")]
    [SerializeField] private Gauge_Zone Gauge_Zone;
    [SerializeField] private Zero_Point_Check Zero_Check;

    [Header("Machine")]
    [SerializeField] private DriverPanel Driver_Panel;
    [SerializeField] private LaserEngraver Engraver;

    [Header("Readouts")]
    [SerializeField] private string Cold_Mode_Text = "—";
    [SerializeField] private string Manual_Mode_Text = "Manual";

    private enum Gate { None, Gauge_Placed, Zero_Set }

    private Gate gate = Gate.None;
    private Transform headTransform;
    private Vector3 headHomeLocal;

    private void Awake()
    {
        if (Engraver != null && Engraver.laserHeadRef != null)
            Capture_Head(Engraver.laserHeadRef.transform);
    }

    private void OnEnable()
    {
        if (Sequencer != null)
        {
            Sequencer.Step_Changed += OnStepChanged;
            Sequencer.Action_Performed += OnActionPerformed;
            Sequencer.Lesson_Completed += OnLessonCompleted;
        }
    }

    private void OnDisable()
    {
        if (Sequencer != null)
        {
            Sequencer.Step_Changed -= OnStepChanged;
            Sequencer.Action_Performed -= OnActionPerformed;
            Sequencer.Lesson_Completed -= OnLessonCompleted;
        }

        End_Gate();
    }

    private void Update()
    {
        if (gate == Gate.None || Continue_Button == null)
            return;

        Continue_Button.interactable = Gate_Satisfied();
    }

    private void OnStepChanged(Lesson_Step step, int index, int count)
    {
        if (index == 0)
            Reset_Cold();

        End_Gate();

        if (Registry != null)
        {
            if (Sequencer.Mode == Lesson_Mode.Guided && step != null && step.Kind == Lesson_Step_Kind.Panel_Action)
                Registry.Set_Highlight(step.Target_Marker_Id);
            else
                Registry.Clear_Highlights();
        }

        if (step == null)
            return;

        switch (step.Step_Id)
        {
            case Step_Place_Gauge:
                Begin_Gate(Gate.Gauge_Placed);
                break;
            case Step_Set_Zero:
                Begin_Gate(Gate.Zero_Set);
                break;
        }
    }

    private void OnActionPerformed(string actionId)
    {
        switch (actionId)
        {
            case Step_Set_Manual:
                if (Driver_Panel != null)
                    Driver_Panel.Set_Operation_Mode(Manual_Mode_Text);
                break;
            case Step_Download_File:
                if (Driver_Panel != null)
                    Driver_Panel.Download_Selected();
                break;
            case Step_Run:
                Start_Job();
                break;
        }
    }

    private void OnLessonCompleted(Lesson_Result result)
    {
        End_Gate();

        if (Registry != null)
            Registry.Clear_Highlights();
    }

    private void Reset_Cold()
    {
        if (Registry != null)
            Registry.Reset_Tabs();

        if (Driver_Panel != null)
        {
            Driver_Panel.Set_Operation_Mode(Cold_Mode_Text);
            Driver_Panel.Clear_Selection();

            // Wakes the Download button now so its Awake doesn't clear the guided highlight later.
            Driver_Panel.ShowImagePanel();
            Driver_Panel.ShowMainPanel();
        }

        if (Engraver != null)
        {
            Engraver.readyMask = null;

            Laser_Head head = Engraver.laserHeadRef;
            if (head != null)
            {
                // Stops any running raster scan
                head.LoadMask(null);

                if (headTransform == null)
                    Capture_Head(head.transform);
                else
                    headTransform.localPosition = headHomeLocal;
            }
        }

        if (Gauge_Zone != null)
            Gauge_Zone.Show_Visual(false);

        if (Zero_Check != null)
            Zero_Check.Show_Visual(false);
    }

    private void Start_Job()
    {
        if (Engraver == null)
        {
            Debug.LogWarning("[Laser_Startup_Controller] Run: no LaserEngraver assigned.", this);
            return;
        }

        if (Engraver.readyMask == null)
        {
            Debug.LogWarning("[Laser_Startup_Controller] Run: no file has been downloaded to the laser.", this);
            return;
        }

        Engraver.StartJob();
    }

    private void Begin_Gate(Gate g)
    {
        gate = g;

        if (g == Gate.Gauge_Placed && Gauge_Zone != null)
            Gauge_Zone.Show_Visual(true);

        if (g == Gate.Zero_Set && Zero_Check != null)
            Zero_Check.Show_Visual(true);

        if (Continue_Button != null)
            Continue_Button.interactable = Gate_Satisfied();
    }

    private void End_Gate()
    {
        if (gate == Gate.Gauge_Placed && Gauge_Zone != null)
            Gauge_Zone.Show_Visual(false);

        if (gate == Gate.Zero_Set && Zero_Check != null)
            Zero_Check.Show_Visual(false);

        gate = Gate.None;

        if (Continue_Button != null)
            Continue_Button.interactable = true;
    }

    private bool Gate_Satisfied()
    {
        bool gaugePlaced = Gauge_Zone == null || Gauge_Zone.Satisfied;

        switch (gate)
        {
            case Gate.Gauge_Placed:
                return gaugePlaced;
            case Gate.Zero_Set:
                return gaugePlaced && (Zero_Check == null || Zero_Check.Satisfied);
            default:
                return true;
        }
    }

    private void Capture_Head(Transform t)
    {
        headTransform = t;
        headHomeLocal = t.localPosition;
    }
}
