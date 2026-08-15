using UnityEngine;

public class Laser_Tender_Arm : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Leave this blank to auto-find 'Bone.007/Bone.007_end', or drag the bone here in the Inspector.")]
    public Transform attachmentPoint;

    private Item_Epoxy_Block currentlyHeldItem;

    void Start()
    {
        if (attachmentPoint == null)
        {
            attachmentPoint = transform.Find("Bone.007/Bone.007_end");

            if (attachmentPoint == null)
            {
                Debug.LogWarning("Attachment point 'Bone.007/Bone.007_end' not found!");
            }
        }

        Grab();
        Invoke("Drop", 2.0f);
    }

    // Finds the nearest Item_Epoxy_Block, teleports it to the attachment point, and parents it.
    public void Grab()
    {
        if (currentlyHeldItem != null || attachmentPoint == null) return;

        Item_Epoxy_Block[] allBlocks = FindObjectsOfType<Item_Epoxy_Block>();
        if (allBlocks.Length == 0) return;

        Item_Epoxy_Block nearestBlock = null;
        float shortestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Item_Epoxy_Block block in allBlocks)
        {
            float distance = Vector3.Distance(currentPosition, block.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestBlock = block;
            }
        }

        if (nearestBlock != null)
        {
            currentlyHeldItem = nearestBlock;

            Rigidbody rb = currentlyHeldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            currentlyHeldItem.transform.position = attachmentPoint.position;
            currentlyHeldItem.transform.rotation = attachmentPoint.rotation;

            currentlyHeldItem.transform.SetParent(attachmentPoint);
        }
    }

    // Unparents the held object, dropping it into the world.
    public void Drop()
    {
        if (currentlyHeldItem != null)
        {
            currentlyHeldItem.transform.SetParent(null);

            Rigidbody rb = currentlyHeldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            currentlyHeldItem = null;
        }
    }

    // TODO: Return to conveyor
}