using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Lesson_Definition", menuName = "Training/Lesson Definition")]
public class Lesson_Definition : ScriptableObject{
    public string Module_Id = "M1";
    public string Scene_Name;
    public string Display_Name;
    public List<Lesson_Step> Steps = new List<Lesson_Step>();
    public int Quiz_Pass_Threshold = 7;

    [Header("Practice-phase behavior (M1 default true; M2 sets it false)")]
    public bool Practice_Shuffles_Quiz = true;

    public List<Lesson_Step> Build_Practice_Steps(System.Random rng){
        List<Lesson_Step> quiz = Steps.FindAll(s => s.Include_In_Quiz);

        if (Practice_Shuffles_Quiz)
            for (int i = quiz.Count - 1; i > 0; i--){
                int j = rng.Next(i + 1);
                (quiz[i], quiz[j]) = (quiz[j], quiz[i]);
            }

        return quiz;
    }
}
