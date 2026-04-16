[System.Serializable]
public class InventorySlot
{
    public EntityRuntime entity;
    public bool IsEmpty => entity == null || entity.IsEmpty;

    public void Clear()
    {
        entity = null;
    }
}
