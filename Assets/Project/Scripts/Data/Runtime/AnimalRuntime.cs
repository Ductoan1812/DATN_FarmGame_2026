using System;
using UnityEngine;

public class AnimalRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>, IHandleEvent<SecondaryActionEvent>, IHandleEvent<NextDayEvent>
{
    private readonly AnimalModule data;
    private AnimalState state = AnimalState.Hungry;
    private EntityRuntime owner;
    private int daysNotFed;

    public AnimalRuntime(AnimalModule data)
    {
        this.data = data;
    }

    public AnimalState State => state;

    public string StatusTextKey => state switch
    {
        AnimalState.ProductReady => data.statusProductReadyKey,
        AnimalState.Fed => data.statusFedKey,
        AnimalState.Dead => data.statusDeadKey,
        _ => data.statusHungryKey
    };

    public string PrimaryOptionTextKey => state switch
    {
        AnimalState.ProductReady => data.collectOptionTextKey,
        AnimalState.Dead => string.Empty,
        _ => data.feedOptionTextKey
    };

    public int DaysNotFed => daysNotFed;

    public void Handle(SpawnedEvent e)
    {
        owner = e.entity;
    }

    public void Handle(SecondaryActionEvent e)
    {
        owner ??= e.target;
        if (state == AnimalState.Dead || e.context == null || e.initiator == null) return;

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
        if (state == AnimalState.Dead)
            return;

        if (state == AnimalState.Fed)
        {
            state = AnimalState.ProductReady;
            daysNotFed = 0;
            return;
        }

        daysNotFed++;
        if (daysNotFed >= Mathf.Max(1, data.daysWithoutFoodToDie))
            DieFromStarvation();
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
        daysNotFed = 0;
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

    private void DieFromStarvation()
    {
        if (state == AnimalState.Dead)
            return;

        state = AnimalState.Dead;
        if (owner == null)
        {
            Debug.LogWarning("[AnimalRuntime] Animal died from starvation but owner is missing.");
            return;
        }

        GameManager.Instance?.EventBus?.Publish(new DestroyEntityRequestPublish(owner.id));
        Debug.Log($"[AnimalRuntime] Animal '{owner.entityData?.id ?? owner.id}' died from starvation after {daysNotFed} day(s) without food.");
    }

    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData
        {
            moduleType = "Animal",
            dataJson = JsonUtility.ToJson(new AnimalSaveData { state = state, daysNotFed = daysNotFed })
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrWhiteSpace(save.dataJson)) return;

        var saveData = JsonUtility.FromJson<AnimalSaveData>(save.dataJson);
        state = saveData != null ? saveData.state : AnimalState.Hungry;
        daysNotFed = saveData != null ? Mathf.Max(0, saveData.daysNotFed) : 0;
    }

    public bool MatchesSave(ModuleSaveData save) => save?.moduleType == "Animal";
    public bool Equals(IModuleRuntime other) => other is AnimalRuntime;

    [Serializable]
    private class AnimalSaveData
    {
        public AnimalState state;
        public int daysNotFed;
    }
}
