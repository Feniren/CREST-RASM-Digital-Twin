using UnityEngine;

public class DigitalTwin_Lesson_Bootstrap : MonoBehaviour{
    [SerializeField] private Lesson_Sequencer Sequencer;
    [SerializeField] private Lesson_Definition Definition;

    private void Start(){
        Sequencer.Begin(Definition, Lesson_Mode.Guided);
    }
}
