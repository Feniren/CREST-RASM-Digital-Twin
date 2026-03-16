using UnityEngine;

public enum RACK_TASK
{
	RETRIEVE , INSERT
}

public class RackScanner : MonoBehaviour
{
    [SerializeField] private Item_ASRS rack;

    public void OnTriggerEnter(Collider other)
    {
        if (rack == null)
        {
            Debug.LogError("Rack reference is not assigned.");
            return;
        }

        Item_Slotted_Table slotted_table = other.GetComponent<Item_Slotted_Table>();
        if (slotted_table == null) return;

        if (slotted_table.task == RACK_TASK.RETRIEVE)
            slotted_table.Item = rack.Retrieve(slotted_table.TableID);

        if (slotted_table.task == RACK_TASK.INSERT)
            rack.Insert(slotted_table);
        

        var spline = other.GetComponent<Spline_Animate>();
        if (spline == null) return;

        spline.Pause();
    }
}
