using UnityEngine;

/// <summary>One machine's NC_Machine_Settings as an authored asset.</summary>
[CreateAssetMenu(fileName = "Machine_Profile", menuName = "NC/Machine Profile")]
public class NC_Machine_Profile : ScriptableObject
{
    public NC_Machine_Settings Settings = new NC_Machine_Settings();
}
