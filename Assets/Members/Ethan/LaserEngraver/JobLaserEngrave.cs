using UnityEngine;

public class JobLaserEngrave : Job_Parent
{
    public LaserEngraver EngraverReference;

    public EngraveMask Image2Engrave;

    public JobLaserEngrave() {
        Name = "Engrave with Laser";
    }

    public void OnEnable(){
        EngraverReference = FindFirstObjectByType<LaserEngraver>();
    }

    public void SetImage(EngraveMask image){
        Image2Engrave = image;
    }

    public override void ExecuteJob() {
        EngraverReference.StartJob();
    }

    public void DownloadJob() {
        EngraverReference.DownloadMask(Image2Engrave);
    }
}
