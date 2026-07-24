using UnityEngine;
using UnityEngine.InputSystem;

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

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) { dir = 0f; return; }

        dir = 0f;
        if (kb.upArrowKey.isPressed) dir = 1f;
        else if (kb.downArrowKey.isPressed) dir = -1f;
    }

    void FixedUpdate()
    {
        if (dir == 0f) return;

        Vector3 p = rb.position;
        p.y = Mathf.Clamp(p.y + dir * speed * Time.fixedDeltaTime, baseY + minY, baseY + maxY);
        rb.MovePosition(p);
    }
}