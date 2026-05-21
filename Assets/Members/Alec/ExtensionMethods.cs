using UnityEngine;

using System;
using System.Reflection;

public static class ExtensionMethod
{
    public static TComponent CopyComponent<TComponent>(this GameObject destination, TComponent original) where TComponent : Component
    {
        Type comp = original.GetType(); // gets the type of component

        Component copy = destination.AddComponent(comp); // adds this component to destination obj and makes it accessible for changing

        FieldInfo[] fields = comp.GetFields(); // gets all the fields 

        foreach(FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original)); // sets given field with the original ver's field's info
        } 

        return copy as TComponent;
    }
}
