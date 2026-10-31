using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lesson_Sequencer : MonoBehaviour{
    [SerializeField] private Marker_Registry Registry;
    [SerializeField] private Mill_Demo_Controller DemoController;

    [Header("Prompt UI")]
    [SerializeField] private GameObject PromptPanel;
    [SerializeField] private TMP_Text PromptText;
    [SerializeField] private Button ContinueButton;

    [Header("Results UI")]
    [SerializeField] private GameObject ResultsPanel;
    [SerializeField] private TMP_Text ResultsText;
    public Button RetryButton;
    public Button ReturnButton;

    public event Action<Lesson_Step, int, int> Step_Changed;
    public event Action<Lesson_Result> Lesson_Completed;
    public event Action<string> Action_Performed;

    public Lesson_Mode Mode { get; private set; }
    public Lesson_Step Current_Step => steps != null && stepIndex >= 0 && stepIndex < steps.Count ? steps[stepIndex] : null;
    public int Step_Index => stepIndex;
    public int Step_Count => steps != null ? steps.Count : 0;

    private Lesson_Definition definition;
    private List<Lesson_Step> steps;
    private int stepIndex;
    private int correctAnswers;
    private int quizTotal;
    private int errors;
    private readonly HashSet<string> completedActions = new HashSet<string>();
    private float startTime;

    private void Awake(){
        ContinueButton.onClick.AddListener(OnContinuePressed);

        if (DemoController != null)
            DemoController.Demo_Finished += OnDemoFinished;

        PromptPanel.SetActive(false);
        ResultsPanel.SetActive(false);
    }

    public void Begin(Lesson_Definition def, Lesson_Mode mode){
        definition = def;
        Mode = mode;
        steps = mode == Lesson_Mode.Guided ? def.Steps : def.Build_Practice_Steps(new System.Random());
        quizTotal = steps.FindAll(s => s.Kind == Lesson_Step_Kind.Select_Component || s.Kind == Lesson_Step_Kind.Panel_Action).Count;
        stepIndex = -1;
        correctAnswers = 0;
        errors = 0;
        completedActions.Clear();
        startTime = Time.time;
        ResultsPanel.SetActive(false);

        if (Registry != null)
            Registry.Set_All_Labels_Visible(false);

        Advance();
    }

    public void Notify_Marker_Selected(Component_Marker marker){
        Lesson_Step step = Current_Step;

        if (step == null || step.Kind != Lesson_Step_Kind.Select_Component)
            return;

        bool isTarget = marker.Marker_Id == step.Target_Marker_Id;

        if (Mode == Lesson_Mode.Guided){
            if (isTarget)
                Advance();
            else
                PromptText.text = ActivePrompt(step) + "\n<color=#FF6666>Not this one — try again.</color>";
        }
        else{
            if (isTarget)
                correctAnswers++;
            Advance();
        }
    }

    public void Notify_Action(string actionId){
        Lesson_Step step = Current_Step;

        if (step == null || step.Kind != Lesson_Step_Kind.Panel_Action)
            return;

        // Already performed — ignore (absorbs double-clicks / VR ray jitter).
        if (completedActions.Contains(actionId))
            return;

        bool isNext = actionId == step.Target_Marker_Id;

        if (Mode == Lesson_Mode.Guided){
            if (isNext){
                completedActions.Add(actionId);
                Action_Performed?.Invoke(actionId);
                Advance();
            }
            else{
                PromptText.text = ActivePrompt(step) + "\n<color=#FF6666>Not yet — follow the highlighted step.</color>";
            }
        }
        else{
            if (isNext){
                correctAnswers++;
                completedActions.Add(actionId);
                Action_Performed?.Invoke(actionId);
                Advance();
            }
            else{
                errors++;
                PromptText.text = ActivePrompt(step) + "\n<color=#FF6666>Out of order — try a different action.</color>";
            }
        }
    }

    public void Show_Results(Lesson_Result result, bool passed){
        PromptPanel.SetActive(false);
        ResultsPanel.SetActive(true);
        string verdict = passed ? "<color=#66FF88>Module complete!</color>" : "<color=#FF6666>Not passed — try again.</color>";

        if (definition.Steps.Exists(s => s.Kind == Lesson_Step_Kind.Panel_Action)){
            ResultsText.text = $"Actions completed: {result.Correct_Answers} / {result.Quiz_Total}\nOut-of-order errors: {result.Errors}\n\n{verdict}";
        }
        else{
            ResultsText.text = $"Quiz score: {result.Correct_Answers} / {result.Quiz_Total}\n\n{verdict}";
        }

        RetryButton.gameObject.SetActive(!passed);
    }

    private void OnContinuePressed(){
        if (Current_Step != null && Current_Step.Kind == Lesson_Step_Kind.Info)
            Advance();
    }

    private void OnDemoFinished(){
        Lesson_Step step = Current_Step;

        if (step != null && (step.Kind == Lesson_Step_Kind.Axis_Demo || step.Kind == Lesson_Step_Kind.Milling_Demo))
            Advance();
    }

    private void Advance(){
        stepIndex++;

        if (stepIndex >= steps.Count){
            Complete();
            return;
        }

        Lesson_Step step = steps[stepIndex];
        PromptPanel.SetActive(true);
        PromptText.text = ActivePrompt(step);
        ContinueButton.gameObject.SetActive(step.Kind == Lesson_Step_Kind.Info);

        Step_Changed?.Invoke(step, stepIndex, steps.Count);

        if (step.Kind == Lesson_Step_Kind.Axis_Demo && DemoController != null)
            DemoController.Play_Axis_Demo();
        else if (step.Kind == Lesson_Step_Kind.Milling_Demo && DemoController != null)
            DemoController.Play_Milling_Demo();
    }

    private void Complete(){
        PromptPanel.SetActive(false);

        Lesson_Result result = new Lesson_Result{
            Module_Id = definition.Module_Id,
            Mode = Mode,
            Correct_Answers = correctAnswers,
            Quiz_Total = quizTotal,
            Errors = errors,
            Elapsed_Seconds = Time.time - startTime
        };

        Lesson_Completed?.Invoke(result);
    }

    private string ActivePrompt(Lesson_Step step){
        if (Mode == Lesson_Mode.Practice && !string.IsNullOrEmpty(step.Practice_Prompt_Text))
            return step.Practice_Prompt_Text;
        return step.Prompt_Text;
    }
}
