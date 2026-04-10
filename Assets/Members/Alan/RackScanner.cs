using System.Collections;
using UnityEngine;

public enum RACK_TASK
{
	RETRIEVE , INSERT
}

public class RackScanner : MonoBehaviour
{
    [SerializeField] private Item_ASRS rack;

    [SerializeField] private float resumeDelay = 1f;

    // BLOCK OPERATIONS TESTER
    //public void OnTriggerEnter(Collider other)
    //{
    //    if (rack == null)
    //    {
    //        Debug.LogError("Rack reference is not assigned.");
    //        return;
    //    }

    //    Item_Slotted_Table slottedTable = other.GetComponent<Item_Slotted_Table>();
    //    if (slottedTable == null) return;

    //    if (slottedTable.task == RACK_TASK.RETRIEVE && slottedTable.Item != null)
    //    {
    //        Debug.Log("Table " + slottedTable.TableID + " already has a block, skipping retrieve.");
    //        return;
    //    }

    //    if (slottedTable.task == RACK_TASK.INSERT && slottedTable.Item == null)
    //    {
    //        Debug.Log("Table " + slottedTable.TableID + " has no block to insert, skipping.");
    //        return;
    //    }

    //    Spline_Animate spline = other.GetComponent<Spline_Animate>();
    //    if (spline == null) return;

    //    Debug.Log("Block operation - Collision detected with Item_Slotted_Table. Table ID: " + slottedTable.TableID);

    //    spline.Pause();
    //    Debug.Log("Spline paused.");

    //    if (slottedTable.task == RACK_TASK.RETRIEVE)
    //        rack.BlockRetrieve(slottedTable);

    //    if (slottedTable.task == RACK_TASK.INSERT)
    //        rack.BlockInsert(slottedTable);

    //    StartCoroutine(ResumeAfterDelay(spline));
    //}
    public void OnTriggerEnter(Collider other)
    {
        if (rack == null)
        {
            Debug.LogError("Rack reference is not assigned.");
            return;
        }

        Item_Slotted_Table slottedTable = other.GetComponent<Item_Slotted_Table>();
        if (slottedTable == null) return;

        if (slottedTable.task == RACK_TASK.RETRIEVE && !rack.NeedsRackReturn(slottedTable))
        {
            Debug.Log("Table " + slottedTable.TableID + " does not need retrieval, skipping.");
            return;
        }

        if (slottedTable.task == RACK_TASK.INSERT && !rack.NeedsRackReturn(slottedTable))
        {
            Debug.Log("Table " + slottedTable.TableID + " is marked to return to rack, skipping insert.");
            return;
        }

        Spline_Animate spline = other.GetComponent<Spline_Animate>();
        if (spline == null) return;

        Debug.Log("Block operation - Collision detected with Item_Slotted_Table. Table ID: " + slottedTable.TableID);
        spline.Pause();
        Debug.Log("Spline paused.");

        //if (slottedTable.task == RACK_TASK.RETRIEVE)
        //    rack.BlockRetrieve(slottedTable);
        //if (slottedTable.task == RACK_TASK.INSERT)
        //    rack.BlockInsert(slottedTable);

        StartCoroutine(ResumeAfterDelay(spline));
    }

    private IEnumerator ResumeAfterDelay(Spline_Animate spline)
    {
        yield return new WaitForSeconds(resumeDelay);
        spline.Play();
        Debug.Log("Spline resumed.");
    }
}
