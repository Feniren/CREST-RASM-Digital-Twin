using UnityEngine;

/// <summary>
/// A cutting tool as an authored asset, so every scene (and later the Digital Twin's tool
/// library) shares one definition per tool instead of inline copies. The asset name is the
/// display name.
/// </summary>
[CreateAssetMenu(fileName = "Tool_Definition", menuName = "Carving/Tool Definition")]
public class Tool_Definition : ScriptableObject
{
    public Tool_Profile Profile = new Tool_Profile();
}
