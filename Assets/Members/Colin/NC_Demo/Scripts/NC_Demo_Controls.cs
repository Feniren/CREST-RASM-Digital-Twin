using UnityEngine;

/// <summary>
/// Demo-only keyboard control (bindings live in NC_Demo_Input.inputactions): Pause, Skip,
/// Restart, Feed_Down/Feed_Up halve and double the override, View_1..3 jump the camera to
/// authored views. Nothing here is needed by the mill station itself.
/// </summary>
public class NC_Demo_Controls : MonoBehaviour
{
    [SerializeField] private NC_Job_Controller Job;
    [SerializeField] private NC_Runner Runner;

    [Header("Views")]
    [SerializeField] private Camera View_Camera;
    [SerializeField] private Transform[] Views;

    NC_Demo_Input input;
    int view;

    void Awake()
    {
        if (Job == null || Runner == null)
        {
            Debug.LogWarning("NC_Demo_Controls: Job and Runner must be assigned.");
            enabled = false;
        }
    }

    // Created here rather than in Awake: a script reload during Play empties non-serialized
    // fields and re-runs OnEnable, not Awake.
    void OnEnable()
    {
        if (input == null) input = new NC_Demo_Input();
        input.Demo.Enable();
    }

    void OnDisable()
    {
        input?.Demo.Disable();
    }

    void OnDestroy()
    {
        input?.Dispose();
    }

    void Update()
    {
        var a = input.Demo;
        if (a.Pause.WasPressedThisFrame()) Runner.Toggle_Pause();
        if (a.Skip.WasPressedThisFrame()) Job.Skip();
        if (a.Restart.WasPressedThisFrame()) Job.Restart();
        if (a.Feed_Down.WasPressedThisFrame()) Runner.Feed_Override *= 0.5f;
        if (a.Feed_Up.WasPressedThisFrame()) Runner.Feed_Override *= 2f;
        if (a.View_1.WasPressedThisFrame()) Set_View(0);
        if (a.View_2.WasPressedThisFrame()) Set_View(1);
        if (a.View_3.WasPressedThisFrame()) Set_View(2);
    }

    public void Next_View()
    {
        if (Views == null || Views.Length == 0) return;
        Set_View((view + 1) % Views.Length);
    }

    public void Set_View(int i)
    {
        if (View_Camera == null || Views == null || i < 0 || i >= Views.Length || Views[i] == null) return;
        view = i;
        View_Camera.transform.SetPositionAndRotation(Views[i].position, Views[i].rotation);
    }
}
