using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "CityGun/Item")]
public class ItemSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<string, ItemData> _items = new();

    public ItemData GetByTid(string tid)
    {
        if (TryGetByTid(tid, out var data))
            return data;

        Debug.LogError($"[ItemSO] Tid가 없습니다: {tid}", this);
        return null;
    }

    public bool TryGetByTid(string tid, out ItemData data)
    {
        return _items.TryGetValue(tid, out data);
    }
}

/// <summary>
/// 소모 아이템의 설계값. 사용처로 탄알, 회복 같은 역할을 구분한다.
/// </summary>
[System.Serializable]
public class ItemData
{
    [SerializeField] private string _tid;
    [SerializeField] private ItemUse _use = ItemUse.Ammo;
    [SerializeField] private int _maxCount = 30;

    public string Tid => _tid;
    public ItemUse Use => _use;

    /// <summary>
    /// 한 묶음에 들 수 있는 개수. 넘치면 다음 묶음으로 나뉜다.
    /// </summary>
    public int MaxCount => _maxCount > 0 ? _maxCount : 1;
}

public enum ItemUse
{
    Ammo
}
