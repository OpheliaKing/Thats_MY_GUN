using System.Threading.Tasks;
using SHIN;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

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
    private const string InventoryAddress = "Assets/Addressable/Prafab/UI/InGame/InventoryUI.prefab";

    private MapSO _maps;
    private CharacterDataSO _characters;
    private WeaponSO _weapons;
    private ProjectilePool _projectilePool;
    private CharacterBase _player;
    private PlayerCamera _playerCamera;
    private CombatManager _combat;
    private UIBase _inventory;
    private bool _inventoryBusy;
    private bool _inventoryInputBound;

    public string MapTid { get; private set; }
    public bool IsRunning { get; private set; }
    public CombatManager Combat => EnsureManager(ref _combat);

    private void OnEnable()
    {
        BindInventoryInput();
    }

    private void OnDisable()
    {
        UnbindInventoryInput();
    }

    public async Task StartGameAsync()
    {
        BindInventoryInput();
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

        BuildMapNavMesh(instance);

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
        player.SetAppearance(CharacterPreset.Default);
        await ApplyTempStartAsync(resources, playerData, player);
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
        _inventory = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UIManager.CloseAll();
            GameManager.Instance.InputManager.EnableMap(InputManager.Map.Player);
        }

        resources?.ReleaseScope(RaidScope);
    }

    private void BindInventoryInput()
    {
        if (_inventoryInputBound || GameManager.Instance == null)
            return;

        GameManager.Instance.InputManager.InventoryPerformed += OnInventoryPerformed;
        _inventoryInputBound = true;
    }

    private void UnbindInventoryInput()
    {
        if (!_inventoryInputBound || GameManager.Instance == null)
            return;

        GameManager.Instance.InputManager.InventoryPerformed -= OnInventoryPerformed;
        _inventoryInputBound = false;
    }

    private async void OnInventoryPerformed()
    {
        if (!IsRunning || _inventoryBusy)
            return;

        if (_inventory != null)
        {
            CloseInventory();
            return;
        }

        _inventoryBusy = true;
        try
        {
            await OpenInventoryAsync();
        }
        finally
        {
            _inventoryBusy = false;
        }
    }

    private async Task OpenInventoryAsync()
    {
        var ui = GameManager.Instance.UIManager;
        var opened = await ui.Show(InventoryAddress, UIType.Common);
        if (!IsRunning || opened == null)
            return;

        var view = opened.GetComponent<InventoryUI>();
        if (view == null)
            view = opened.gameObject.AddComponent<InventoryUI>();

        view.CloseRequested -= CloseInventory;
        view.CloseRequested += CloseInventory;
        view.Bind(_player);
        _inventory = opened;
        GameManager.Instance.InputManager.EnableMap(InputManager.Map.UI);
    }

    private void CloseInventory()
    {
        if (_inventory == null)
            return;

        var view = _inventory.GetComponent<InventoryUI>();
        if (view != null)
            view.CloseRequested -= CloseInventory;

        GameManager.Instance.UIManager.Close();
        _inventory = null;
        GameManager.Instance.InputManager.EnableMap(InputManager.Map.Player);
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

    private static void BuildMapNavMesh(GameObject map)
    {
        var surface = map.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = map.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
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
    /// 임시. 시작 무기, 탄알, 적 한 명. 이 함수만 지우면 임시 배치가 사라진다.
    /// </summary>
    private async Task ApplyTempStartAsync(ResourceManager resources, CharacterData playerData, CharacterBase player)
    {
        const string TempWeaponTid = "weapon_assault_rifle_001";
        const string TempAmmoTid = "ammo_rifle_001";
        const int TempAmmoCount = 60;
        var tempEnemyPosition = new Vector3(2f, 0f, 0f);

        await GiveTempWeaponAsync(player);
        await GiveTempAmmoAsync(player);
        await SpawnTempEnemyAsync();

        async Task GiveTempWeaponAsync(CharacterBase character)
        {
            if (character == null || character.Info == null)
                return;

            var weapons = await GetWeaponsAsync(resources);
            var item = new ItemInfo(TempWeaponTid, ItemType.Weapon);
            if (weapons == null || !weapons.TryGetByTid(item.Tid, out var weaponData))
            {
                Debug.LogError($"[InGameManager] 임시 무기를 찾지 못했습니다. tid={TempWeaponTid}");
                return;
            }

            await character.AcquireItem(item, resources, _projectilePool, weaponData);
        }

        async Task GiveTempAmmoAsync(CharacterBase character)
        {
            if (character == null || character.Info == null)
                return;

            var ammo = new ItemInfo(TempAmmoTid, ItemType.Ammo, TempAmmoCount);
            await character.AcquireItem(ammo, resources, _projectilePool);
        }

        async Task SpawnTempEnemyAsync()
        {
            if (resources == null || playerData == null || string.IsNullOrEmpty(playerData.PrefabAddress))
            {
                Debug.LogError("[InGameManager] 임시 적을 만들 데이터가 없습니다.");
                return;
            }

            var enemyObject = await resources.InstantiateAsync(playerData.PrefabAddress, startInactive: false, worldPosition: tempEnemyPosition);
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

            enemy.InitCharacter(CharacterType.Enemy, playerData);
            enemy.SetAppearance(CharacterPreset.Cowboy);
            enemy.transform.SetPositionAndRotation(tempEnemyPosition, Quaternion.identity);
            var body = enemy.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = tempEnemyPosition;
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var ai = enemy.gameObject.AddComponent<EnemyAI>();
            ai.Bind(enemy, player);
            await GiveTempWeaponAsync(enemy);
            Debug.Log($"[InGameManager] 임시 적 생성. pos={tempEnemyPosition}", enemy);
        }
    }
}
