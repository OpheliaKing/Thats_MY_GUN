/// <summary>
/// 획득한 아이템. Tid로 데이터를 찾고, 타입으로 어느 SO인지 고른다.
/// </summary>
public class ItemInfo
{
    public ItemInfo(string tid, ItemType type, int count = 1)
    {
        Tid = tid;
        Type = type;
        SetCount(count);
    }

    public string Tid { get; }
    public ItemType Type { get; }
    public int Count { get; private set; }

    public void SetCount(int count)
    {
        Count = count > 0 ? count : 0;
    }
}

public enum ItemType
{
    None,
    Weapon,
    Ammo
}
