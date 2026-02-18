using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item_Crafting_Library : MonoBehaviour{
    [SerializeField]
    public List<GameObject> ItemCraftingLibrary = new List<GameObject>();

    [SerializeField]
    public List<Item_Crafting_Recipe> ItemCraftingRecipe = new List<Item_Crafting_Recipe>();

    Item_Crafting_Library(){
        ItemCraftingRecipe.Add(new Item_Crafting_Recipe());
        ItemCraftingRecipe[0].Name = "Torus";
    }
}
