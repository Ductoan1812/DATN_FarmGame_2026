using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

/// <summary>
/// Container chứa item đang trang bị trên Player/NPC.
/// Key = EquipmentPart (enum của HeroEditor4D).
///
/// Khi equip:
///   1. Tính toán stats (xóa cũ + cộng mới) → nếu fail thì dừng
///   2. Nếu OK → tìm Owner → lấy Character4D → gọi Character4D.Equip()
///   3. Fire OnChanged event → PlayerEquipment lắng nghe → publish UI
/// </summary>
public class EquipmentRuntime : IModuleRuntime
{
    private readonly EquipmentModule _config;
    private readonly Dictionary<EquipmentPart, EntityRuntime> _slots = new();

    /// <summary>StatsRuntime của owner (player) — set từ bên ngoài khi init.</summary>
    public StatsRuntime OwnerStats { get; set; }

    /// <summary>Fired khi bất kỳ slot nào thay đổi (equip/unequip).</summary>
    public event Action OnChanged;

    public EquipmentRuntime(EquipmentModule config)
    {
        _config = config;
    }

    // ══════ Equip ══════

    /// <summary>
    /// Trang bị entity vào slot tương ứng.
    /// Đọc AppearanceRuntime từ entity để biết EquipmentPart + SpriteId.
    /// 1. Tính stats (remove cũ + apply mới)
    /// 2. Cập nhật Character4D visual
    /// 3. Fire OnChanged
    /// Trả về entity cũ nếu slot đã có (swap), null nếu slot trống.
    /// Trả về entity gốc (không thay đổi) nếu equip thất bại.
    /// </summary>
    public EntityRuntime Equip(EntityRuntime entity)
    {
        if (entity == null) return null;

        var appearance = entity.GetModule<AppearanceRuntime>();
        if (appearance == null)
        {
            Debug.LogWarning($"[EquipmentRuntime] '{entity.entityData?.keyName}' không có AppearanceModule → không equip được.");
            return null;
        }

        var part = appearance.EquipmentPart;

        // Kiểm tra slot có được hỗ trợ không
        if (_config.supportedParts != null && _config.supportedParts.Count > 0
            && !_config.supportedParts.Contains(part))
        {
            Debug.LogWarning($"[EquipmentRuntime] Part '{part}' không nằm trong supportedParts.");
            return null;
        }

        // ── Bước 1: Tính stats ──
        EntityRuntime previous = null;
        if (_slots.TryGetValue(part, out var existing))
        {
            RemoveStats(existing);
            previous = existing;
        }

        ApplyStats(entity);

        // ── Cập nhật slot ──
        _slots[part] = entity;

        // ── Bước 2: Cập nhật Character4D visual ──
        ApplyVisual(entity, appearance);

        // ── Bước 3: Fire event ──
        OnChanged?.Invoke();

        return previous;
    }

    // ══════ Unequip ══════

    /// <summary>Tháo item khỏi slot. Remove stats + visual. Trả về entity.</summary>
    public EntityRuntime Unequip(EquipmentPart part)
    {
        if (!_slots.TryGetValue(part, out var entity)) return null;

        RemoveStats(entity);
        _slots.Remove(part);

        // Clear visual
        var character4D = FindCharacter4D();
        character4D?.UnEquip(part);

        OnChanged?.Invoke();
        return entity;
    }

    /// <summary>
    /// Equip Hand item (từ hotbar selection).
    /// Hand item đặc biệt: không xóa khỏi inventory, chỉ hiển thị visual + stats.
    /// </summary>
    public void EquipHand(EntityRuntime entity)
    {
        if (entity == null)
        {
            ClearHand();
            return;
        }

        var appearance = entity.GetModule<AppearanceRuntime>();
        if (appearance == null)
        {
            ClearHand();
            return;
        }

        var part = appearance.EquipmentPart;

        // Remove stats + visual của hand item cũ (nếu có)
        ClearHandInternal();

        // Apply mới
        ApplyStats(entity);
        _slots[part] = entity;
        ApplyVisual(entity, appearance);

        OnChanged?.Invoke();
    }

