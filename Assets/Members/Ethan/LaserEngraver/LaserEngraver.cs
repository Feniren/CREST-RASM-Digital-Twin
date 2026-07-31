using UnityEngine;

public class LaserEngraver : MonoBehaviour
{
    public Laser_Head laserHeadRef;
    public EngraveMask readyMask;

    void OnEnable(){
        laserHeadRef = FindFirstObjectByType<Laser_Head>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        readyMask = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DownloadMask(EngraveMask mask)
    {
        readyMask = mask;
        Debug.Log($"Downloaded print job: {mask.WidthInches} x {mask.HeightInches} inches at {mask.DPI} DPI");
    }

    public void StartJob()
    {
        // TEMP: Laser_Head manages application of job; in the future,
        //       it should only be in charge of applying the reveal mask.
        laserHeadRef.LoadMask(readyMask);
        laserHeadRef.TryApplyJob();
    }

}
