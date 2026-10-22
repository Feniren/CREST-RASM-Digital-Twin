using UnityEngine;

public class Job_Lathe_Metal_Penholder : Job_Parent{
	public Item_Lathe LatheReference;

	public Item_Tube TubeReference;

	public Job_Lathe_Metal_Penholder(){
		Name = "Lathe Metal Penholder";
	}

	public void OnEnable(){
		LatheReference = FindFirstObjectByType<Item_Lathe>();
	}

	public void OnTargetFound(){
		LatheReference.SensorReference.OnTargetFound.RemoveListener(OnTargetFound);

		Destroy(LatheReference.SensorReference.SensedPlate.Item);

		GameObject PenTube = Instantiate(FindFirstObjectByType<Data_Loader>().ItemLibrary.GetItemFromName("Pen Tube"), Vector3.zero, Quaternion.identity);

		LatheReference.SensorReference.SensedPlate.Item = PenTube;
		LatheReference.SensorReference.SensedPlate.SetItem();

		OnJobComplete.Invoke();
	}

	public override void ExecuteJob(){
		LatheReference.SensorReference.TargetItem = TubeReference;

		LatheReference.SensorReference.OnTargetFound.AddListener(OnTargetFound);
	}
}
