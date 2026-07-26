using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objective 1: Move object to zone")]
    public Transform targetObject;
    public Vector3 zoneCenter;
    public float zoneRadius = 1f;
    public float visualHeight = 0.02f;
    public float detectionHeight = 3f;
    public Color zoneColor = new Color(0f, 1f, 0.4f, 0.35f);

    [Header("Objective 2: Height check")]
    public Transform checkedObject;
    public float requiredY = 2f;
    public float tolerance = 0.1f;

    const int IDLE = -1;
    int stage = IDLE;
    GameObject marker;

    void Start()
    {
        marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "ObjectiveZone";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.position = zoneCenter + Vector3.up * (visualHeight * 0.5f);
        marker.transform.localScale = new Vector3(zoneRadius * 2f, visualHeight * 0.5f, zoneRadius * 2f);

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = zoneColor;
        marker.GetComponent<MeshRenderer>().material = mat;

        marker.SetActive(false);
    }

    public void StartObjectives()
    {
        stage = 0;
        if (marker != null) marker.SetActive(true);
    }

    public void ResetObjectives()
    {
        stage = IDLE;
        if (marker != null) marker.SetActive(false);
    }

    void Update()
    {
        if (stage != 0) return;

        Vector3 p = targetObject.position;
        float dy = p.y - zoneCenter.y;
        if (dy < 0f || dy > detectionHeight) return;

        Vector2 a = new Vector2(p.x, p.z);
        Vector2 b = new Vector2(zoneCenter.x, zoneCenter.z);
        if (Vector2.Distance(a, b) <= zoneRadius)
        {
            Debug.Log("Objective 1 complete");
            marker.SetActive(false);
            stage = 1;
        }
    }

    public void ConfirmObjective()
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

        ResetObjectives();
    }
}