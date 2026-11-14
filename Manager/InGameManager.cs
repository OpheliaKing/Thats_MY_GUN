using System.Threading.Tasks;
using SHIN;
using UnityEngine;

/// <summary>
/// 한 판의 시작, 중단, 맵 로드를 담당한다.
/// </summary>
public class InGameManager : ManagerBase
{
    private const string RaidScope = "raid";
    private const string MapCatalogAddress = "Assets/Addressable/SO/MapSO.asset";
    private const string CharacterCatalogAddress = "Assets/Addressable/SO/CharacterData.asset";
    private const string WeaponCatalogAddress = "Assets/Addressable/SO/WeaponSO.asset";
    private const string PlayerTid = "char_player_001";
    private const string TempStartWeaponTid = "weapon_assault_rifle_001";
    private static readonly Vector3 TempEnemyPosition = new Vector3(2f, 0f, 0f);

    private MapSO _maps;
    private CharacterDataSO _characters;
    private WeaponSO _weapons;
    private ProjectilePool _projectilePool;
    private CharacterBase _player;
    private PlayerCamera _playerCamera;
    private CombatManager _combat;

    public string MapTid { get; private set; }
    public bool IsRunning { get; private set; }
    public CombatManager Combat => EnsureManager(ref _combat);

    public async Task StartGameAsync()
    {
        if (IsRunning)
        {
            Debug.LogWarning("[InGameManager] 이미 인게임입니다.");
            return;
        }

        var resources = GameManager.Instance != null ? GameManager.Instance.ResourceManager : null;
        if (resources == null)
        {
            Debug.LogError("[InGameManager] ResourceManager가 없습니다.");
            return;
        }

        var maps = await GetMapsAsync(resources);
        if (maps == null || !maps.TryGetRandom(out var tid, out var map) || string.IsNullOrEmpty(map.PrefabAddress))
        {
            Debug.LogError("[InGameManager] 불러올 맵이 없습니다.");
            return;
        }

        var characters = await GetCharactersAsync(resources);
        if (characters == null || !characters.TryGetByTid(PlayerTid, out var playerData) || string.IsNullOrEmpty(playerData.PrefabAddress))
        {
            Debug.LogError("[InGameManager] 플레이어 데이터가 없습니다.");
            return;
        }

        resources.BeginScope(RaidScope);
        _projectilePool?.Clear();
        _projectilePool = new ProjectilePool(transform, resources);
        var instance = await resources.InstantiateAsync(map.PrefabAddress, startInactive: false);
        if (instance == null)
        {
            EndRaid(resources);
            Debug.LogError($"[InGameManager] 맵 생성 실패. tid={tid}");
            return;
        }

        // ===== 플레이어 스폰 위치 =====
        // 지금은 원점. 나중에 맵의 스폰 지점으로 교체한다.
        var playerSpawnPosition = Vector3.zero;

        var playerObject = await resources.InstantiateAsync(playerData.PrefabAddress, startInactive: false, worldPosition: playerSpawnPosition);
        if (playerObject == null)
        {
            EndRaid(resources);
            Debug.LogError("[InGameManager] 플레이어 생성 실패.");
            return;
        }

        var player = playerObject.GetComponent<CharacterBase>();
        if (player == null)
        {
            EndRaid(resources);
            Debug.LogError("[InGameManager] 플레이어에 CharacterBase가 없습니다.");
            return;
        }

        player.InitCharacter(CharacterType.Player, playerData);
        await GiveTempStartWeaponAsync(player, resources);
        player.SetAppearance(CharacterPreset.Default);
        await SpawnTempEnemyAsync(resources, playerData);
        BindPlayerCamera(player);
        BindPlayerInput(player);
        MapTid = tid;
        IsRunning = true;
    }

    public void Exit()
    {
        if (!IsRunning)
            return;

        BindPlayerInput(null);
        BindPlayerCamera(null);

        EndRaid(GameManager.Instance != null ? GameManager.Instance.ResourceManager : null);

        IsRunning = false;
    }

    private void EndRaid(ResourceManager resources)
    {
        _projectilePool?.Clear();
        _projectilePool = null;
        resources?.ReleaseScope(RaidScope);
    }

