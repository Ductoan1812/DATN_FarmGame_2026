using System;
using UnityEngine;

public class AnimalRuntime : IModuleRuntime, IHandleEvent<SecondaryActionEvent>, IHandleEvent<NextDayEvent>
{
    private readonly AnimalModule data;
    private AnimalState state = AnimalState.Hungry;
    private EntityRuntime owner;

    public AnimalRuntime(AnimalModule data)
    {
        this.data = data;
    }

    public AnimalState State => state;

    public string StatusTextKey => state switch
    {
        AnimalState.ProductReady => data.statusProductReadyKey,
        AnimalState.Fed => data.statusFedKey,
        _ => data.statusHungryKey
    };

    public string PrimaryOptionTextKey => state == AnimalState.ProductReady
        ? data.collectOptionTextKey
        : data.feedOptionTextKey;

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (e.context == null || e.initiator == null) return;

        var animal = owner ?? e.target;
        if (animal == null) return;

        e.context.AddOption(
            $"animal.{animal.entityData?.id ?? animal.id}.{state}",
            PrimaryOptionTextKey,
            data.priority,
            () => Interact(e.initiator));
    }

    public void Handle(NextDayEvent e)
    {
        if (state == AnimalState.Fed)
            state = AnimalState.ProductReady;
    }

    private void Interact(EntityRuntime player)
    {
        if (player == null) return;

        if (state == AnimalState.ProductReady)
        {
            CollectProduct(player);
            return;
        }

        if (state == AnimalState.Hungry)
        {
            Feed(player);
            return;
        }

        Debug.Log("[AnimalRuntime] Animal already fed today.");
    }

    private void Feed(EntityRuntime player)
    {
        if (data.feedItem == null)
        {
            Debug.LogWarning("[AnimalRuntime] feedItem missing.");
            return;
        }

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null) return;

        if (!inventoryService.Remove(data.feedItem.id, 1, player))
        {
            Debug.Log($"[AnimalRuntime] Missing feed item '{data.feedItem.id}'.");
            return;
        }

        state = AnimalState.Fed;
        Debug.Log($"[AnimalRuntime] Fed animal with '{data.feedItem.id}'.");
    }

    private void CollectProduct(EntityRuntime player)
    {
        if (data.productItem == null)
        {
            Debug.LogWarning("[AnimalRuntime] productItem missing.");
            return;
        }

        var gameManager = GameManager.Instance;
        var entityService = gameManager?.EntityService;
        var inventoryService = gameManager?.InventoryService;
        if (entityService == null || inventoryService == null) return;

        var product = entityService.Create(data.productItem, Mathf.Max(1, data.productAmount));
        int received = inventoryService.Pickup(product, player);
        if (received <= 0)
        {
            if (product != null && !product.IsEmpty)
                entityService.Destroy(product);
            return;
        }

        state = AnimalState.Hungry;
        Debug.Log($"[AnimalRuntime] Collected '{data.productItem.id}' x{received}.");
    }

    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData
        {
            moduleType = "Animal",
            dataJson = JsonUtility.ToJson(new AnimalSaveData { state = state })
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrWhiteSpace(save.dataJson)) return;

        var data = JsonUtility.FromJson<AnimalSaveData>(save.dataJson);
        state = data != null ? data.state : AnimalState.Hungry;
    }

    public bool MatchesSave(ModuleSaveData save) => save?.moduleType == "Animal";
    public bool Equals(IModuleRuntime other) => other is AnimalRuntime;

    [Serializable]
    private class AnimalSaveData
    {
        public AnimalState state;
    }
}
