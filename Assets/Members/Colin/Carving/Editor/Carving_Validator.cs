using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks the open Carving scene's wiring. Replaces the correctness-by-construction the deleted
/// scene builder provided: every reference below is dereferenced without a null check somewhere
/// at Play, so an empty slot is a silent no-op or an exception. Findings are logged one per line
/// (the console truncates long multi-line messages).
/// </summary>
public static class Carving_Validator
{
    [MenuItem("Carving/7 Validate Scene")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        var errors = new List<string>();
        var warnings = new List<string>();
        var listed = new HashSet<Carving_Tool>();

        var jogs = Object.FindObjectsByType<Tool_Jog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (jogs.Length != 1) errors.Add("expected exactly one Tool_Jog, found " + jogs.Length + ".");
        foreach (var jog in jogs)
        {
            var so = new SerializedObject(jog);
            var tools = so.FindProperty("Tools");
            if (tools.arraySize == 0) errors.Add("Tool_Jog.Tools is empty — nothing carves.");
            for (int i = 0; i < tools.arraySize; i++)
            {
                var tool = tools.GetArrayElementAtIndex(i).objectReferenceValue as Carving_Tool;
                if (tool == null) errors.Add("Tool_Jog.Tools[" + i + "] is empty.");
                else listed.Add(tool);
            }
            if (so.FindProperty("Mill").objectReferenceValue == null) errors.Add("Tool_Jog.Mill is empty — R cannot reset it.");
            if (so.FindProperty("Lathe").objectReferenceValue == null) errors.Add("Tool_Jog.Lathe is empty — R cannot reset it.");
            if (so.FindProperty("Readout").objectReferenceValue == null) warnings.Add("Tool_Jog.Readout is empty — no on-screen stats.");
        }

        foreach (var tool in Object.FindObjectsByType<Carving_Tool>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(tool);
            var def = so.FindProperty("Definition").objectReferenceValue as Tool_Definition;
            if (def == null) errors.Add(tool.name + ": Definition is empty — the tool disables itself.");
            else if (def.Profile.Diameter <= 0f || def.Profile.Length <= 0f) errors.Add(def.name + ": Diameter and Length must be positive.");
            if (so.FindProperty("Visual").objectReferenceValue == null) errors.Add(tool.name + ": Visual is empty — the tool is invisible.");
            bool mill = so.FindProperty("Mill").objectReferenceValue != null;
            bool lathe = so.FindProperty("Lathe").objectReferenceValue != null;
            if (mill == lathe) errors.Add(tool.name + ": assign exactly one of Mill / Lathe.");
            if (!listed.Contains(tool)) warnings.Add(tool.name + " is not in Tool_Jog.Tools, so it never carves.");
        }

        foreach (var stock in Object.FindObjectsByType<Mill_Stock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Check_Stock(stock, new[] { "Cell", "Pitch" }, errors);
        foreach (var stock in Object.FindObjectsByType<Lathe_Stock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Check_Stock(stock, new[] { "Cell", "Length", "Radius" }, errors);
            if (new SerializedObject(stock).FindProperty("Segments").intValue < 3) errors.Add(stock.name + ".Segments must be at least 3.");
        }

        foreach (var path in Object.FindObjectsByType<Carving_Demo_Path>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var steps = new SerializedObject(path).FindProperty("Steps");
            for (int i = 0; i < steps.arraySize; i++)
                if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("Tool").objectReferenceValue == null)
                    errors.Add(path.name + ": Carving_Demo_Path.Steps[" + i + "].Tool is empty.");
        }

        foreach (string error in errors) Debug.LogError("Carving_Validator: " + error);
        foreach (string warning in warnings) Debug.LogWarning("Carving_Validator: " + warning);
        if (errors.Count == 0 && warnings.Count == 0) Debug.Log("Carving_Validator: '" + scene.name + "' OK.");
        else Debug.Log("Carving_Validator: '" + scene.name + "' — " + errors.Count + " error(s), " + warnings.Count + " warning(s).");
    }

    // Both stocks build their mesh on their own MeshFilter and take the material from their MeshRenderer.
    static void Check_Stock(Component stock, string[] positive_fields, List<string> errors)
    {
        var go = stock.gameObject;
        if (go.GetComponent<MeshFilter>() == null) errors.Add(go.name + ": no MeshFilter.");
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.sharedMaterial == null) errors.Add(go.name + ": MeshRenderer has no material.");
        if (go.transform.lossyScale != Vector3.one) errors.Add(go.name + ": must be unit scale (carving works in metres).");
        var so = new SerializedObject(stock);
        foreach (string field in positive_fields)
            if (so.FindProperty(field).floatValue <= 0f) errors.Add(go.name + "." + field + " must be positive.");
    }
}
