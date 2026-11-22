using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Module 1's check, shown on the "M1_QuizPassed" step (the last step, so it
// can't stand in for the physical loading tasks before it).
//
// Questions is a POOL. Each attempt (and every Retry) draws a fresh set:
// Multiple Choice Per Attempt questions picked at random from the pool's
// multiple-choice questions, in random order and with their answers
// shuffled, then Cell Questions Per Attempt rack-cell questions, always
// last. With the defaults that's 3 random of 6 + 1 cell question = 4.
//
// One question at a time; each answer is locked in, explained, and scored;
// Pass Mark correct passes. A pass enables Continue, which reports the step
// complete exactly once; a fail offers Retry.
//
// A rack-cell question is answered by selecting a cell on the real rack
// (through ASRS_Rack_Cell_Inspector). It names a material — picked at random
// from what the rack actually stores, or the one in Cell Table Id — and
// accepts any cell holding that same material.
//
// Separate from Quiz_Manager (the "replay the steps in order" quiz used by
// Module 2), which this neither uses nor changes.
public class ASRS_Module1_Quiz : ASRS_Module1_Step_Feature
{
    [Serializable]
    public class Question
    {
        [Tooltip("For a rack-cell question, {material} is replaced with the material the trainee must find.")]
        [TextArea(2, 4)] public string prompt;
        public string[] options = new string[0];
        public int correctOption;
        [TextArea(2, 4)] public string explanation;

        [Header("Rack-cell question")]
        public bool rackCellQuestion;
        [Tooltip("The cell whose material is asked for — used when Random Cell Material is off, or as a fallback.")]
        public string cellTableId;
    }

    [Header("Step")]
    [SerializeField] private string quizActionId = "M1_QuizPassed";

    [Header("Questions (pool)")]
    [SerializeField] private Question[] questions = new Question[0];
    [Tooltip("Correct answers needed to pass one attempt (capped at the number of questions asked).")]
    [SerializeField, Min(1)] private int passMark = 2;
    [SerializeField] private ASRS_Rack_Cell_Inspector cellInspector;

    [Header("Randomising")]
    [Tooltip("Multiple-choice questions asked per attempt, drawn at random from the pool.")]
    [SerializeField, Min(0)] private int multipleChoicePerAttempt = 3;
    [Tooltip("Rack-cell questions asked per attempt — always asked last.")]
    [SerializeField, Min(0)] private int cellQuestionsPerAttempt = 1;
    [Tooltip("Shuffle the order of each multiple-choice question's answers.")]
    [SerializeField] private bool shuffleOptions = true;
    [Tooltip("Rack-cell questions ask for a material picked at random from what the rack stores, instead of always the one in Cell Table Id.")]
    [SerializeField] private bool randomCellMaterial = true;

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
    private readonly System.Random rng = new System.Random();
    private ASRS_Quiz_Session session;
    private bool completionReported;
    private string cellMaterial;

    private int[] drawn = new int[0];       // pool indices asked this attempt, in order
    private int[] optionOrder = new int[0]; // button i shows the question's option optionOrder[i]

    protected override string LogTag => ASRS_Module1_Log.Quiz;

    public string QuizActionId => quizActionId;
    public Question[] Questions => questions;
    public int PassMark => passMark;
    public int MultipleChoicePerAttempt => multipleChoicePerAttempt;
    public int CellQuestionsPerAttempt => cellQuestionsPerAttempt;
    public ASRS_Rack_Cell_Inspector CellInspector => cellInspector;
    public Button[] OptionButtons => optionButtons;

    private int AskedCount => drawn.Length;
    private int EffectivePassMark => Mathf.Min(passMark, Mathf.Max(1, AskedCount));
    private Question Current => questions[drawn[session.CurrentIndex]];

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

