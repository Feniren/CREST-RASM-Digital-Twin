// using UnityEngine;

// public class Job_Mill_Epoxy_Penholder : Job_Parent{
// 	public Item_Mill MillReference;

// 	public Item_Epoxy_Block EpoxyBlockReference;

// 	public Job_Mill_Epoxy_Penholder(){
// 		Name = "Mill Epoxy Penholder";
// 	}

// 	public void OnEnable(){
// 		MillReference = FindFirstObjectByType<Item_Mill>();
// 	}

// 	public void OnTargetFound(){
// 		MillReference.SensorReference.OnTargetFound.RemoveListener(OnTargetFound);

// 		Destroy(MillReference.SensorReference.SensedPlate.Item);

// 		GameObject EpoxyPenholder = Instantiate(FindFirstObjectByType<Data_Loader>().ItemLibraryReference.GetItemFromName("Epoxy Penholder"), Vector3.zero, Quaternion.identity);

// 		MillReference.SensorReference.SensedPlate.Item = EpoxyPenholder;
// 		MillReference.SensorReference.SensedPlate.SetItem();

// 		OnJobComplete.Invoke();
// 	}

// 	public override void ExecuteJob(){
// 		MillReference.SensorReference.TargetItem = EpoxyBlockReference;

// 		MillReference.SensorReference.OnTargetFound.AddListener(OnTargetFound);
// 	}
// }