    /// <summary>Clear tất cả hand-type slots (weapon/shield/bow...).</summary>
    public void ClearHand()
    {
        ClearHandInternal();
        OnChanged?.Invoke();
    }

    private void ClearHandInternal()
    {
        // Hand items = weapon/shield slots
        var handParts = new[]
        {
            EquipmentPart.MeleeWeapon1H,
            EquipmentPart.MeleeWeapon2H,
            EquipmentPart.Bow,
            EquipmentPart.Crossbow,
            EquipmentPart.Firearm1H,
            EquipmentPart.Firearm2H,
            EquipmentPart.SecondaryMelee1H,
            EquipmentPart.SecondaryFirearm1H,
            EquipmentPart.Shield
        };

        var character4D = FindCharacter4D();

        foreach (var part in handParts)
        {
            if (_slots.TryGetValue(part, out var existing))
            {
                RemoveStats(existing);
                _slots.Remove(part);
                character4D?.UnEquip(part);
            }
        }
    }

    // ══════ Query ══════

    public EntityRuntime Get(EquipmentPart part)
    {
        _slots.TryGetValue(part, out var entity);
        return entity;
    }

    public bool HasItem(EquipmentPart part) => _slots.ContainsKey(part);

    public IEnumerable<KeyValuePair<EquipmentPart, EntityRuntime>> GetAll() => _slots;

    /// <summary>Kiểm tra entity có đang được equip không (bất kỳ slot nào).</summary>
    public bool IsEquipped(EntityRuntime entity)
    {
        if (entity == null) return false;
        return _slots.ContainsValue(entity);
    }

    // ══════ Stats ══════

    private void ApplyStats(EntityRuntime entity)
    {
        if (OwnerStats == null || entity?.entityData?.baseStats?.baseStats == null) return;
        foreach (var entry in entity.entityData.baseStats.baseStats)
            OwnerStats.AddFlat(entry.statType, entry.value);
    }

    private void RemoveStats(EntityRuntime entity)
    {
        if (OwnerStats == null || entity?.entityData?.baseStats?.baseStats == null) return;
        foreach (var entry in entity.entityData.baseStats.baseStats)
            OwnerStats.AddFlat(entry.statType, -entry.value);
    }

    // ══════ Visual ══════

    private void ApplyVisual(EntityRuntime entity, AppearanceRuntime appearance)
    {
        var character4D = FindCharacter4D();
        if (character4D == null) return;

        var spriteId = appearance.SpriteId;
        var part = appearance.EquipmentPart;

        if (string.IsNullOrEmpty(spriteId))
        {
            Debug.LogWarning($"[EquipmentRuntime] Entity '{entity.entityData?.keyName}' có AppearanceModule nhưng spriteId rỗng.");
            return;
        }

        var spriteCollection = character4D.SpriteCollection;
        if (spriteCollection == null)
        {
            Debug.LogWarning("[EquipmentRuntime] Character4D.SpriteCollection null.");
            return;
        }

        var itemSprite = FindItemSprite(spriteCollection, part, spriteId);
        if (itemSprite == null)
        {
            Debug.LogWarning($"[EquipmentRuntime] Không tìm thấy sprite '{spriteId}' cho part '{part}' trong SpriteCollection.");
            return;
        }

        character4D.Equip(itemSprite, part);
    }

    private Character4D FindCharacter4D()
    {
        // Tìm qua Owner → GameObject → GetComponentInChildren
        var ownerEntity = FindOwnerEntity();
        if (ownerEntity?.Owner?.GameObject == null) return null;
        return ownerEntity.Owner.GameObject.GetComponentInChildren<Character4D>();
    }

