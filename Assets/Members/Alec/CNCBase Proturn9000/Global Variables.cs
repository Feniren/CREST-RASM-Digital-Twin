using UnityEngine;

public static class GlobalVariables
{
    // jogging string constants
    public const string posZ = "Pos Z";
    public const string negZ = "Neg Z";
    public const string posX = "Pos X";
    public const string negX = "Neg X";
    public const string cont = "Continuous";

    // jogging speeds

    // carriage limits; inches for neg limit: (-3.35, 0, -12.25)
    public static readonly Vector3 posLimit = new Vector3(0,0,0), negLimit = new Vector3(-0.08509f, 0, -0.31115f);
    
    // error state
}
