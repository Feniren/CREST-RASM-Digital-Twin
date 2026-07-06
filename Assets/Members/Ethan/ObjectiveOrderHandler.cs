using UnityEngine;

public class ObjectiveOrderHandler : MonoBehaviour
{
    public int WaypointRuns = 5;
    public int PoseRuns = 5;
    public WaypointSpawner WaypointHandler;
    public RandomJointPoseProvider PoseProvider;
    public GameObject PoseGhost;
    public GameObject PickPlaceHandler;

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
        WaypointHandler.SpawnWaypoint();
    }

    public void WaypointHit()
    {
        WaypointRuns--;
        if (WaypointRuns > 0)
        {
            StartWaypoint();
        }
        else
        {
            FirstPose();
        }
    }

    void FirstPose()
    {
        PoseGhost.SetActive(true);
        Invoke(nameof(StartPose), 0.01f);
    }

    void StartPose()
    {
        PoseProvider.Trigger();
    }

    public void PoseHit()
    {
        PoseRuns--;
        if (PoseRuns > 0)
        {
            StartPose();
        }
        else
        {
            FirstPickPlace();
        }
    }

    void FirstPickPlace()
    {
        PoseGhost.SetActive(false);
        PickPlaceHandler.SetActive(true);
    }
}