using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-mode-only demo: moves each sandbox tool along a short cut path at a fixed feed so the
/// carve can be checked without a keyboard (also handy for repeatable before/after comparisons).
/// Driven from EditorApplication.update, which ticks even when the Editor is unfocused.
/// </summary>
public static class Carving_Debug_Menu
{
    const float Feed_M_S = 0.04f;

    struct Step
    {
        public string Tool;
        public Vector3 Target;
        public Step(string tool, float x, float y, float z) { Tool = tool; Target = new Vector3(x, y, z); }
    }

    static readonly Queue<Step> steps = new Queue<Step>();
    static double last_time;
    static bool running;

    [MenuItem("Carving/8 Debug - Run Demo Cuts")]
    public static void Run_Demo_Cuts()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Carving: enter Play mode in Carving_Sandbox first.");
            return;
        }
        if (Object.FindFirstObjectByType<Tool_Jog>() == null)
        {
            Debug.LogError("Carving: no Tool_Jog in the open scene.");
            return;
        }

        steps.Clear();
        // Mill: block top face is y = 0.05; tools park at y = 0.07.
        steps.Enqueue(new Step("Tool_Flat_10", -0.015f, 0.047f, 0f));      // plunge 3 mm
        steps.Enqueue(new Step("Tool_Flat_10", -0.015f, 0.047f, 0.03f));   // slot
        steps.Enqueue(new Step("Tool_Flat_10", -0.015f, 0.070f, 0.03f));   // retract
        steps.Enqueue(new Step("Tool_Ball_6", 0.015f, 0.046f, -0.03f));
        steps.Enqueue(new Step("Tool_Ball_6", 0.015f, 0.046f, 0.03f));
        steps.Enqueue(new Step("Tool_Ball_6", 0.015f, 0.070f, 0.03f));
        steps.Enqueue(new Step("Tool_Drill_3", 0.045f, 0.030f, 0f));       // 20 mm deep hole
        steps.Enqueue(new Step("Tool_Drill_3", 0.045f, 0.070f, 0f));
        steps.Enqueue(new Step("Tool_Flat_3", -0.045f, 0.048f, -0.03f));
        steps.Enqueue(new Step("Tool_Flat_3", -0.020f, 0.048f, -0.03f));
        steps.Enqueue(new Step("Tool_Flat_3", -0.020f, 0.070f, -0.03f));
        // Lathe: axis at (0.35, 0.025), radius 0.025, tools park at x = 0.385 (r = 35 mm).
        steps.Enqueue(new Step("Tool_Lathe_Flat_6", 0.370f, 0.025f, -0.04f));  // r = 20 mm
        steps.Enqueue(new Step("Tool_Lathe_Flat_6", 0.370f, 0.025f, 0.0f));    // turn 40 mm
        steps.Enqueue(new Step("Tool_Lathe_Flat_6", 0.385f, 0.025f, 0.0f));
        steps.Enqueue(new Step("Tool_Lathe_Ball_6", 0.372f, 0.025f, 0.02f));   // r = 22 mm groove
        steps.Enqueue(new Step("Tool_Lathe_Ball_6", 0.372f, 0.025f, 0.05f));
        steps.Enqueue(new Step("Tool_Lathe_Ball_6", 0.385f, 0.025f, 0.05f));

        if (!running)
        {
            running = true;
            EditorApplication.update += Tick;
        }
        last_time = EditorApplication.timeSinceStartup;
        Debug.Log("Carving demo: " + steps.Count + " moves queued at " + Feed_M_S * 1000f + " mm/s");
    }

    static void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - last_time);
        last_time = now;

        if (!Application.isPlaying || steps.Count == 0)
        {
            Stop();
            return;
        }

        var step = steps.Peek();
        var go = GameObject.Find(step.Tool);
        if (go == null)
        {
            Debug.LogError("Carving demo: tool not found: " + step.Tool);
            steps.Dequeue();
            return;
        }

        var t = go.transform;
        t.position = Vector3.MoveTowards(t.position, step.Target, Feed_M_S * dt);
        if ((t.position - step.Target).sqrMagnitude < 1e-12f)
        {
            steps.Dequeue();
            if (steps.Count == 0) Debug.Log("Carving demo: done");
        }
    }

    static void Stop()
    {
        if (!running) return;
        running = false;
        EditorApplication.update -= Tick;
        steps.Clear();
    }
}
