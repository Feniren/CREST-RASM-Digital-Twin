using UnityEngine;


public class RackScanner : MonoBehaviour
{
    Item_ASRS rack;
    
    public void OnTriggerEnter(Collider other)
    {
        Item_Slotted_Table slotted_table = other.GetComponent<Item_Slotted_Table>();
        if (slotted_table == null) return;


        // item slotted is missing a thing so that I can read off what instructions to do
        if (slotted_table.rack_task == rack_task.RETRIEVE)
            slotted_table.Item = rack.Retreive(slotted_table.TableID);

        if (slotted_table.rack_task == rack_task.INSERT)
            rack.Insert(slotted_table);

        var spline = other.GetComponent<Spline_Animate>();
        if (spline == null) return;

        spline.Pause();
    }
}
