using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Module 2 visual state: lights the PC screens, advances the SCORBASE/CNCBase
// panel readouts, ticks the Search-Home rows, and pops the workpiece onto the
// vice at the end. Driven entirely by Lesson_Sequencer events — the guided
// highlight from Step_Changed, the per-action effects from Action_Performed.
// Panels stay fully visible and clickable throughout (a dim CanvasGroup is the
// "screen off" look) so out-of-order clicks still register as practice errors.
public class Startup_State_Controller : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Action_Button_Registry Registry;

    [Header("Screens")]
    [SerializeField] private CanvasGroup ArmScreen;
    [SerializeField] private CanvasGroup MillScreen;

    [Header("Indicators")]
    [SerializeField] private Image ControllerIndicator;

    [Header("Mill main power (real ProMill 8000 'kaig' switch)")]
    [SerializeField] private Renderer MillPowerPart;

    [Header("Status readouts")]
    [SerializeField] private TMP_Text ArmStatus;
    [SerializeField] private TMP_Text ScorbaseMode;
    [SerializeField] private TMP_Text MillStatus;
    [SerializeField] private TMP_Text MillReadout;
    [SerializeField] private GameObject[] AxisCheckRows;

    [Header("End state")]
    [SerializeField] private GameObject DemoBlock;

    private static readonly Color OnColor = new Color(0.2f, 1f, 0.3f, 1f);
    private static readonly Color OffColor = new Color(0.25f, 0.25f, 0.28f, 1f);
    private const float ScreenOffAlpha = 0.25f;

    private Vector3 blockScale = Vector3.one;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock partBlock;
    private Color millPowerOffColor;
    private bool millPowerCaptured;

    private void Awake(){
        if (DemoBlock != null)
            blockScale = DemoBlock.transform.localScale;
    }

    private void OnEnable(){
        if (Sequencer != null){
            Sequencer.Step_Changed += OnStepChanged;
            Sequencer.Action_Performed += OnActionPerformed;
        }
    }

    private void OnDisable(){
        if (Sequencer != null){
            Sequencer.Step_Changed -= OnStepChanged;
            Sequencer.Action_Performed -= OnActionPerformed;
        }
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        if (index == 0)
            Reset_Cold();

        if (Registry == null)
            return;

        if (Sequencer.Mode == Lesson_Mode.Guided && step != null && step.Kind == Lesson_Step_Kind.Panel_Action)
            Registry.Set_Highlight(step.Target_Marker_Id);
        else
            Registry.Clear_Highlights();
    }

    private void OnActionPerformed(string actionId){
        switch (actionId){
            case "arm_pc_on":
                Set_Screen(ArmScreen, true);
                Set_Text(ArmStatus, "SCORBASE: not launched");
                break;
            case "arm_controller_on":
                Set_Indicator(ControllerIndicator, true);
                break;
            case "scorbase_launch":
                Set_Text(ArmStatus, "Status: Offline");
                break;
            case "scorbase_online":
                Set_Text(ArmStatus, "Status: <color=#66FF88>Active</color>");
                break;
            case "scorbase_home":
                StopAllCoroutines();
                StartCoroutine(Search_Home());
                break;
            case "scorbase_standalone":
                Set_Text(ScorbaseMode, "Mode: <color=#66FF88>Standalone</color>");
                break;
            case "mill_pc_on":
                Set_Screen(MillScreen, true);
                Set_Text(MillStatus, "CNCBase: not launched");
                break;
            case "mill_power_on":
                Set_Mill_Power(true);
                break;
            case "cncbase_launch":
                Set_Text(MillStatus, "Connect: choose Active");
                break;
            case "cncbase_online":
                Set_Text(MillStatus, "Connect: <color=#66FF88>Active</color>");
                break;
            case "cncbase_home":
                Set_Text(MillReadout, "X 0.00   Y 0.00   Z 0.00");
                Set_Text(MillStatus, "<color=#66FF88>Homed</color>");
                break;
            case "run_start_fms":
                Set_Text(MillStatus, "<color=#66FF88>Running: start_fms.nc</color>");
                break;
            case "verify_arm":
                Set_Text(ArmStatus, "Status: <color=#66FF88>Active · Homed · Standalone</color>");
                break;
            case "verify_mill":
                Set_Text(MillStatus, "<color=#66FF88>Running: start_fms.nc</color>");
                Appear_Block();
                break;
        }
    }

    private void Reset_Cold(){
        StopAllCoroutines();

        if (Registry != null)
            Registry.Reset_Tabs();

        Set_Screen(ArmScreen, false);
        Set_Screen(MillScreen, false);
        Set_Indicator(ControllerIndicator, false);
        Set_Mill_Power(false);
        Set_Text(ArmStatus, "Status: —");
        Set_Text(ScorbaseMode, "Mode: CIM");
        Set_Text(MillStatus, "—");
        Set_Text(MillReadout, "X ---   Y ---   Z ---");

        if (AxisCheckRows != null)
            foreach (GameObject row in AxisCheckRows)
                Show(row, false);

        if (DemoBlock != null){
            DemoBlock.transform.localScale = Vector3.zero;
            DemoBlock.SetActive(false);
        }
    }

    private IEnumerator Search_Home(){
        if (AxisCheckRows != null)
            foreach (GameObject row in AxisCheckRows){
                Show(row, true);
                yield return new WaitForSeconds(0.35f);
            }

        Set_Text(ArmStatus, "Status: <color=#66FF88>Active · Homed</color>");
    }

    private void Appear_Block(){
        if (DemoBlock == null)
            return;

        DemoBlock.SetActive(true);
        StartCoroutine(Pop_Block());
    }

    private IEnumerator Pop_Block(){
        float t = 0f;

        while (t < 1f){
            t += Time.deltaTime / 0.4f;
            DemoBlock.transform.localScale = Vector3.Lerp(Vector3.zero, blockScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        DemoBlock.transform.localScale = blockScale;
    }

    private void Set_Screen(CanvasGroup screen, bool on){
        if (screen != null)
            screen.alpha = on ? 1f : ScreenOffAlpha;
    }

    private void Set_Indicator(Image indicator, bool on){
        if (indicator != null)
            indicator.color = on ? OnColor : OffColor;
    }

    // Tints the real mill 'kaig' switch green when powered, restoring its captured
    // base color when reset. Per-renderer MaterialPropertyBlock — matches M1's
    // Marker_State_Toggle and never touches the shared material.
    private void Set_Mill_Power(bool on){
        if (MillPowerPart == null)
            return;

        if (partBlock == null)
            partBlock = new MaterialPropertyBlock();

        if (!millPowerCaptured){
            millPowerOffColor = MillPowerPart.sharedMaterial != null && MillPowerPart.sharedMaterial.HasProperty(BaseColorId)
                ? MillPowerPart.sharedMaterial.GetColor(BaseColorId)
                : Color.white;
            millPowerCaptured = true;
        }

        MillPowerPart.GetPropertyBlock(partBlock);
        partBlock.SetColor(BaseColorId, on ? OnColor : millPowerOffColor);
        MillPowerPart.SetPropertyBlock(partBlock);
    }

    private void Set_Text(TMP_Text text, string value){
        if (text != null)
            text.text = value;
    }

    private void Show(GameObject go, bool on){
        if (go != null)
            go.SetActive(on);
    }
}
