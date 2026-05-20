using UnityEngine;
using Parabox.CSG;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using System.Threading.Tasks;
using System.Collections;
public class Lathe_Sim : MonoBehaviour
{
    [SerializeField]  GameObject toolPrefab;
    [SerializeField]  GameObject stockObj;

    [SerializeField] private readonly float slack = 0.1f;
    

    void Start() {
        // Drilling(.5f, .25f);
        // test();
    }
    // CSG quick tutorial guide
    public void dictionary()
    {
        // create cube + sphere, increase size of sphere
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * 1.3f; // changes overall size of object to 1.3x og size
        sphere.transform.localScale += new Vector3(0f, 3f, 0f); // changes just the y side (length for this scenario due to 90 deg rotation)
        
        // perform subtraction 
        Model result = CSG.Subtract(cube, sphere);

        // re-render new version
        var composite = new GameObject();
        composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
        composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
    }

    public void Drilling(float diameter = 1f, float depth = 1f) // depth = y, diameter = x,z
    {
        /*
            1. spawn tool prefab
            2. set tool size
            3. set tool position to designated spot (main.y - depth)
            4. perform subtraction  
        */
        GameObject toolInstance = Instantiate(toolPrefab);

        toolInstance.transform.localScale = new Vector3(diameter, depth + slack, diameter);

        toolInstance.transform.position = stockObj.transform.position;
        float drillDepth = stockObj.transform.localScale.y - depth;
        toolInstance.transform.position = stockObj.transform.position;
        toolInstance.transform.position += new Vector3(drillDepth, 0f, 0f);

        Model result = CSG.Subtract(stockObj, toolInstance.transform.GetChild(0).gameObject);

        GameObject composite = new GameObject();
        // GameObject composite = Cloning(stockObj);

        
        MeshCopy(result, composite);

        // what's supposed to happen after everything works properly:
        string name = stockObj.name;

        Destroy(toolInstance);
        Destroy(stockObj);

        stockObj = composite;
        stockObj.name = name;
    }

    public void Turning(float diameter = 0.5f, float depth = 0.5f) {
        /*
            1. spawn two tool prefabs: one for subtraction and other for union
                a. subtraction tool prefab diameter is to be bigger than stock obj's diameter
                b. union will take in actual diameter + slack
            2. subtract using given depth with subtraction prefab, 
            3. output into a new gameObject (temp output)
            4. union new gameobject with union tool prefab
            5. output into a 2nd new gameObject, set this one as new stockObj
            6. delete all prefabs + temp output
        */
        // float slackDiameter = stockObj.l
        GameObject unionTool = Instantiate(toolPrefab), subTool = Instantiate(toolPrefab);
    }

    private void MeshCopy(Model CSGresult, GameObject output) {
        output.AddComponent<MeshFilter>().sharedMesh = CSGresult.mesh;
        output.AddComponent<MeshRenderer>().sharedMaterials = CSGresult.materials.ToArray();
    }

    public GameObject Cloning(GameObject original) // work in progress
    {
        // StartCoroutine(DelayedAction()); -- may be needed if operation does not work immediately, used right after calling this function

        // GameObject clone = Instantiate(original);
        List<string> exclusions = new List<string>{"MeshFilter", "MeshRenderer"};
        Component[] components = original.GetComponents(typeof(Component));

        foreach (Component component in components)
        {
            string name = component.GetType().Name;
            if(exclusions.Contains(name))
            {
                DestroyImmediate(component);
            }
        }
        GameObject clone = Instantiate(original);

        clone.name = "Clone";
        return clone;
    }
}
