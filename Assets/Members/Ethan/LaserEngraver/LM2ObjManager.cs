using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objective 1: Move object to zone")]
    public Transform targetObject;
    public Vector3 zoneCenter;
    public float zoneRadius = 1f;
    public Color zoneColor = new Color(0f, 1f, 0.4f, 0.35f);

    [Header("Objective 2: Height check")]
    public Transform checkedObject;
    public float requiredY = 2f;
    public float tolerance = 0.1f;
    public InputActionReference confirmAction;

    int stage = 0;
    GameObject marker;

    void Start()
    {
        marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "ObjectiveZone";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.position = zoneCenter;
        marker.transform.localScale = new Vector3(zoneRadius * 2f, 0.01f, zoneRadius * 2f);

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = zoneColor;
        marker.GetComponent<MeshRenderer>().material = mat;
    }

    void OnEnable()
    {
        if (confirmAction != null)
        {
            confirmAction.action.Enable();
            confirmAction.action.performed += OnConfirm;
        }
    }

    void OnDisable()
    {
        if (confirmAction != null)
            confirmAction.action.performed -= OnConfirm;
    }

    void Update()
    {
        if (stage != 0) return;

        Vector3 a = targetObject.position;
        Vector3 b = zoneCenter;
        a.y = b.y = 0f;
        if (Vector3.Distance(a, b) <= zoneRadius)
        {
            Debug.Log("Objective 1 complete");
            marker.SetActive(false);
            stage = 1;
        }
    }

    void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (stage != 1) return;

        if (Mathf.Abs(checkedObject.position.y - requiredY) <= tolerance)
        {
            Debug.Log("Objective 2 complete");
            stage = 2;
        }
        else
        {
            Debug.Log("Not right yet: y = " + checkedObject.position.y);
        }
    }
}