using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSO", menuName = "CityGun/Map")]
public class MapSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<string, MapData> _maps = new();

    public MapData GetByTid(string tid)
    {
        if (TryGetByTid(tid, out var data))
            return data;

        Debug.LogError($"[MapSO] Tid가 없습니다: {tid}", this);
        return null;
    }

    public bool TryGetByTid(string tid, out MapData data)
    {
        return _maps.TryGetValue(tid, out data);
    }

    public bool TryGetRandom(out string tid, out MapData data)
    {
        tid = null;
        data = null;
        if (_maps.Count == 0)
            return false;

        var keys = new string[_maps.Count];
        _maps.Keys.CopyTo(keys, 0);
        tid = keys[Random.Range(0, keys.Length)];
        return _maps.TryGetValue(tid, out data);
    }
}

[System.Serializable]
public class MapData
{
    [SerializeField] private string _prefabAddress;

    public string PrefabAddress => _prefabAddress;
}
