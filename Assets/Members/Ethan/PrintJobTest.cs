using System.IO;
using UnityEngine;

public class PrintJobTest : MonoBehaviour
{
	public Laser_Head laserHead;
	public string testImagePath = "Assets/Members/Ethan/TestImage.png";
	string projectRoot;
	string fullPath;
	public float jobWidthInches = 1f;
	//TODO: DPI needs setting based on resolution of the UV used for engravable blocks
	public int jobDPI = 100;

	void Start()
	{
		projectRoot = Path.GetDirectoryName(Application.dataPath);
		fullPath = Path.Combine(projectRoot, testImagePath);
		if (!File.Exists(fullPath))
		{
			Debug.LogError($"Missing test image at{fullPath}");
			return;
		}

		byte[] data = File.ReadAllBytes(fullPath);
		PrintJob job = PrintJob.FromImage(data, jobWidthInches, jobDPI);
		laserHead.LoadJob(job);
	}
}