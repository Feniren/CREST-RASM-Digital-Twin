using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot deterministic build of Assets/Members/Colin/Carving/Scenes/Carving_Sandbox.unity.
/// Re-run it to regenerate the scene while the sandbox is being iterated on; delete it once the
/// scene is stable (repo convention for one-shot tools).
/// </summary>
public static class Carving_Sandbox_Builder
{
    const string Root = "Assets/Members/Colin/Carving";
    const string Scene_Path = Root + "/Scenes/Carving_Sandbox.unity";
    const string Materials = Root + "/Materials";

    [MenuItem("Carving/1 Build Sandbox Scene")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Carving: exit Play mode first.");
            return;
        }
        if (SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("Carving: the open scene has unsaved changes; save or discard them first.");
            return;
        }

        Directory.CreateDirectory(Materials);
        Directory.CreateDirectory(Root + "/Scenes");
        AssetDatabase.Refresh();

        var stock_mat = Make_Material(Materials + "/Carving_Stock.mat", new Color(0.87f, 0.80f, 0.62f), 0f, 0.35f);
        var flat_mat = Make_Material(Materials + "/Carving_Tool_Flat.mat", new Color(0.62f, 0.64f, 0.68f), 0.8f, 0.7f);
        var ball_mat = Make_Material(Materials + "/Carving_Tool_Ball.mat", new Color(0.35f, 0.55f, 0.90f), 0.8f, 0.7f);
        var drill_mat = Make_Material(Materials + "/Carving_Tool_Drill.mat", new Color(0.85f, 0.35f, 0.30f), 0.8f, 0.7f);
        var stand_mat = Make_Material(Materials + "/Carving_Stand.mat", new Color(0.30f, 0.30f, 0.32f), 0.2f, 0.3f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0.15f, 0.55f, -0.75f);
            cam.transform.LookAt(new Vector3(0.15f, 0.03f, 0f));
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.01f;
        }

        var tools = new List<Carving_Tool>();

        // Mill station: block on a stand, four tools parked 20 mm above the top face.
        var mill_station = new GameObject("Mill_Station");
        var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Stand";
        stand.transform.SetParent(mill_station.transform, true);
        stand.transform.position = new Vector3(0f, -0.01f, 0f);
        stand.transform.localScale = new Vector3(0.30f, 0.02f, 0.20f);
        stand.GetComponent<MeshRenderer>().sharedMaterial = stand_mat;

        var mill_go = new GameObject("Mill_Stock");
        mill_go.transform.SetParent(mill_station.transform, true);
        mill_go.transform.position = new Vector3(-0.075f, 0f, -0.05f);
        var mill = mill_go.AddComponent<Mill_Stock>();
        mill.Material = stock_mat;

        tools.Add(Make_Tool("Tool_Flat_3", mill_station.transform, new Vector3(-0.045f, 0.070f, 0f), Quaternion.identity, 0.003f, 0.030f, Tool_Profile.Tip.Flat, flat_mat, mill, null));
        tools.Add(Make_Tool("Tool_Flat_10", mill_station.transform, new Vector3(-0.015f, 0.070f, 0f), Quaternion.identity, 0.010f, 0.040f, Tool_Profile.Tip.Flat, flat_mat, mill, null));
        tools.Add(Make_Tool("Tool_Ball_6", mill_station.transform, new Vector3(0.015f, 0.070f, 0f), Quaternion.identity, 0.006f, 0.035f, Tool_Profile.Tip.Ball, ball_mat, mill, null));
        tools.Add(Make_Tool("Tool_Drill_3", mill_station.transform, new Vector3(0.045f, 0.070f, 0f), Quaternion.identity, 0.003f, 0.040f, Tool_Profile.Tip.Drill, drill_mat, mill, null));

        // Lathe station: stock along Z, tools approach radially from +X.
        var lathe_station = new GameObject("Lathe_Station");
        lathe_station.transform.position = new Vector3(0.35f, 0f, 0f);
        var guide = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        guide.name = "Axis_Guide";
        guide.transform.SetParent(lathe_station.transform, true);
        guide.transform.position = new Vector3(0.35f, 0.025f, 0f);
        guide.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        guide.transform.localScale = new Vector3(0.004f, 0.08f, 0.004f);
        guide.GetComponent<MeshRenderer>().sharedMaterial = stand_mat;
        Object.DestroyImmediate(guide.GetComponent<Collider>());

        var lathe_go = new GameObject("Lathe_Stock");
        lathe_go.transform.SetParent(lathe_station.transform, true);
        lathe_go.transform.position = new Vector3(0.35f, 0.025f, -0.06f);
        var lathe = lathe_go.AddComponent<Lathe_Stock>();
        lathe.Material = stock_mat;

        var lathe_rot = Quaternion.FromToRotation(Vector3.up, Vector3.right);
        tools.Add(Make_Tool("Tool_Lathe_Flat_6", lathe_station.transform, new Vector3(0.385f, 0.025f, -0.04f), lathe_rot, 0.006f, 0.030f, Tool_Profile.Tip.Flat, flat_mat, null, lathe));
        tools.Add(Make_Tool("Tool_Lathe_Ball_6", lathe_station.transform, new Vector3(0.385f, 0.025f, 0.02f), lathe_rot, 0.006f, 0.030f, Tool_Profile.Tip.Ball, ball_mat, null, lathe));

        var jog = new GameObject("Tool_Jog").AddComponent<Tool_Jog>();
        jog.Tools = tools;
        jog.Mill = mill;
        jog.Lathe = lathe;

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, Scene_Path);
        Debug.Log("Carving: built " + Scene_Path);
        Debug.Log("Carving: mill stock " + mill.Size * 1000f + " mm at " + mill.Cell * 1000f + " mm cells, " + (tools.Count - 2) + " mill tools");
        Debug.Log("Carving: lathe stock Ø" + lathe.Radius * 2000f + " x " + lathe.Length * 1000f + " mm at " + lathe.Cell * 1000f + " mm cells, 2 lathe tools");
    }

    static Material Make_Material(string path, Color colour, float metallic, float smoothness)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", colour);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Carving_Tool Make_Tool(string name, Transform parent, Vector3 tip, Quaternion rotation,
                                  float diameter, float length, Tool_Profile.Tip tip_shape, Material mat,
                                  Mill_Stock mill, Lathe_Stock lathe)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(tip, rotation);
        var tool = go.AddComponent<Carving_Tool>();
        tool.Profile = new Tool_Profile { Diameter = diameter, Length = length, Tip_Shape = tip_shape };
        tool.Mill = mill;
        tool.Lathe = lathe;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Visual";
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.transform.SetParent(go.transform, false);
        vis.GetComponent<MeshRenderer>().sharedMaterial = mat;
        tool.Sync_Visual();
        return tool;
    }
}
