/// <summary>
/// 획득한 아이템. Tid로 데이터를 찾고, 타입으로 어느 SO인지 고른다.
/// </summary>
public class ItemInfo
{
    public ItemInfo(string tid, ItemType type)
    {
        Tid = tid;
        Type = type;
    }

    public string Tid { get; }
    public ItemType Type { get; }
}

public enum ItemType
{
    None,
    Weapon
}
