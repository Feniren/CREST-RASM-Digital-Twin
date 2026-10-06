using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// The Digital Twin's mill station for the job system: loads a program by file name and, when
/// the job hands it a plate, takes the block off, cuts it in the vise (a plain block is swapped
/// for a fresh stock; a part cut here before keeps its cut), and sets the carved part back on
/// the same plate. The mill stays busy (Item_CNC_Machine.ProcessingItem)
/// for the whole cycle, which holds the belt at the RFID sensor.
/// </summary>
public class NC_Mill_Station : MonoBehaviour
{
    [Header("Machine")]
    public Item_Mill Mill;
    [SerializeField] private NC_Runner Runner;
    [SerializeField] private Mill_Doors_Physics Doors;
    [SerializeField] private NC_Machine_Profile Machine;

    [Header("Stock")]
    [Tooltip("Part root (item, rigidbody, collider) with the Mill_Stock as a child")]
    [SerializeField] private GameObject Workpiece_Prefab;
    [Tooltip("Rides the stock drives; the Work_Origin the link uses is authored under it")]
    [SerializeField] private Transform Stock_Mount;

    [Header("Programs")]
    [Tooltip("Folder under StreamingAssets holding the .nc files")]
    [SerializeField] private string Folder = "NC";

    /// <summary>A fresh plan for the file (plans are changed while they run), or null when it is missing.</summary>
    public NC_Job Load(string file)
    {
        string path = Path.Combine(Application.streamingAssetsPath, Folder, file);
        if (!File.Exists(path))
        {
            Debug.LogError("NC_Mill_Station: program not found: " + path);
            return null;
        }
        var job = NC_Program_Library.Load(path, Machine.Settings);
        foreach (var n in job.Program.Notices) Debug.LogWarning("NC_Mill_Station: " + job.Name + ": " + n);
        foreach (var n in job.Plan.Notices)
            if (n.Severity != NC_Notice.Level.Info) Debug.LogWarning("NC_Mill_Station: " + job.Name + ": " + n);
        return job;
    }

    public void Run(NC_Job job, Item_Plate plate, Action done)
    {
        Mill.ProcessingItem = true;   // before any yield: the sensor must ignore the next plate
        StartCoroutine(Cycle(job, plate, done));
    }

    IEnumerator Cycle(NC_Job job, Item_Plate plate, Action done)
    {
        yield return Set_Doors(true);
        var part = plate.Item;
        var stock = part.GetComponentInChildren<Mill_Stock>();
        if (stock != null)
        {
            plate.UnSetItem();                                     // a part cut here before: back in the vise with its cut
            part.transform.SetParent(Stock_Mount, false);
            part.transform.localRotation = Quaternion.identity;
            stock.enabled = true;
        }
        else
        {
            Destroy(part);                                         // a plain block: swapped for a fresh stock
            part = Instantiate(Workpiece_Prefab, Stock_Mount);
            part.name = "Part_" + Path.GetFileNameWithoutExtension(job.Name);
            part.GetComponent<Rigidbody>().isKinematic = true;
            stock = part.GetComponentInChildren<Mill_Stock>();
        }
        part.transform.localPosition = -stock.transform.localPosition;   // the stock's min corner on the mount: block in the vise
        yield return Set_Doors(false);

        Runner.Load(job.Plan, stock);
        Runner.Play();
        yield return new WaitUntil(() => Runner.Current_State == NC_Runner.State.Done);
        Debug.Log("NC_Mill_Station: " + job.Name + " done, cut " + Runner.Cut_Seconds.ToString("0") + " s"
                  + (Runner.Alarm_Text != null ? ", ALARM " + Runner.Alarm_Text : "") + ", " + job.Warning_Count + " warnings");

        yield return Set_Doors(true);
        stock.enabled = false;                                     // the mesh stays; no more remesh checks
        plate.Item = part;
        plate.SetItem();
        Mill.ProcessingItem = false;
        done();
    }

    IEnumerator Set_Doors(bool open)
    {
        Doors.Set_Open(open);
        yield return new WaitUntil(() => !Doors.IsSliding);
    }
}
