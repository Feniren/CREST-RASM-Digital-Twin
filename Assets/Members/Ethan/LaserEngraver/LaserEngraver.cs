using UnityEngine;

public class LaserEngraver : MonoBehaviour
{
    public PrintJob readyJob;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        readyJob = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DownloadJob(PrintJob job)
    {
        readyJob = job;
        Debug.Log($"Downloaded print job: {job.WidthInches} x {job.HeightInches} inches at {job.DPI} DPI");
    }

}
