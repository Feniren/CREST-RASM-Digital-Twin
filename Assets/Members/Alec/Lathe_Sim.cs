using UnityEngine;
using Parabox.CSG;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using System.Threading.Tasks;
using System.Collections;
public class Lathe_Sim : MonoBehaviour
{
    // stockObj should be given a method to change its actual value
    [SerializeField]  GameObject toolPrefab;
    [SerializeField]  GameObject stockObj;

    [SerializeField] private readonly float slack = 0.1f;
    

    void Start() {
        Drilling(3, .5f, 1);
        // test();
    }
    // CSG quick tutorial guide
    private void CSGquickDictionary()
    {
        // create cube + sphere, increase size of sphere
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * 1.3f; // changes overall size of object to 1.3x og size
        sphere.transform.localScale += new Vector3(0f, 3f, 0f); // changes just the y side (length for this scenario due to 90 deg rotation)
        
        // operations 
        Model result = CSG.Subtract(cube, sphere);
        result = CSG.Intersect(cube, sphere);
        result = CSG.Union(cube, sphere);

        // re-render new version
        var composite = new GameObject();
        composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
        composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
    }

    private GameObject ToolSetup(float diameter = 1f, float depth = 1f, float positionOffset = 0f)
    {
        // if dimensions don't make sense, throw an error message/exception
        // ask if there's any big limitations/keep-out zones for safety reasons

        GameObject tool = Instantiate(toolPrefab);

        tool.transform.localScale = new Vector3(diameter, depth + slack, diameter);
        
        float operationPosition = stockObj.transform.localScale.y - depth - positionOffset + slack;

        tool.transform.position = stockObj.transform.position;
        tool.transform.position += new Vector3(operationPosition, 0f, 0f);

        return tool;
    }

    public void Cloning(GameObject original, GameObject clone) // work in progress
    {
        List<string> exclusions = new List<string>{"MeshFilter", "MeshRenderer"};
        Component[] components = original.GetComponents(typeof(Component));

        foreach (Component component in components)
        {
            string name = component.GetType().Name;
            if(exclusions.Contains(name))
            {
                continue;
            }
            else
            {
                clone.CopyComponent(component);
            }
        }
    }

    public void Drilling(float diameter = 1f, float depth = 1f, float positionOffset = 0f) // depth = y, diameter = x,z
    {
        /*
            1. spawn tool prefab
            2. set tool size
            3. set tool position to designated spot (main.y - depth)
            4. perform subtraction  
        */
        GameObject toolInstance = ToolSetup(diameter, depth, positionOffset);

        // Model result = CSG.Subtract(stockObj, toolInstance.transform.GetChild(0).gameObject);

        // GameObject composite = new GameObject();
        // // GameObject composite = Cloning(stockObj);

        
        // MeshCopy(result, composite);

        // // what's supposed to happen after everything works properly:
        // string name = stockObj.name;

        // Destroy(toolInstance);
        // Destroy(stockObj);

        // stockObj = composite;
        // stockObj.name = name;
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
}