    // Fresh attempt: a newly drawn set of questions, score and buttons reset.
    public void BeginAttempt()
    {
        if (questions == null || questions.Length == 0)
        {
            ASRS_Module1_Log.Error(LogTag, "No questions configured — run 'ASRS/Module 1/Update Quiz (6 Questions, Random 3 + Cell Question)'.", this);
            return;
        }

        bool[] isCell = new bool[questions.Length];
        for (int i = 0; i < questions.Length; i++)
            isCell[i] = questions[i] != null && questions[i].rackCellQuestion;

        drawn = ASRS_Quiz_Draw.Draw(isCell, multipleChoicePerAttempt, cellQuestionsPerAttempt, rng);
        if (drawn.Length == 0)
        {
            ASRS_Module1_Log.Error(LogTag, "The question pool has nothing to ask with the current per-attempt settings.", this);
            return;
        }

        session = new ASRS_Quiz_Session(drawn.Length, EffectivePassMark);
        ASRS_Module1_Log.Info(LogTag, $"Attempt started — asking pool questions [{string.Join(", ", Array.ConvertAll(drawn, i => (i + 1).ToString()))}] of {questions.Length}, pass mark {EffectivePassMark}/{drawn.Length}.", this);
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        Question q = Current;

        ASRS_Module1_UI.SetText(counterText, $"Question {session.CurrentIndex + 1} of {AskedCount}");
        ASRS_Module1_UI.SetText(feedbackText, "");
        ASRS_Module1_UI.SetVisible(nextButton, false);
        ASRS_Module1_UI.SetVisible(retryButton, false);
        ASRS_Module1_UI.SetVisible(continueButton, false);

        if (q.rackCellQuestion)
        {
            optionOrder = new int[0];
            ShowOptions();
            cellMaterial = PickCellMaterial(q);

            if (cellMaterial != null)
            {
                ASRS_Module1_UI.SetText(questionText, q.prompt.Replace("{material}", cellMaterial));
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

        List<int> used = new List<int>();
        for (int i = 0; i < q.options.Length; i++)
            if (!string.IsNullOrEmpty(q.options[i]))
                used.Add(i);

        optionOrder = shuffleOptions ? ASRS_Quiz_Draw.Shuffled(used, rng) : used.ToArray();

        ASRS_Module1_UI.SetText(questionText, q.prompt);
        ShowOptions();
    }

    // A material the rack really stores: a random one of the distinct
    // materials (so a rare material is as likely as the common one), or the
    // one in Cell Table Id. Null if neither can be resolved.
    private string PickCellMaterial(Question q)
    {
        if (cellInspector == null)
            return null;

        if (randomCellMaterial)
        {
            List<string> materials = new List<string>();
            foreach (string id in ASRS_Rack_Addressing.AllTableIds())
            {
                string material = cellInspector.GetCellInfo(id).Material;
                if (!string.IsNullOrEmpty(material) && !materials.Exists(m => ASRS_Rack_Cell_Inspector.SameMaterial(m, material)))
                    materials.Add(material);
            }

            if (materials.Count > 0)
                return materials[rng.Next(materials.Count)];
        }

        return cellInspector.TryResolveMaterial(q.cellTableId, out string resolved, out _) ? resolved : null;
    }

    // Button i shows option optionOrder[i]; buttons past that are hidden.
    private void ShowOptions()
    {
        Question q = session != null && !session.IsFinished ? Current : null;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool used = q != null && i < optionOrder.Length;
            ASRS_Module1_UI.SetVisible(optionButtons[i], used);
            if (used)
                SetOptionLabel(i, q.options[optionOrder[i]], null);
        }
    }

    // Widget_Button repaints its background on every hover change, so the
    // verdict goes on the label instead of the button colour.
    private void SetOptionLabel(int button, string text, string verdict)
    {
        if (button >= optionLabels.Length || optionLabels[button] == null)
            return;

        string label = $"{(char)('A' + button)}.  {text}";
        optionLabels[button].text = verdict == null ? label : $"{label}   <b>{verdict}</b>";
    }

    private void OnOptionPressed(int button)
    {
        if (!IsShowing || session == null || session.IsFinished || session.CurrentAnswered)
            return;

        Question q = Current;
        if (q.rackCellQuestion || button >= optionOrder.Length)
            return;

        int chosen = optionOrder[button];
        int correctButton = Array.IndexOf(optionOrder, q.correctOption);
        bool correct = chosen == q.correctOption;

        SetOptionLabel(button, q.options[chosen], correct ? "<color=#66FF88>— correct</color>" : "<color=#FF8866>— your answer</color>");
        if (!correct && correctButton >= 0)
            SetOptionLabel(correctButton, q.options[q.correctOption], "<color=#66FF88>— correct answer</color>");

        string verdict = correct
            ? "<color=#66FF88>Correct.</color>"
            : correctButton >= 0
                ? $"<color=#FF8866>Not quite — the answer is {(char)('A' + correctButton)}.</color>"
                : "<color=#FF8866>Not quite.</color>";
        Answer(correct, $"{verdict} {q.explanation}");
    }

    private void OnCellPicked(ASRS_Cell_Info info)
    {
        if (!IsShowing || session == null || session.IsFinished || session.CurrentAnswered)
            return;

        Question q = Current;
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

        ASRS_Module1_Log.Info(LogTag, $"Q{session.CurrentIndex + 1} (pool #{drawn[session.CurrentIndex] + 1}): {(correct ? "correct" : "wrong")} (score {session.Score}/{AskedCount}).", this);
        ASRS_Module1_UI.SetText(feedbackText, feedback);
        ASRS_Module1_UI.SetText(nextLabel, session.CurrentIndex + 1 < AskedCount ? "Next question  ›" : "See result  ›");
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
        optionOrder = new int[0];
        ShowOptions();
        ASRS_Module1_UI.SetVisible(nextButton, false);

        bool passed = session.Passed;
        ASRS_Module1_UI.SetText(counterText, "Result");
        ASRS_Module1_UI.SetText(questionText, $"You scored {session.Score} out of {AskedCount}.");
        ASRS_Module1_UI.SetText(feedbackText, passed
            ? $"<color=#66FF88>Passed</color> — {EffectivePassMark} or more correct. Press Continue to finish Module 1."
            : $"<color=#FF8866>Not passed</color> — you need at least {EffectivePassMark} correct. Review the lesson panel and try again — Retry asks a new set of questions.");

        ASRS_Module1_UI.SetVisible(continueButton, passed);
        ASRS_Module1_UI.SetVisible(retryButton, !passed);
        ASRS_Module1_Log.Info(LogTag, $"Attempt finished: {session.Score}/{AskedCount} — {(passed ? "PASS" : "FAIL")}.", this);
    }

    private void OnContinuePressed()
    {
        if (!IsShowing || session == null || !session.Passed || completionReported)
            return;

        completionReported = true;
        CompleteStep(quizActionId);
    }
}
