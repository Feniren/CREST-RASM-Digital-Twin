using UnityEngine;

public class ObjectiveOrderHandler : MonoBehaviour
{
    public int WaypointRuns = 5;
    public int PoseRuns = 5;
    public int PickPlaceRuns = 5;
    public WaypointSpawner WaypointHandler;
    public RandomJointPoseProvider PoseProvider;
    public GameObject PoseGhost;
    public GameObject PickPlaceObject;
    public PickAndPlaceHandler PickPlaceHandler;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartWaypoint();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartWaypoint()
    {
        if (WaypointRuns > 0)
        {
            WaypointHandler.SpawnWaypoint();
        }
        else
        {
            FirstPose();
        }
    }

    public void WaypointHit()
    {
        WaypointRuns--;
        StartWaypoint();
    }

    void FirstPose()
    {
        PoseGhost.SetActive(true);
        Invoke(nameof(StartPose), 0.01f);
    }

    void StartPose()
    {
        if (PoseRuns > 0)
        {
            PoseProvider.Trigger();
        }
        else
        {
            FirstPickPlace();
        }
    }

    public void PoseHit()
    {
        PoseRuns--;
        StartPose();
    }

    void FirstPickPlace()
    {
        PoseGhost.SetActive(false);
        PickPlaceObject.SetActive(true);
    }

    void StartPickPlace()
    {
        if (PickPlaceRuns > 0)
        {
            PickPlaceHandler.SpawnBlock();
        }
        else
        {
            Debug.Log("Done!");
        }
    }

    public void PickPlaceHit()
    {
        PickPlaceRuns--;
        StartPickPlace();
    }
}