using UnityEngine;

[DisallowMultipleComponent]
public class IconObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EntityRuntime _EntityRoot;
    private EntityRoot _root;


    private void OnEnable()
    {
        _root = GetComponent<EntityRoot>();
        if(_root != null) _root.OnEntityReady += SetEntityRoot;
        spriteRenderer.sprite = null;
        TrySetIcon();
    }
    
    private void OnDisable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (spriteRenderer != null) spriteRenderer.sprite = null;
    }

    private void SetEntityRoot(EntityRuntime root)
    {
        _EntityRoot = root;
        TrySetIcon();
    }
    private void TrySetIcon()
    {
        if (_EntityRoot != null && _EntityRoot.entityData != null )spriteRenderer.sprite = _EntityRoot.entityData.icon;
    }
}
