using UnityEngine;
using System.Collections.Generic;

public class DriverPanel : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject zSetPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        zSetPanel.SetActive(false);
    }

    public void ShowZSetPanel()
    {
        mainPanel.SetActive(false);
        zSetPanel.SetActive(true);
    }
}
