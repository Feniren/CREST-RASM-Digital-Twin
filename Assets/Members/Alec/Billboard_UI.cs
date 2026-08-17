using Unity.Mathematics;
using UnityEngine;

public class Billboard_UI : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    private Vector3 originalRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalRotation = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        // transform.LookAt(camTransform);
        transform.forward = camTransform.forward;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, originalRotation.z);
    }
}
