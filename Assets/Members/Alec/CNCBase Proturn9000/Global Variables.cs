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

    // UI sprite organization
    public const string UI_class_global = "Global";
    public const string UI_class_top = "Top Bar";
    public const string UI_class_bottom = "Bottom Bar";
    public const string UI_class_control = "Control Windows";
    public const string UI_class_location = "Location Windows"; // unused currently: 10/21/26
    public const string UI_class_frame = "Static Frame";
}
