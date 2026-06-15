using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class adjustSize : MonoBehaviour
{
    private float diameter = -1, length = -1;
    [SerializeField] private Lathe_Sim sim;

    public void SetDia(TMP_InputField inputField)
    {
        float.TryParse(inputField.text, out float diameter);
        this.diameter = diameter;
    }
    public void SetLen(TMP_InputField inputField)
    {
        float.TryParse(inputField.text, out float length);
        this.length = length;
    }

    public void CallLathe()
    {
        if(diameter > -1 && length > -1)
        {
            print("Diameter " + diameter + " Length " + length);
            sim.Drilling(diameter, length);
        }
        else
        {
            sim.Drilling();
        }
    }

    public void Reload()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
