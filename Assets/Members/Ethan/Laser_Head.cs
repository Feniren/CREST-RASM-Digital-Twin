using UnityEngine;

public class Laser_Head : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        Debug.DrawRay(transform.position, Vector3.down * 100f, Color.red);
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
        {
            // hit.point, hit.normal, hit.collider, hit.distance available here
            Debug.Log(hit.collider.name);
        }

    }
}
