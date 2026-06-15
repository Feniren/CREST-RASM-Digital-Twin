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
        // Drilling(3, .5f);
        // Turning();
        Grooving();
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

    public void Drilling(float diameter = 1f, float depth = 1f, float positionOffset = 0f) // depth = y, diameter = x,z
    {
        /*
            1. spawn tool prefab + set up tol position, size, etc
            2. perform subtraction
            3. clone component details from old stock
            4. copy onto new + add new meshes
        */
        GameObject toolInstance = ToolSetup(diameter, depth, positionOffset);

        Model result = CSG.Subtract(stockObj, toolInstance/*.transform.GetChild(0).gameObject*/);

        GameObject composite = Cloning(stockObj);
        
        MeshCopy(result, composite);

        composite.name = stockObj.name;

        Destroy(toolInstance);
        Destroy(stockObj);
    }

    public void Facing(float depth = 0.01f)
    {
        float stockDiameter = stockObj.transform.lossyScale.x;
        Drilling(stockDiameter, depth);
    }

    public GameObject Parting(float depth = 2f, float toolWidth = 0.01f)
    {
        GameObject partedObj = ToolSetup(stockObj.transform.lossyScale.x, depth - toolWidth, -2);
        Facing(depth);
        return partedObj;
    }

    public void Turning(float diameter = 0.5f, float depth = 0.5f, float positionOffset = 0f) {
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

        float subDiameter = stockObj.transform.lossyScale.x+1;
        GameObject unionTool = ToolSetup(diameter, depth, positionOffset), subTool = ToolSetup(subDiameter, depth, positionOffset);

        Model subResult = CSG.Subtract(stockObj, subTool);

        GameObject subComposite = new GameObject("sub-composite"), composite;
        MeshCopy(subResult, subComposite);

        Model result = CSG.Union(subComposite, unionTool);
        DestroyImmediate(subComposite);
        
        composite = Cloning(stockObj);

        MeshCopy(result, composite);
        composite.name = stockObj.name;

        Destroy(unionTool);
        Destroy(subTool);
        Destroy(stockObj);
    }

    public void Grooving(float diameter = .5f, float depth = .5f, float positionOffset = 1f)
    {
        Turning(diameter, depth, positionOffset);
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

    public GameObject Cloning(GameObject original) // work in progress
    {
        GameObject clone = new GameObject("stock clone");
        List<string> exclusions = new List<string>{"MeshFilter", "MeshRenderer", "CapsuleCollider", "Transform"};
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
        return clone;
    }
    
    private void MeshCopy(Model result, GameObject composite)
    {
        composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
        composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
        composite.AddComponent<MeshCollider>().sharedMesh = result.mesh; // check in to see if this is what we want
    }
}
