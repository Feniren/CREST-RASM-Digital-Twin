using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Wrist_HUD : MonoBehaviour{
    [SerializeField] private Lesson_Controller Controller;
    [SerializeField] private TMP_Text ProgressText;
    [SerializeField] private TMP_Text TimerText;
    [SerializeField] private Button ResetButton;
    [SerializeField] private Button ToggleButton;
    [SerializeField] private GameObject TogglePanel;

    private string lastProgress;
    private string lastTimer;

    private void Awake(){
        ResetButton.onClick.AddListener(Controller.Restart_Phase);
        ToggleButton.onClick.AddListener(() => TogglePanel.SetActive(!TogglePanel.activeSelf));
    }

    private void Update(){
        string progress = Controller.Module_Active ? $"{Controller.Step_Index + 1} / {Controller.Step_Count}" : "--";

        if (progress != lastProgress){
            lastProgress = progress;
            ProgressText.text = progress;
        }

        int seconds = (int)Controller.Elapsed_Time;
        string timer = $"{seconds / 60:00}:{seconds % 60:00}";

        if (timer != lastTimer){
            lastTimer = timer;
            TimerText.text = timer;
        }
    }
}
