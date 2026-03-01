using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item_Crafting_Recipe{
    [SerializeField]
    public List<KeyValuePair<string, int>> ItemCraftingRecipe;

    [SerializeField]
    public string Name;

    public Item_Crafting_Recipe(){
        ItemCraftingRecipe = new List<KeyValuePair<string, int>>();
    }
}
