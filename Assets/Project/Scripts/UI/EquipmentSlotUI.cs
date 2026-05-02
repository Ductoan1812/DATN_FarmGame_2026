using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private EquipmentPart equipmentPart;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject emptyState;

    public EquipmentPart Part => equipmentPart;

    private void Awake()
    {
        AutoAssignRefs();
    }

    private void OnValidate()
    {
        AutoAssignRefs();
    }

    public void SetItem(EntityRuntime item)
    {
        var sprite = item?.entityData?.icon;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (emptyState != null)
        {
            emptyState.SetActive(sprite == null);
        }
    }

    private void AutoAssignRefs()
    {
        if (icon == null)
        {
            var iconTransform = transform.Find("Icon")
                ?? transform.Find("icon")
                ?? transform.Find("ImgItem")
                ?? transform.Find("Image");

            if (iconTransform != null)
            {
                icon = iconTransform.GetComponent<Image>();
            }
        }

        if (emptyState == null)
        {
            var emptyTransform = transform.Find("Empty")
                ?? transform.Find("EmptyState")
                ?? transform.Find("Placeholder");

            if (emptyTransform != null)
            {
                emptyState = emptyTransform.gameObject;
            }
        }
    }
}
