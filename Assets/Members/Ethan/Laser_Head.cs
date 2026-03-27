using UnityEngine;

public class Laser_Head : MonoBehaviour
{
    public float intensity = 100f; // how quickly objects are engraved

    void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
        {
            e_Item_Epoxy_Block epoxy_block = hit.collider.GetComponent<e_Item_Epoxy_Block>();
            if (epoxy_block != null)
            {
                Debug.Log("Thing happening...");
                Debug.Log($"textureCoord: {hit.textureCoord}, triangleIndex: {hit.triangleIndex}");
                int x = Mathf.FloorToInt(hit.textureCoord.x * epoxy_block.Atlas.width);
                int y = Mathf.FloorToInt(hit.textureCoord.y * epoxy_block.Atlas.height);
                epoxy_block.PaintAt(x, y, intensity * Time.deltaTime);
            }
        }
    }
}