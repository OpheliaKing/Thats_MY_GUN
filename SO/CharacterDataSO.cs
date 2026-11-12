using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "CityGun/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<string, CharacterData> _characters = new();

    public CharacterData GetByTid(string tid)
    {
        if (TryGetByTid(tid, out var data))
            return data;

        Debug.LogError($"[CharacterDataSO] Tid가 없습니다: {tid}", this);
        return null;
    }

    public bool TryGetByTid(string tid, out CharacterData data)
    {
        return _characters.TryGetValue(tid, out data);
    }
}

[System.Serializable]
public class CharacterData
{
    [SerializeField] private string _name;
    [SerializeField] private string _prefabAddress;
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _moveSpeed = 5f;

    public string Name => _name;
    public string PrefabAddress => _prefabAddress;
    public float MaxHp => _maxHp;
    public float MoveSpeed => _moveSpeed;
}
