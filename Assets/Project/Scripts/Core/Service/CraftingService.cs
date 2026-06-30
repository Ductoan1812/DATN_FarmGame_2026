using System.Collections.Generic;
using UnityEngine;

public class CraftingService
{
    private readonly EntityService entityService;
    private readonly InventoryService inventoryService;
    private readonly ProgressionService progressionService;
    private readonly EventBus eventBus;

    public CraftingService(
        EntityService entityService,
        InventoryService inventoryService,
        ProgressionService progressionService,
        EventBus eventBus)
    {
        this.entityService = entityService;
        this.inventoryService = inventoryService;
        this.progressionService = progressionService;
        this.eventBus = eventBus;
    }

    public void Open(EntityRuntime crafter, EntityRuntime station, IReadOnlyList<RecipeData> recipes)
    {
        if (crafter == null) return;
        var viewData = BuildView(crafter, station, recipes);
        eventBus?.Publish(new CraftingViewPublish(viewData));
    }

    public CraftingResult TryCraft(EntityRuntime crafter, RecipeData recipe, int times = 1)
    {
        if (crafter == null || recipe == null)
            return PublishResult(crafter, recipe, CraftingFailReason.InvalidRequest, 0);

        times = Mathf.Max(1, times);
        progressionService?.EnsureInitialized(crafter);

        var requirement = UnlockService.MergeLevelFallback(recipe.unlockRequirement, recipe.requiredLevel);
        if (!UnlockService.IsUnlocked(crafter, requirement))
            return PublishResult(crafter, recipe, CraftingFailReason.LevelTooLow, 0);

        if (!HasIngredients(crafter, recipe, times))
            return PublishResult(crafter, recipe, CraftingFailReason.NotEnoughIngredients, 0);

        if (!CanReceiveOutputs(crafter, recipe, times))
            return PublishResult(crafter, recipe, CraftingFailReason.InventoryFull, 0);

        if (!ConsumeIngredients(crafter, recipe, times))
            return PublishResult(crafter, recipe, CraftingFailReason.ConsumeFailed, 0);

        if (!GrantOutputs(crafter, recipe, times))
            return PublishResult(crafter, recipe, CraftingFailReason.OutputFailed, 0);

        int grantedExp = Mathf.Max(0, recipe.craftExp) * times;
        if (grantedExp > 0)
            progressionService?.GrantExp(crafter, grantedExp, ExpSourceType.Craft);

        return PublishResult(crafter, recipe, CraftingFailReason.None, times);
    }

    public CraftingViewData BuildView(EntityRuntime crafter, EntityRuntime station, IReadOnlyList<RecipeData> recipes)
    {
        var recipeViews = new List<CraftingRecipeViewData>();
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;
                if (!IsRecipeValid(recipe)) continue;
                recipeViews.Add(BuildRecipeView(crafter, recipe));
            }
        }

        return new CraftingViewData(crafter, station, recipeViews);
    }

    // A recipe is only shown if it can actually produce something and every
    // listed ingredient resolves to a real item. This hides recipes whose
    // item references are missing/dangling so the UI never renders blank rows.
    private static bool IsRecipeValid(RecipeData recipe)
    {
        if (recipe == null) return false;

        bool hasValidOutput = false;
        if (recipe.outputs != null)
        {
            foreach (var output in recipe.outputs)
            {
                if (output?.item == null) continue;
                hasValidOutput = true;
                break;
            }
        }

        if (!hasValidOutput) return false;

        if (recipe.ingredients != null)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient != null && ingredient.amount > 0 && ingredient.item == null)
                    return false;
            }
        }

        return true;
    }


    private CraftingRecipeViewData BuildRecipeView(EntityRuntime crafter, RecipeData recipe)
    {
        var ingredients = new List<CraftingIngredientViewData>();
        bool hasAllIngredients = true;

        if (recipe.ingredients != null)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient?.item == null || ingredient.amount <= 0) continue;

                int current = inventoryService?.CountEntity(crafter, ingredient.item.id) ?? 0;
                int required = Mathf.Max(1, ingredient.amount);
                bool enough = current >= required;
                hasAllIngredients &= enough;
                ingredients.Add(new CraftingIngredientViewData(ingredient.item, current, required, enough));
            }
        }

        progressionService?.EnsureInitialized(crafter);
        var requirement = UnlockService.MergeLevelFallback(recipe.unlockRequirement, recipe.requiredLevel);
        bool unlocked = UnlockService.IsUnlocked(crafter, requirement);
        string lockedReasonKey = UnlockService.GetLockedReasonKey(crafter, requirement);
        return new CraftingRecipeViewData(recipe, ingredients, unlocked, hasAllIngredients, unlocked && hasAllIngredients, lockedReasonKey);
    }

    private bool HasIngredients(EntityRuntime crafter, RecipeData recipe, int times)
    {
        if (inventoryService == null || recipe.ingredients == null) return true;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient?.item == null || ingredient.amount <= 0) continue;
            int required = ingredient.amount * times;
            if (inventoryService.CountEntity(crafter, ingredient.item.id) < required)
                return false;
        }

        return true;
    }

    private bool ConsumeIngredients(EntityRuntime crafter, RecipeData recipe, int times)
    {
        if (inventoryService == null || recipe.ingredients == null) return true;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient?.item == null || ingredient.amount <= 0) continue;
            if (!inventoryService.Remove(ingredient.item.id, ingredient.amount * times, crafter))
                return false;
        }

        return true;
    }

    private bool CanReceiveOutputs(EntityRuntime crafter, RecipeData recipe, int times)
    {
        if (entityService == null || inventoryService == null || recipe.outputs == null) return false;

        foreach (var output in recipe.outputs)
        {
            if (output?.item == null || output.amount <= 0) continue;

            int remaining = output.amount * times;
            int stackSize = Mathf.Max(1, output.item.maxStack);
            while (remaining > 0)
            {
                int batch = Mathf.Min(remaining, stackSize);
                var preview = entityService.Create(output.item, batch);
                int canReceive = inventoryService.CanReceive(crafter, preview, batch);
                entityService.Destroy(preview);
                if (canReceive < batch) return false;
                remaining -= batch;
            }
        }

        return true;
    }

    private bool GrantOutputs(EntityRuntime crafter, RecipeData recipe, int times)
    {
        if (entityService == null || inventoryService == null || recipe.outputs == null) return false;

        foreach (var output in recipe.outputs)
        {
            if (output?.item == null || output.amount <= 0) continue;

            int remaining = output.amount * times;
            int stackSize = Mathf.Max(1, output.item.maxStack);
            while (remaining > 0)
            {
                int batch = Mathf.Min(remaining, stackSize);
                var item = entityService.Create(output.item, batch);
                int received = inventoryService.Pickup(item, crafter);
                remaining -= received;

                if (received < batch && item != null && !item.IsEmpty)
                    entityService.Destroy(item);

                if (received <= 0)
                    return false;
            }
        }

        return true;
    }

    private CraftingResult PublishResult(EntityRuntime crafter, RecipeData recipe, CraftingFailReason failReason, int timesCrafted)
    {
        var result = new CraftingResult(failReason == CraftingFailReason.None, failReason, timesCrafted);
        eventBus?.Publish(new CraftingResultPublish(crafter, recipe, result));
        return result;
    }
}

