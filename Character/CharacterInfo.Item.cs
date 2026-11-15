using System.Collections.Generic;

public partial class CharacterInfo
{
    private readonly Dictionary<string, ItemInfo> _inventory = new();

    public ItemInfo EquippedItem { get; private set; }
    public int ItemCount => _inventory.Count;

    public bool ContainsItem(string tid)
    {
        return !string.IsNullOrEmpty(tid) && _inventory.ContainsKey(tid);
    }

    public bool TryGetItem(string tid, out ItemInfo item)
    {
        if (string.IsNullOrEmpty(tid))
        {
            item = null;
            return false;
        }

        return _inventory.TryGetValue(tid, out item);
    }

    public void AddItem(ItemInfo item)
    {
        if (item == null || string.IsNullOrEmpty(item.Tid))
            return;

        _inventory[item.Tid] = item;
    }

    public void CollectItems(List<ItemInfo> result)
    {
        if (result == null)
            return;

        result.Clear();
        foreach (var pair in _inventory)
        {
            if (pair.Value != null)
                result.Add(pair.Value);
        }
    }

     public void SetEquippedItem(ItemInfo item)
    {
        EquippedItem = item;
    }
}
