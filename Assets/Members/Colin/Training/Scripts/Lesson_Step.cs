using UnityEngine;

// Serialized as an int in Lesson_Definition assets — only ever APPEND new kinds at
// the end. Reordering shifts existing assets' Kind values (e.g. M1_Lesson.asset).
public enum Lesson_Step_Kind { Info, Select_Component, Axis_Demo, Milling_Demo, Panel_Action }

public enum Lesson_Mode { Guided, Practice }

[System.Serializable]
public class Lesson_Step{
    public string Step_Id;
    public Lesson_Step_Kind Kind;
    [TextArea] public string Prompt_Text;
    [TextArea] public string Practice_Prompt_Text;
    public string Target_Marker_Id; // also the Action_Id for Panel_Action steps
    public AudioClip Narration;
    public bool Include_In_Quiz;
}

[System.Serializable]
public class Lesson_Result{
    public string Module_Id;
    public Lesson_Mode Mode;
    public int Correct_Answers;
    public int Quiz_Total;
    public int Errors;
    public float Elapsed_Seconds;
}
