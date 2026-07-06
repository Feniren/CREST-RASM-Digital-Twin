using UnityEngine;

public class ObjectiveOrderHandler : MonoBehaviour
{
    public int WaypointRuns = 5;
    public int PoseRuns = 5;
    public WaypointSpawner WaypointHandler;

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
            StartPose();
        }
    }

    void StartPose()
    {

    }

    public void PoseHit()
    {

    }
}
