using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Module 1's three-question check, shown on the "M1_QuizPassed" step (the
// last step, so it can't stand in for the physical loading tasks before
// it). One question at a time; each answer is locked in, explained, and
// scored; 2 of 3 passes. A pass enables Continue, which reports the step
// complete exactly once; a fail offers Retry, which resets everything.
//
// Questions are multiple choice, or a rack-cell question answered by
// selecting a cell on the real rack (through ASRS_Rack_Cell_Inspector). A
// cell question names the material stored in Cell Table Id at runtime, and
// accepts any cell holding that same material.
//
// Separate from Quiz_Manager (the "replay the steps in order" quiz used by
// Module 2), which this neither uses nor changes.
public class ASRS_Module1_Quiz : ASRS_Module1_Step_Feature
{
    [Serializable]
    public class Question
    {
        [Tooltip("For a rack-cell question, {material} is replaced with the material stored in Cell Table Id.")]
        [TextArea(2, 4)] public string prompt;
        public string[] options = new string[0];
        public int correctOption;
        [TextArea(2, 4)] public string explanation;

        [Header("Rack-cell question")]
        public bool rackCellQuestion;
        public string cellTableId;
    }

    [Header("Step")]
    [SerializeField] private string quizActionId = "M1_QuizPassed";

    [Header("Questions")]
    [SerializeField] private Question[] questions = new Question[0];
    [SerializeField, Min(1)] private int passMark = 2;
    [SerializeField] private ASRS_Rack_Cell_Inspector cellInspector;

    [Header("UI")]
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button[] optionButtons = new Button[0];
    [SerializeField] private TMP_Text[] optionLabels = new TMP_Text[0];
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;

    private readonly ASRS_Module1_UI.Debouncer debouncer = new ASRS_Module1_UI.Debouncer();
    private ASRS_Quiz_Session session;
    private bool completionReported;
    private string cellMaterial;

    protected override string LogTag => ASRS_Module1_Log.Quiz;

