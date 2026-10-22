using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored demo cuts for the sandbox: moves each listed tool to its target at a fixed feed so
/// the carve can be checked without a keyboard. Carving/8 Debug - Run Demo Cuts drives it over MCP.
/// </summary>
public class Carving_Demo_Path : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        public Carving_Tool Tool;
        public Vector3 Target;   // world metres
    }

    [SerializeField] private float Feed_M_S = 0.04f;
    [SerializeField] private List<Step> Steps = new List<Step>();

    public bool Is_Running => run != null;

    Coroutine run;

    public void Run()
    {
        if (run != null) StopCoroutine(run);
        run = StartCoroutine(Run_Steps());
    }

    IEnumerator Run_Steps()
    {
        Debug.Log("Carving_Demo_Path: " + Steps.Count + " moves at " + Feed_M_S * 1000f + " mm/s");
        foreach (var step in Steps)
        {
            if (step.Tool == null) continue;
            var t = step.Tool.transform;
            while (t.position != step.Target)
            {
                t.position = Vector3.MoveTowards(t.position, step.Target, Feed_M_S * Time.deltaTime);
                yield return null;
            }
        }
        Debug.Log("Carving_Demo_Path: done");
        run = null;
    }
}