public sealed class CraftingViewData
{
    public EntityRuntime Crafter { get; }
    public EntityRuntime Station { get; }
    public IReadOnlyList<CraftingRecipeViewData> Recipes { get; }

    public CraftingViewData(EntityRuntime crafter, EntityRuntime station, IReadOnlyList<CraftingRecipeViewData> recipes)
    {
        Crafter = crafter;
        Station = station;
        Recipes = recipes;
    }
}

public sealed class CraftingRecipeViewData
{
    public RecipeData Recipe { get; }
    public IReadOnlyList<CraftingIngredientViewData> Ingredients { get; }
    public bool LevelOk { get; }
    public bool HasIngredients { get; }
    public bool CanCraft { get; }
    public string LockedReasonKey { get; }

    public CraftingRecipeViewData(
        RecipeData recipe,
        IReadOnlyList<CraftingIngredientViewData> ingredients,
        bool levelOk,
        bool hasIngredients,
        bool canCraft)
        : this(recipe, ingredients, levelOk, hasIngredients, canCraft, string.Empty)
    {
    }

    public CraftingRecipeViewData(
        RecipeData recipe,
        IReadOnlyList<CraftingIngredientViewData> ingredients,
        bool levelOk,
        bool hasIngredients,
        bool canCraft,
        string lockedReasonKey)
    {
        Recipe = recipe;
        Ingredients = ingredients;
        LevelOk = levelOk;
        HasIngredients = hasIngredients;
        CanCraft = canCraft;
        LockedReasonKey = lockedReasonKey ?? string.Empty;
    }
}

public sealed class CraftingIngredientViewData
{
    public EntityData Item { get; }
    public int CurrentAmount { get; }
    public int RequiredAmount { get; }
    public bool HasEnough { get; }

    public CraftingIngredientViewData(EntityData item, int currentAmount, int requiredAmount, bool hasEnough)
    {
        Item = item;
        CurrentAmount = currentAmount;
        RequiredAmount = requiredAmount;
        HasEnough = hasEnough;
    }
}

public readonly struct CraftingResult
{
    public readonly bool Success;
    public readonly CraftingFailReason FailReason;
    public readonly int TimesCrafted;

    public CraftingResult(bool success, CraftingFailReason failReason, int timesCrafted)
    {
        Success = success;
        FailReason = failReason;
        TimesCrafted = timesCrafted;
    }
}

public enum CraftingFailReason
{
    None,
    InvalidRequest,
    LevelTooLow,
    NotEnoughIngredients,
    InventoryFull,
    ConsumeFailed,
    OutputFailed
}

public readonly struct CraftingViewPublish
{
    public readonly CraftingViewData viewData;
    public CraftingViewPublish(CraftingViewData viewData) { this.viewData = viewData; }
}

public readonly struct CraftingResultPublish
{
    public readonly EntityRuntime crafter;
    public readonly RecipeData recipe;
    public readonly CraftingResult result;

    public CraftingResultPublish(EntityRuntime crafter, RecipeData recipe, CraftingResult result)
    {
        this.crafter = crafter;
        this.recipe = recipe;
        this.result = result;
    }
}