    public string QuizActionId => quizActionId;
    public Question[] Questions => questions;
    public int PassMark => passMark;
    public ASRS_Rack_Cell_Inspector CellInspector => cellInspector;
    public Button[] OptionButtons => optionButtons;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int option = i; // captured per button
            ASRS_Module1_UI.Bind(optionButtons[i], debouncer, () => OnOptionPressed(option));
        }

        ASRS_Module1_UI.Bind(nextButton, debouncer, OnNextPressed);
        ASRS_Module1_UI.Bind(retryButton, debouncer, BeginAttempt);
        ASRS_Module1_UI.Bind(continueButton, debouncer, OnContinuePressed);
    }

    public override bool HandlesAction(string actionId) => actionId == quizActionId;

    protected override void OnFeatureEnter(string actionId)
    {
        completionReported = false;
        BeginAttempt();
    }

    protected override void OnFeatureExit(string actionId)
    {
        if (cellInspector != null)
            cellInspector.EndExternalPick();
    }

    // Fresh attempt: score, answers and buttons all reset.
    public void BeginAttempt()
    {
        if (questions == null || questions.Length == 0)
        {
            ASRS_Module1_Log.Error(LogTag, "No questions configured — run 'ASRS/Module 1/Build Module 1 In Current Scene'.", this);
            return;
        }

        session = new ASRS_Quiz_Session(questions.Length, passMark);
        ASRS_Module1_Log.Info(LogTag, $"Attempt started — {questions.Length} questions, pass mark {passMark}.", this);
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        Question q = questions[session.CurrentIndex];

        ASRS_Module1_UI.SetText(counterText, $"Question {session.CurrentIndex + 1} of {questions.Length}");
        ASRS_Module1_UI.SetText(feedbackText, "");
        ASRS_Module1_UI.SetVisible(nextButton, false);
        ASRS_Module1_UI.SetVisible(retryButton, false);
        ASRS_Module1_UI.SetVisible(continueButton, false);

        if (q.rackCellQuestion)
        {
            ShowOptions(new string[0]);
            cellMaterial = null;

            if (cellInspector != null && cellInspector.TryResolveMaterial(q.cellTableId, out cellMaterial, out _))
            {
                string prompt = q.prompt.Replace("{material}", cellMaterial);
                ASRS_Module1_UI.SetText(questionText, prompt);
                cellInspector.BeginExternalPick($"Quiz: select a cell that stores <b>{cellMaterial}</b>.", OnCellPicked);
            }
            else
            {
                // Without a rack inventory the question can't be asked fairly
                // — count it as correct rather than fail the trainee for a
                // scene fault, and say so loudly.
                ASRS_Module1_Log.Error(LogTag, $"Rack-cell question {session.CurrentIndex + 1} can't resolve a material (inspector missing or rack empty) — scoring it as correct.", this);
                ASRS_Module1_UI.SetText(questionText, "This rack question is unavailable — no stored materials were found.");
                Answer(true, "Skipped (scene problem — see Console).");
            }

            return;
        }

        if (cellInspector != null)
            cellInspector.EndExternalPick();

        ASRS_Module1_UI.SetText(questionText, q.prompt);
        ShowOptions(q.options);
    }

    private void ShowOptions(string[] options)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool used = options != null && i < options.Length && !string.IsNullOrEmpty(options[i]);
            ASRS_Module1_UI.SetVisible(optionButtons[i], used);
            if (used)
                SetOptionLabel(i, options[i], null);
        }
    }

    // Widget_Button repaints its background on every hover change, so the
    // verdict goes on the label instead of the button colour.
    private void SetOptionLabel(int option, string text, string verdict)
    {
        if (option >= optionLabels.Length || optionLabels[option] == null)
            return;

        string label = $"{(char)('A' + option)}.  {text}";
        optionLabels[option].text = verdict == null ? label : $"{label}   <b>{verdict}</b>";
    }

    private void OnOptionPressed(int option)
    {
        if (!IsShowing || session == null || session.IsFinished || session.CurrentAnswered)
            return;

        Question q = questions[session.CurrentIndex];
        if (q.rackCellQuestion || option >= q.options.Length)
            return;

        bool correct = option == q.correctOption;
        SetOptionLabel(option, q.options[option], correct ? "<color=#66FF88>— correct</color>" : "<color=#FF8866>— your answer</color>");
        if (!correct && q.correctOption >= 0 && q.correctOption < q.options.Length)
            SetOptionLabel(q.correctOption, q.options[q.correctOption], "<color=#66FF88>— correct answer</color>");

        string verdict = correct
            ? "<color=#66FF88>Correct.</color>"
            : $"<color=#FF8866>Not quite — the answer is {(char)('A' + q.correctOption)}.</color>";
        Answer(correct, $"{verdict} {q.explanation}");
    }

    private void OnCellPicked(ASRS_Cell_Info info)
    {
        if (!IsShowing || session == null || session.IsFinished || session.CurrentAnswered)
            return;

        Question q = questions[session.CurrentIndex];
        if (!q.rackCellQuestion)
            return;

        bool correct = ASRS_Rack_Cell_Inspector.SameMaterial(info.Material, cellMaterial);
        string holds = !info.Occupied ? "is empty" : info.Material != null ? $"stores {info.Material}" : "holds an empty template";
        string verdict = correct
            ? $"<color=#66FF88>Correct — cell {info.TableId} stores {cellMaterial}.</color>"
            : $"<color=#FF8866>Not quite — cell {info.TableId} {holds}, not {cellMaterial}.</color>";

        if (cellInspector != null)
        {
            cellInspector.ShowFeedback(verdict);
            cellInspector.EndExternalPick();
        }

        Answer(correct, $"{verdict} {q.explanation}");
    }

    private void Answer(bool correct, string feedback)
    {
        if (!session.Submit(correct))
            return; // already answered — a double press can't score twice

        ASRS_Module1_Log.Info(LogTag, $"Q{session.CurrentIndex + 1}: {(correct ? "correct" : "wrong")} (score {session.Score}/{questions.Length}).", this);
        ASRS_Module1_UI.SetText(feedbackText, feedback);
        ASRS_Module1_UI.SetText(nextLabel, session.CurrentIndex + 1 < questions.Length ? "Next question  ›" : "See result  ›");
        ASRS_Module1_UI.SetVisible(nextButton, true);
    }

    private void OnNextPressed()
    {
        if (!IsShowing || session == null || !session.Advance())
            return;

        if (session.IsFinished)
            ShowResult();
        else
            ShowQuestion();
    }

    private void ShowResult()
    {
        ShowOptions(new string[0]);
        ASRS_Module1_UI.SetVisible(nextButton, false);

        bool passed = session.Passed;
        ASRS_Module1_UI.SetText(counterText, "Result");
        ASRS_Module1_UI.SetText(questionText, $"You scored {session.Score} out of {questions.Length}.");
        ASRS_Module1_UI.SetText(feedbackText, passed
            ? $"<color=#66FF88>Passed</color> — {passMark} or more correct. Press Continue to finish Module 1."
            : $"<color=#FF8866>Not passed</color> — you need at least {passMark} correct. Review the lesson panel and try again.");

        ASRS_Module1_UI.SetVisible(continueButton, passed);
        ASRS_Module1_UI.SetVisible(retryButton, !passed);
        ASRS_Module1_Log.Info(LogTag, $"Attempt finished: {session.Score}/{questions.Length} — {(passed ? "PASS" : "FAIL")}.", this);
    }

    private void OnContinuePressed()
    {
        if (!IsShowing || session == null || !session.Passed || completionReported)
            return;

        completionReported = true;
        CompleteStep(quizActionId);
    }
}
