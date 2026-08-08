using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    [TextArea]
    public string[] stageMessages = new string[]
    {
        "Set mode to manual",
        "Download print file",
        "Move gauge under laser carriage",
        "Set zero point",
        "Press RUN"
    };
    public string wrongHeightMessage = "Not at zero point yet";

    [Header("Objective 3: Move object to zone")]
    public Transform targetObject;
    public Vector3 zoneCenter;
    public float zoneRadius = 1f;
    public float visualHeight = 0.02f;
    public float detectionHeight = 3f;
    public Color zoneColor = new Color(0f, 1f, 0.4f, 0.35f);

    [Header("Objective 4: Height check (zero point)")]
    public Transform checkedObject;
    public float requiredY = 2f;
    public float tolerance = 0.1f;

    const int IDLE = -1;
    const int MANUAL_MODE = 0;
    const int DOWNLOAD_PRINT_FILE = 1;
    const int MOVE_TO_ZONE = 2;
    const int HEIGHT_CHECK = 3;
    const int RUN = 4;
    const int COMPLETE = 5;

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

        StartObjectives();
    }

    public void StartObjectives()
    {
        stage = MANUAL_MODE;
        if (marker != null) marker.SetActive(false);
        UpdateText();
    }

    public void ResetObjectives()
    {
        stage = IDLE;
        if (marker != null) marker.SetActive(false);
    }

    void UpdateText()
    {
        if (objectiveText == null) return;
        if (stage >= 0 && stage < stageMessages.Length)
            objectiveText.text = stageMessages[stage];
        else
            objectiveText.text = "";
    }

    public void ManualSet()
    {
        if (stage != MANUAL_MODE) return;
        Debug.Log("Objective 1 complete: manual mode set");
        stage = DOWNLOAD_PRINT_FILE;
        UpdateText();
    }

    public void PrintFileDownloaded()
    {
        if (stage != DOWNLOAD_PRINT_FILE) return;
        Debug.Log("Objective 2 complete: print file downloaded");
        stage = MOVE_TO_ZONE;
        if (marker != null) marker.SetActive(true);
        UpdateText();
    }

    void Update() // includes objective 3 logic
    {
        if (stage != MOVE_TO_ZONE) return;

        Vector3 p = targetObject.position;
        float dy = p.y - zoneCenter.y;
        if (dy < 0f || dy > detectionHeight) return;

        Vector2 a = new Vector2(p.x, p.z);
        Vector2 b = new Vector2(zoneCenter.x, zoneCenter.z);
        if (Vector2.Distance(a, b) <= zoneRadius)
        {
            Debug.Log("Objective 3 complete: object in zone");
            marker.SetActive(false);
            stage = HEIGHT_CHECK;
            UpdateText();
        }
    }

    public void ConfirmObjective()
    {
        if (stage != HEIGHT_CHECK) return;

        if (Mathf.Abs(checkedObject.position.y - requiredY) <= tolerance)
        {
            Debug.Log("Objective 4 complete: zero point confirmed");
            stage = RUN;
            UpdateText();
        }
        else
        {
            Debug.Log("Not right yet: y = " + checkedObject.position.y);
            if (objectiveText != null) objectiveText.text = wrongHeightMessage;
        }
    }

    public void RunTriggered()
    {
        if (stage != RUN) return;
        Debug.Log("Objective 5 complete: run started");
        stage = COMPLETE;
        UpdateText();
    }
}
