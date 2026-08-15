using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Tray : MonoBehaviour
{
    public float maxY = 0.5f;
    public float minY = 0f;
    public float speed = 0.5f;

    Rigidbody rb;
    float baseY;
    float dir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        baseY = rb.position.y;
    }


    public void Raise() => dir = 1f;  // for PointerDown on a VR button
    public void Lower() => dir = -1f; // this too

    public void StopMove() => dir = 0f; // for PointerUp

    void OnDisable() => dir = 0f;

    void FixedUpdate()
    {
        if (dir == 0f) return;

        Vector3 p = rb.position;
        p.y = Mathf.Clamp(p.y + dir * speed * Time.fixedDeltaTime, baseY + minY, baseY + maxY);
        rb.MovePosition(p);
    }
}