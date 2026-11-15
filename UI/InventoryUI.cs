using System;
using System.Collections.Generic;
using SHIN;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 칸과 장착 중인 총을 현재 아이템 데이터로 채운다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    static readonly Color EmptySlot = new Color(0.165f, 0.180f, 0.204f, 1f);
    static readonly Color FilledSlot = new Color(0.22f, 0.24f, 0.27f, 1f);
    static readonly Color EquippedSlot = new Color(0.28f, 0.24f, 0.16f, 1f);
    static readonly Color LabelColor = new Color(0.93f, 0.91f, 0.86f, 1f);

    readonly List<ItemInfo> _items = new();
    readonly List<Transform> _slots = new();
    bool _closeHooked;

    public event Action CloseRequested;

    public void Bind(CharacterBase player)
    {
        HookCloseButton();
        CollectSlots();
        _items.Clear();
        if (player != null && player.Info != null)
            player.Info.CollectItems(_items);

        var equipped = player != null ? player.EquippedItem : null;
        if (_items.Count > _slots.Count)
            Debug.LogWarning($"[InventoryUI] 칸보다 아이템이 많습니다. 아이템={_items.Count}, 칸={_slots.Count}", this);

        Transform equippedSlot = null;
        for (var i = 0; i < _slots.Count; i++)
        {
            var item = i < _items.Count ? _items[i] : null;
            var isEquipped = item != null && item == equipped;
            FillSlot(_slots[i], item, isEquipped);
            if (isEquipped)
                equippedSlot = _slots[i];
        }

        PlaceEquippedFrame(equippedSlot);
        HidePlaceholderIcon();
        FillLoadout(player, equipped);
    }

    void HookCloseButton()
    {
        if (_closeHooked)
            return;

        var button = FindDeep(transform, "CloseButton")?.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(RequestClose);

        var dim = FindDeep(transform, "Dim");
        if (dim != null)
        {
            var dimButton = dim.GetComponent<Button>();
            if (dimButton == null)
                dimButton = dim.gameObject.AddComponent<Button>();

            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(RequestClose);
        }

        _closeHooked = true;
    }

    void RequestClose()
    {
        CloseRequested?.Invoke();
    }

    void CollectSlots()
    {
        if (_slots.Count > 0)
            return;

        var grid = FindDeep(transform, "Grid");
        if (grid == null)
        {
            Debug.LogError("[InventoryUI] Grid가 없습니다.", this);
            return;
        }

        for (var i = 0; i < grid.childCount; i++)
        {
            var child = grid.GetChild(i);
            if (child.name.StartsWith("Slot_"))
                _slots.Add(child);
        }
    }

    void FillSlot(Transform slot, ItemInfo item, bool equipped)
    {
        var image = slot.GetComponent<Image>();
        if (image != null)
            image.color = item == null ? EmptySlot : equipped ? EquippedSlot : FilledSlot;

        var label = GetOrCreateLabel(slot);
        label.gameObject.SetActive(item != null);
        if (item != null)
            label.text = item.Tid;
    }

    TextMeshProUGUI GetOrCreateLabel(Transform slot)
    {
        var existing = slot.Find("ItemLabel");
        if (existing != null && existing.TryGetComponent<TextMeshProUGUI>(out var found))
            return found;

        var labelObject = new GameObject("ItemLabel", typeof(RectTransform));
        labelObject.transform.SetParent(slot, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        var sample = FindDeep(transform, "WeaponName")?.GetComponent<TextMeshProUGUI>();
        if (sample != null)
        {
            label.font = sample.font;
            label.fontSharedMaterial = sample.fontSharedMaterial;
        }

        label.fontSize = 12f;
        label.color = LabelColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    void PlaceEquippedFrame(Transform equippedSlot)
    {
        var frame = FindDeep(transform, "EquippedFrame");
        if (frame == null)
            return;

        if (equippedSlot == null)
        {
            frame.gameObject.SetActive(false);
            return;
        }

        frame.SetParent(equippedSlot, false);
        frame.SetAsFirstSibling();
        var rect = frame as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
        }

        frame.gameObject.SetActive(true);
    }

    void HidePlaceholderIcon()
    {
        var icon = FindDeep(transform, "ItemIcon");
        if (icon != null)
            icon.gameObject.SetActive(false);
    }

    void FillLoadout(CharacterBase player, ItemInfo equipped)
    {
        var name = equipped != null ? equipped.Tid : "없음";
        var ammo = "";
        if (player != null && player.EquippedWeapon != null && equipped != null && equipped.Type == ItemType.Weapon)
            ammo = $"남은 탄 {player.EquippedWeapon.Ammo}";

        SetText("WeaponName", name);
        SetText("WeaponAmmo", ammo);
        SetText("Name", name);
        SetText("Ammo", ammo);
        SetText("Equipped", equipped != null ? "장착 중" : "");
    }

    void SetText(string objectName, string value)
    {
        var text = FindDeep(transform, objectName)?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = value;
    }

    static Transform FindDeep(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
