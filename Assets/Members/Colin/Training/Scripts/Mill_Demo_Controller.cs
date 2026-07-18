using System;
using System.Collections;
using UnityEngine;
using ProMill8000;

public class Mill_Demo_Controller : MonoBehaviour{
    [Header("Mill (machine-axis naming: X table, Y saddle, Z spindle)")]
    [SerializeField] private AxisMovement WorktableX;
    [SerializeField] private AxisMovement SaddleY;
    [SerializeField] private AxisMovement SpindleZ;
    [SerializeField] private MillingAnimation Milling;
    [SerializeField] private Item_Mill_Doors Doors;

    [Header("Axis indicators")]
    [SerializeField] private GameObject XIndicator;
    [SerializeField] private GameObject YIndicator;
    [SerializeField] private GameObject ZIndicator;

    [Header("Travel (meters)")]
    [SerializeField] private float XTravel = 0.14f;
    [SerializeField] private float YTravel = 0.04f;
    [SerializeField] private float ZTravel = 0.27f;
    [SerializeField] private float DoorSeconds = 1f;

    public event Action Demo_Finished;

    public void Play_Axis_Demo(){
        StartCoroutine(AxisDemo());
    }

    public void Play_Milling_Demo(){
        StartCoroutine(MillingDemo());
    }

    private IEnumerator AxisDemo(){
        yield return RunAxis(XIndicator, WorktableX, new[]{ XTravel, -XTravel, 0f });
        yield return RunAxis(YIndicator, SaddleY, new[]{ YTravel, -YTravel, 0f });
        yield return RunAxis(ZIndicator, SpindleZ, new[]{ -ZTravel, 0f });
        Demo_Finished?.Invoke();
    }

    private IEnumerator MillingDemo(){
        if (Doors != null){
            Doors.AlternateInteract(null);
            yield return new WaitForSeconds(DoorSeconds);
        }

        Milling.Play();
        yield return null;

        while (Milling.IsPlaying)
            yield return null;

        if (Doors != null){
            Doors.AlternateInteract(null);
            yield return new WaitForSeconds(DoorSeconds);
        }

        Demo_Finished?.Invoke();
    }

    private IEnumerator RunAxis(GameObject indicator, AxisMovement axis, float[] offsets){
        if (indicator != null)
            indicator.SetActive(true);

        foreach (float offset in offsets){
            axis.MoveToOffset(offset);

            while (axis.IsMoving)
                yield return null;
        }

        if (indicator != null)
            indicator.SetActive(false);
    }
}
