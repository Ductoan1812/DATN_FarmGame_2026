using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    private EquipmentSlotUI[] slots;
    private EventBus eventBus;
    private bool subscribedToBus;

    private void Awake()
    {
        CacheSlots();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedToBus && eventBus != null)
        {
            eventBus.Unsubscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
            subscribedToBus = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribedToBus)
        {
            return;
        }

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null)
        {
            return;
        }

        eventBus.Subscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
        subscribedToBus = true;
    }

    private void OnEquipmentSlotChanged(EquipmentSlotChangedPublish e)
    {
        if (slots == null)
        {
            return;
        }

        foreach (var slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.Part == e.part)
            {
                slot.SetItem(e.item);
            }
        }
    }

    private void CacheSlots()
    {
        slots = GetComponentsInChildren<EquipmentSlotUI>(true);
    }
}
