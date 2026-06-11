using Unity.VisualScripting;
using UnityEngine;

public class dummy_component : MonoBehaviour
{
    public int one = 1;
    [SerializeField] int two = 2;
    [SerializeField] private int three = 3;
    private int four = 4;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
