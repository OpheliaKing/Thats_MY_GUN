using System.Collections.Generic;

public partial class CharacterInfo
{
    private readonly List<ItemInfo> _inventory = new();

    public ItemInfo EquippedItem { get; private set; }
    public int ItemCount => _inventory.Count;

    public bool ContainsItem(string tid)
    {
        return FindIndex(tid) >= 0;
    }

    public bool TryGetItem(string tid, out ItemInfo item)
    {
        var index = FindIndex(tid);
        if (index < 0)
        {
            item = null;
            return false;
        }

        item = _inventory[index];
        return item != null;
    }

    public int CountItem(string tid)
    {
        if (string.IsNullOrEmpty(tid))
            return 0;

        var count = 0;
        for (var i = 0; i < _inventory.Count; i++)
        {
            var item = _inventory[i];
            if (item != null && item.Tid == tid)
                count += item.Count;
        }

        return count;
    }

    /// <summary>
    /// 아이템을 넣는다. 탄알은 maxCount를 넘기면 다음 묶음으로 나눈다.
    /// </summary>
    public void AddItem(ItemInfo item, int maxCount = 0)
    {
        if (item == null || string.IsNullOrEmpty(item.Tid))
            return;

        if (item.Type != ItemType.Ammo)
        {
            var index = FindIndex(item.Tid);
            if (index >= 0)
                _inventory[index] = item;
            else
                _inventory.Add(item);
            return;
        }

        var remaining = item.Count;
        if (remaining <= 0)
            return;

        var limit = maxCount > 0 ? maxCount : int.MaxValue;
        var placedOriginal = false;
        for (var i = 0; i < _inventory.Count && remaining > 0; i++)
        {
            var stack = _inventory[i];
            if (stack == null || stack.Tid != item.Tid || stack.Type != ItemType.Ammo)
                continue;

            var room = limit - stack.Count;
            if (room <= 0)
                continue;

            var move = room < remaining ? room : remaining;
            stack.SetCount(stack.Count + move);
            remaining -= move;
        }

        while (remaining > 0)
        {
            var stackCount = limit < remaining ? limit : remaining;
            if (!placedOriginal)
            {
                item.SetCount(stackCount);
                _inventory.Add(item);
                placedOriginal = true;
            }
            else
            {
                _inventory.Add(new ItemInfo(item.Tid, item.Type, stackCount));
            }

            remaining -= stackCount;
        }

        if (!placedOriginal)
            item.SetCount(0);
    }

    /// <summary>
    /// 같은 Tid 묶음에서 개수를 뺀다. 빈 묶음은 인벤토리에서 지운다.
    /// </summary>
    public int ConsumeItem(string tid, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(tid))
            return 0;

        var consumed = 0;
        for (var i = _inventory.Count - 1; i >= 0 && consumed < amount; i--)
        {
            var stack = _inventory[i];
            if (stack == null || stack.Tid != tid)
                continue;

            var take = stack.Count < amount - consumed ? stack.Count : amount - consumed;
            stack.SetCount(stack.Count - take);
            consumed += take;
            if (stack.Count <= 0)
                _inventory.RemoveAt(i);
        }

        return consumed;
    }

    public void CollectItems(List<ItemInfo> result)
    {
        if (result == null)
            return;

        result.Clear();
        for (var i = 0; i < _inventory.Count; i++)
        {
            if (_inventory[i] != null)
                result.Add(_inventory[i]);
        }
    }

    public void SetEquippedItem(ItemInfo item)
    {
        EquippedItem = item;
    }

    private int FindIndex(string tid)
    {
        if (string.IsNullOrEmpty(tid))
            return -1;

        for (var i = 0; i < _inventory.Count; i++)
        {
            var item = _inventory[i];
            if (item != null && item.Tid == tid)
                return i;
        }

        return -1;
    }
}
