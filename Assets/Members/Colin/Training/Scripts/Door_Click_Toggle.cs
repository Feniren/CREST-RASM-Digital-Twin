using UnityEngine;

public class Door_Click_Toggle : MonoBehaviour{
    [SerializeField] private Component_Marker DoorMarker;
    [SerializeField] private Item_Mill_Doors Doors;
    [SerializeField] private Lesson_Sequencer Sequencer;

    private void OnEnable(){
        DoorMarker.Selected += On_Door_Selected;
    }

    private void OnDisable(){
        DoorMarker.Selected -= On_Door_Selected;
    }

    // Toggle only when no selection step is pending, so quiz answers and
    // guided selects are never swallowed by the door.
    public void On_Door_Selected(Component_Marker marker){
        Lesson_Step step = Sequencer.Current_Step;

        if (step == null || step.Kind == Lesson_Step_Kind.Info)
            Doors.AlternateInteract(null);
    }
}
