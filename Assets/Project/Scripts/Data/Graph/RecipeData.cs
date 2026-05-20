using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Recipe", fileName = "NewRecipe")]
public class RecipeData : ScriptableObject
{
    public string id;
    public string titleKey;
    [Min(1)] public int requiredLevel = 1;
    public UnlockRequirementData unlockRequirement = new();
    public List<RecipeIngredient> ingredients = new();
    public List<RecipeIngredient> outputs = new();
    [Min(0)] public int craftExp;
}