    /// <summary>
    /// Tìm EntityRuntime sở hữu EquipmentRuntime này.
    /// Duyệt qua tất cả entity trong registry không khả thi,
    /// nên dùng cách: EquipmentRuntime được tạo từ module của entity nào
    /// thì entity đó chính là owner. Lưu ref khi init.
    /// </summary>
    private EntityRuntime _ownerEntity;

    /// <summary>Set bởi bên ngoài (PlayerEquipment) sau khi init.</summary>
    public void SetOwner(EntityRuntime owner)
    {
        _ownerEntity = owner;
    }

    private EntityRuntime FindOwnerEntity() => _ownerEntity;

    /// <summary>
    /// Tra cứu ItemSprite từ SpriteCollection dựa trên EquipmentPart + spriteId.
    /// </summary>
    private static ItemSprite FindItemSprite(SpriteCollection collection, EquipmentPart part, string spriteId)
    {
        List<ItemSprite> list = part switch
        {
            EquipmentPart.Armor    => collection.Armor,
            EquipmentPart.Helmet   => collection.Armor, // Helmet cũng nằm trong Armor list
            EquipmentPart.Vest     => collection.Armor,
            EquipmentPart.Bracers  => collection.Armor,
            EquipmentPart.Leggings => collection.Armor,
            EquipmentPart.MeleeWeapon1H      => collection.MeleeWeapon1H,
            EquipmentPart.MeleeWeapon2H      => collection.MeleeWeapon2H,
            EquipmentPart.SecondaryMelee1H   => collection.MeleeWeapon1H,
            EquipmentPart.Bow                => collection.Bow,
            EquipmentPart.Crossbow           => collection.Crossbow,
            EquipmentPart.Firearm1H          => collection.Firearm1H,
            EquipmentPart.Firearm2H          => collection.Firearm2H,
            EquipmentPart.SecondaryFirearm1H => collection.Firearm1H,
            EquipmentPart.Shield             => collection.Shield,
            EquipmentPart.Back               => collection.Back,
            EquipmentPart.Mask               => collection.Mask,
            EquipmentPart.Earrings           => collection.Earrings,
            EquipmentPart.Wings              => collection.Wings,
            _ => null
        };

        if (list == null) return null;
        return list.FirstOrDefault(i => i.Id == spriteId);
    }

    // ══════ Save / Load ══════

    public ModuleSaveData ToSaveData()
    {
        var entries = new List<EquipSlotSave>();
        foreach (var kv in _slots)
        {
            entries.Add(new EquipSlotSave { part = (int)kv.Key, entityId = kv.Value.id });
        }

        var data = new EquipmentSaveData { entries = entries.ToArray() };
        return new ModuleSaveData
        {
            moduleType = "Equipment",
            dataJson = JsonUtility.ToJson(data)
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var data = JsonUtility.FromJson<EquipmentSaveData>(save.dataJson);
        if (data?.entries == null) return;
        _pendingSlots = data.entries;
    }

    public void RestoreSlots(EntityRegistry registry)
    {
        if (_pendingSlots == null) return;
        foreach (var s in _pendingSlots)
        {
            var entity = registry.Get(s.entityId);
            if (entity == null)
            {
                Debug.LogWarning($"[EquipmentRuntime] Không tìm thấy entity id={s.entityId}");
                continue;
            }

            var part = (EquipmentPart)s.part;
            _slots[part] = entity;
            ApplyStats(entity);

            // Restore visual
            var appearance = entity.GetModule<AppearanceRuntime>();
            if (appearance != null)
                ApplyVisual(entity, appearance);
        }
        _pendingSlots = null;

        OnChanged?.Invoke();
    }

    private EquipSlotSave[] _pendingSlots;

    public bool Equals(IModuleRuntime other) => other is EquipmentRuntime;

    [Serializable] private class EquipmentSaveData { public EquipSlotSave[] entries; }
    [Serializable] private class EquipSlotSave { public int part; public string entityId; }
}