    /// <summary>
    /// 스폰한 플레이어에게만 입력 콜백을 붙인다. 이전 플레이어는 구독을 해제한다.
    /// </summary>
    public void BindPlayerInput(CharacterBase player)
    {
        if (_player != null)
            _player.UnbindInput();

        _player = player;
        if (_player == null)
            return;

        var input = GameManager.Instance != null ? GameManager.Instance.InputManager : null;
        if (input == null)
        {
            Debug.LogError("[InGameManager] InputManager가 없습니다.");
            return;
        }

        _player.BindInput(input);
    }

    /// <summary>
    /// 메인 카메라가 이 플레이어를 따라가게 한다. 판이 끝나면 대상을 비운다.
    /// </summary>
    private void BindPlayerCamera(CharacterBase player)
    {
        if (player == null)
        {
            if (_playerCamera != null)
                _playerCamera.SetTarget(null);
            return;
        }

        _playerCamera = EnsurePlayerCamera();
        if (_playerCamera == null)
            return;

        _playerCamera.SetTarget(player.transform);
    }

    private PlayerCamera EnsurePlayerCamera()
    {
        if (_playerCamera != null)
            return _playerCamera;

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("[InGameManager] Main Camera가 없습니다.");
            return null;
        }

        var playerCamera = camera.GetComponent<PlayerCamera>();
        if (playerCamera == null)
            playerCamera = camera.gameObject.AddComponent<PlayerCamera>();

        return playerCamera;
    }

    private async Task<MapSO> GetMapsAsync(ResourceManager resources)
    {
        if (_maps != null)
            return _maps;

        _maps = await resources.LoadAsync<MapSO>(MapCatalogAddress);
        return _maps;
    }

    private async Task<CharacterDataSO> GetCharactersAsync(ResourceManager resources)
    {
        if (_characters != null)
            return _characters;

        _characters = await resources.LoadAsync<CharacterDataSO>(CharacterCatalogAddress);
        return _characters;
    }

    private async Task<WeaponSO> GetWeaponsAsync(ResourceManager resources)
    {
        if (_weapons != null)
            return _weapons;

        _weapons = await resources.LoadAsync<WeaponSO>(WeaponCatalogAddress);
        return _weapons;
    }

    /// <summary>
    /// 임시. 인게임 시작 시 (2, 0, 0)에 적을 한 명 만든다. 적 데이터가 없어서 플레이어 프리팹을 쓴다.
    /// </summary>
    private async Task SpawnTempEnemyAsync(ResourceManager resources, CharacterData data)
    {
        if (resources == null || data == null || string.IsNullOrEmpty(data.PrefabAddress))
        {
            Debug.LogError("[InGameManager] 임시 적을 만들 데이터가 없습니다.");
            return;
        }

        var enemyObject = await resources.InstantiateAsync(data.PrefabAddress, startInactive: false, worldPosition: TempEnemyPosition);
        if (enemyObject == null)
        {
            Debug.LogError("[InGameManager] 임시 적 생성 실패.");
            return;
        }

        var enemy = enemyObject.GetComponent<CharacterBase>();
        if (enemy == null)
        {
            Debug.LogError("[InGameManager] 임시 적에 CharacterBase가 없습니다.");
            return;
        }

        enemy.InitCharacter(CharacterType.Enemy, data);
        enemy.SetAppearance(CharacterPreset.Cowboy);
        enemy.transform.SetPositionAndRotation(TempEnemyPosition, Quaternion.identity);
        var body = enemy.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = TempEnemyPosition;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[InGameManager] 임시 적 생성. pos={TempEnemyPosition}", enemy);
    }

    /// <summary>
    /// 임시. 캐릭터를 처음 만들 때 돌격소총을 쥐여 준다.
    /// </summary>
    private async Task GiveTempStartWeaponAsync(CharacterBase player, ResourceManager resources)
    {
        if (player == null || player.Info == null)
            return;

        var weapons = await GetWeaponsAsync(resources);
        var item = new ItemInfo(TempStartWeaponTid, ItemType.Weapon);
        if (!TryGetItemData(weapons, item, out var weaponData))
        {
            Debug.LogError($"[InGameManager] 임시 무기를 찾지 못했습니다. tid={TempStartWeaponTid}");
            return;
        }

        await player.AcquireItem(item, resources, _projectilePool, weaponData);
    }

    private static bool TryGetItemData(WeaponSO weapons, ItemInfo item, out WeaponData weaponData)
    {
        weaponData = null;
        if (item == null)
            return false;

        switch (item.Type)
        {
            case ItemType.Weapon:
                return weapons != null && weapons.TryGetByTid(item.Tid, out weaponData);
            default:
                return false;
        }
    }
}
