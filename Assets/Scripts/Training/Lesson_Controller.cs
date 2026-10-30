using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lesson_Controller : MonoBehaviour, Save_Data_Interface{
    // One menu panel per machine (Mill, Lathe): its modules, where their buttons
    // go, and its own progress text.
    [System.Serializable]
    private class Menu_Panel{
        public List<Lesson_Definition> Modules;
        public Transform ButtonContainer;
        public TMP_Text StatusText;
    }

    [SerializeField] private Data_Loader DataLoader;
    [SerializeField] private Module_Loader ModuleLoader;
    [SerializeField] private List<Menu_Panel> Panels;
    [SerializeField] private Button ModuleButtonPrefab;

    public bool Module_Active { get; private set; }
    public int Step_Index { get; private set; }
    public int Step_Count { get; private set; }
    public Lesson_Mode Current_Mode { get; private set; }
    public float Elapsed_Time => Module_Active ? Time.time - moduleStartTime : 0f;

    private Serialized_Dictionary<string, int> moduleProgress = new Serialized_Dictionary<string, int>();
    private Serialized_Dictionary<string, int> quizScores = new Serialized_Dictionary<string, int>();
    // Every panel's modules in panel order — the index space of Start_Module.
    private readonly List<Lesson_Definition> availableModules = new List<Lesson_Definition>();
    private Lesson_Definition activeDefinition;
    private Lesson_Sequencer activeSequencer;
    private float moduleStartTime;

    private void Awake(){
        foreach (Menu_Panel panel in Panels)
            availableModules.AddRange(panel.Modules);

        ModuleLoader.Module_Loaded += OnModuleLoaded;
        ModuleLoader.Module_Unloaded += OnModuleUnloaded;
    }

    private void Start(){
        BuildModuleButtons();
        UpdateMenuStatus();
    }

    public void Start_Module(int index){
        if (Module_Active || index < 0 || index >= availableModules.Count)
            return;

        activeDefinition = availableModules[index];
        ModuleLoader.Load_Module(activeDefinition.Scene_Name);
    }

    // Called by Training_Play_Redirect after a module-scene Play redirect.
    public bool Start_Module_By_Scene(string sceneName){
        for (int i = 0; i < availableModules.Count; i++)
            if (availableModules[i] != null && availableModules[i].Scene_Name == sceneName){
                Start_Module(i);
                return true;
            }

        return false;
    }

    public void Restart_Phase(){
        if (activeSequencer != null)
            activeSequencer.Begin(activeDefinition, Current_Mode);
    }

    public void LoadData(Save_Data SaveData){
        /*if (SaveData.Module_Progress != null)
            moduleProgress = SaveData.Module_Progress;

        if (SaveData.Quiz_Scores != null)
            quizScores = SaveData.Quiz_Scores;*/

        UpdateMenuStatus();
    }

    public void SaveData(ref Save_Data SaveData){
        /*SaveData.Module_Progress = moduleProgress;
        SaveData.Quiz_Scores = quizScores;*/
    }

    private void OnModuleLoaded(Scene scene){
        activeSequencer = null;

        foreach (GameObject root in scene.GetRootGameObjects()){
            activeSequencer = root.GetComponentInChildren<Lesson_Sequencer>(true);

            if (activeSequencer != null)
                break;
        }

        if (activeSequencer == null){
            Debug.LogError($"Lesson_Controller: no Lesson_Sequencer found in scene '{scene.name}'");
            return;
        }

        activeSequencer.Step_Changed += OnStepChanged;
        activeSequencer.Lesson_Completed += OnLessonCompleted;
        activeSequencer.RetryButton.onClick.AddListener(RetryPractice);
        activeSequencer.ReturnButton.onClick.AddListener(ModuleLoader.Return_To_Menu);

        Module_Active = true;
        moduleStartTime = Time.time;
        Current_Mode = Lesson_Mode.Guided;

        if (GetProgress(activeDefinition.Module_Id) < 1)
            moduleProgress[activeDefinition.Module_Id] = 1;

        activeSequencer.Begin(activeDefinition, Lesson_Mode.Guided);
    }

    private void OnModuleUnloaded(){
        if (activeSequencer != null){
            activeSequencer.Step_Changed -= OnStepChanged;
            activeSequencer.Lesson_Completed -= OnLessonCompleted;
        }

        activeSequencer = null;
        activeDefinition = null;
        Module_Active = false;
        UpdateMenuStatus();
    }

    private void OnStepChanged(Lesson_Step step, int index, int count){
        Step_Index = index;
        Step_Count = count;
        Current_Mode = activeSequencer.Mode;
    }

    private void OnLessonCompleted(Lesson_Result result){
        if (result.Mode == Lesson_Mode.Guided){
            Current_Mode = Lesson_Mode.Practice;
            activeSequencer.Begin(activeDefinition, Lesson_Mode.Practice);
            return;
        }

        bool passed = result.Correct_Answers >= activeDefinition.Quiz_Pass_Threshold;

        if (result.Correct_Answers > GetScore(activeDefinition.Module_Id))
            quizScores[activeDefinition.Module_Id] = result.Correct_Answers;

        if (passed)
            moduleProgress[activeDefinition.Module_Id] = 2;

        DataLoader.SaveGame();
        activeSequencer.Show_Results(result, passed);
        UpdateMenuStatus();
    }

    private void RetryPractice(){
        if (activeSequencer != null)
            activeSequencer.Begin(activeDefinition, Lesson_Mode.Practice);
    }

    private int GetProgress(string moduleId){
        return moduleProgress.TryGetValue(moduleId, out int value) ? value : 0;
    }

    private int GetScore(string moduleId){
        return quizScores.TryGetValue(moduleId, out int value) ? value : 0;
    }

    // Menu buttons are generated from each panel's Modules — adding a module to
    // the right panel's list is the only step needed to put it on the menu.
    private void BuildModuleButtons(){
        if (ModuleButtonPrefab == null){
            Debug.LogError("Lesson_Controller: ModuleButtonPrefab not assigned — menu will be empty.");
            return;
        }

        int index = 0;

        foreach (Menu_Panel panel in Panels){
            if (panel.ButtonContainer == null)
                Debug.LogError("Lesson_Controller: a menu panel has no ButtonContainer — its modules won't appear.");

            foreach (Lesson_Definition def in panel.Modules){
                int moduleIndex = index++;

                if (def == null || panel.ButtonContainer == null)
                    continue;

                Button button = Instantiate(ModuleButtonPrefab, panel.ButtonContainer);
                button.gameObject.name = def.Module_Id + "_Button";
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();

                if (label != null)
                    label.text = def.Display_Name;

                button.onClick.AddListener(() => Start_Module(moduleIndex));
            }
        }
    }

    private void UpdateMenuStatus(){
        foreach (Menu_Panel panel in Panels){
            if (panel.StatusText == null || panel.Modules.Count == 0)
                continue;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (Lesson_Definition def in panel.Modules){
                string moduleId = def.Module_Id;
                int progress = GetProgress(moduleId);
                int total = def.Steps.FindAll(s => s.Include_In_Quiz).Count;

                if (sb.Length > 0)
                    sb.Append('\n');

                if (progress == 2)
                    sb.Append($"{moduleId} complete — best score {GetScore(moduleId)} / {total}");
                else if (progress == 1)
                    sb.Append($"{moduleId} started — best score {GetScore(moduleId)} / {total}");
                else
                    sb.Append($"{moduleId} not started");
            }

            panel.StatusText.text = sb.ToString();
        }
    }
}
