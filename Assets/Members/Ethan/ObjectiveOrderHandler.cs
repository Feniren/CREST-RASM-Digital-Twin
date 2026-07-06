using UnityEngine;
using TMPro;

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
    public TextMeshProUGUI TextMesh;
    public AudioSource goalSound;

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
        goalSound.Play();
        WaypointRuns--;
        StartWaypoint();
    }

    void FirstPose()
    {
        TextMesh.text = "Now, move the arm to match the hologram's poses.";
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
        goalSound.Play();
        PoseRuns--;
        StartPose();
    }

    void FirstPickPlace()
    {
        TextMesh.text = $"Now, pick up the block from the left platform and move it to the right platform 5 times.\n\n\nSince this task has consistent conditions, try using Record and Playback to complete it.";
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
        goalSound.Play();
        PickPlaceRuns--;
        StartPickPlace();
    }
}