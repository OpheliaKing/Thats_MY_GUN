using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        [SerializeField] private Transform _weaponPoint;

        private readonly Dictionary<string, CachedWeapon> _weapons = new();

        private ItemInfo _equippedItem;
        private WeaponInfo _equippedWeapon;
        private GameObject _equippedWeaponObject;
        private WeaponObject _equippedWeaponView;
        private ProjectilePool _projectilePool;

        public Transform WeaponPoint => _weaponPoint;
        public ItemInfo EquippedItem => _equippedItem;
        public WeaponInfo EquippedWeapon => _equippedWeapon;
        public GameObject EquippedWeaponObject => _equippedWeaponObject;
        public bool IsFireHeld => _isFireHeld;

        private bool _isFireHeld;

        private void BindFireInput()
        {
            if (_input == null)
                return;

            _input.LeftClickStarted += OnFireInputStarted;
            _input.LeftClickCanceled += OnFireInputCanceled;
            if (_input.IsLeftClickHeld)
                OnFireInputStarted();
        }

        private void UnbindFireInput()
        {
            if (_input == null)
                return;

            _input.LeftClickStarted -= OnFireInputStarted;
            _input.LeftClickCanceled -= OnFireInputCanceled;
            _isFireHeld = false;
            _equippedWeaponView?.SetFireSignal(false);
        }

        private void OnFireInputStarted()
        {
            _isFireHeld = true;
            _equippedWeaponView?.SetFireSignal(true);
            Debug.Log("[CharacterBase] 발사 입력 시작.", this);
        }

        private void OnFireInputCanceled()
        {
            _isFireHeld = false;
            _equippedWeaponView?.SetFireSignal(false);
            Debug.Log("[CharacterBase] 발사 입력 해제.", this);
        }

        public async Task EquipWeapon(ItemInfo item, WeaponData weaponData, ResourceManager resources, ProjectilePool projectilePool)
        {
            if (_info == null)
            {
                Debug.LogError("[CharacterBase] 캐릭터 정보가 없습니다.", this);
                return;
            }

            if (item == null || string.IsNullOrEmpty(item.Tid))
            {
                Debug.LogError("[CharacterBase] 장착할 무기가 없습니다.", this);
                return;
            }

            if (weaponData == null)
            {
                Debug.LogError($"[CharacterBase] 무기 데이터가 없습니다. tid={item.Tid}", this);
                return;
            }

            if (projectilePool != null)
                _projectilePool = projectilePool;

            _projectilePool?.Ensure(weaponData.ProjectileAddress);

            if (_weaponPoint == null)
            {
                Debug.LogError("[CharacterBase] 무기 장착 위치가 없습니다.", this);
                return;
            }

            if (_equippedItem != null && _equippedItem.Tid == item.Tid && _equippedWeaponObject != null)
            {
                _equippedWeaponObject.SetActive(true);
                Debug.Log($"[CharacterBase] 같은 무기가 이미 장착 중. tid={item.Tid}", this);
                return;
            }

            if (!_info.ContainsItem(item.Tid))
                _info.AddItem(item);

            if (!_weapons.TryGetValue(item.Tid, out var cachedWeapon) || cachedWeapon == null)
            {
                cachedWeapon = new CachedWeapon();
                _weapons[item.Tid] = cachedWeapon;
            }

            if (cachedWeapon.Info == null)
                cachedWeapon.Info = new WeaponInfo(weaponData);

            var weapon = cachedWeapon.Info;
            var instance = cachedWeapon.Object;
            var cached = instance != null;
            if (!cached)
            {
                if (resources == null)
                {
                    Debug.LogError("[CharacterBase] ResourceManager가 없습니다.", this);
                    return;
                }

                if (string.IsNullOrEmpty(weaponData.PrefabAddress))
                {
                    Debug.LogError($"[CharacterBase] 무기 프리팹 주소가 없습니다. tid={item.Tid}", this);
                    return;
                }

                instance = await resources.InstantiateAsync(weaponData.PrefabAddress, _weaponPoint, startInactive: true);
                if (instance == null)
                    return;

                cachedWeapon.Object = instance;
            }

            if (_equippedItem != null)
                UnEquipWeapon();

            instance.transform.SetParent(_weaponPoint, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            _equippedItem = item;
            _equippedWeapon = weapon;
            _equippedWeaponObject = instance;
            BindEquippedWeaponView(instance, weapon);
            _info.SetEquippedItem(item);
            _info.EquipWeapon(weapon);
            Debug.Log($"[CharacterBase] 무기 장착. tid={item.Tid}, prefab={weaponData.PrefabAddress}, cached={cached}, projectile={weaponData.ProjectileAddress}, 탄약={weapon.Ammo}/{weaponData.MaxMagazine}, 탄속={weaponData.BulletSpeed}, 연사={weaponData.FireRate}, 피해={weaponData.Damage}", this);
        }

        public void UnEquipWeapon()
        {
            var previousTid = _equippedItem != null ? _equippedItem.Tid : string.Empty;
            if (_equippedWeaponView != null)
                _equippedWeaponView.SetFireSignal(false);

            if (_equippedWeaponObject != null)
                _equippedWeaponObject.SetActive(false);

            _equippedWeaponView = null;
            _equippedWeaponObject = null;
            _equippedWeapon = null;
            _equippedItem = null;

            if (_info != null)
                _info.UnEquipWeapon();

            if (!string.IsNullOrEmpty(previousTid))
                Debug.Log($"[CharacterBase] 무기 해제. tid={previousTid}", this);
        }

        private void BindEquippedWeaponView(GameObject instance, WeaponInfo weapon)
        {
            var view = instance.GetComponentInChildren<WeaponObject>(true);
            if (view == null)
                view = instance.AddComponent<WeaponObject>();

            view.Bind(weapon, transform, _projectilePool);
            _equippedWeaponView = view;
            if (_isFireHeld)
                view.SetFireSignal(true);
        }

        private sealed class CachedWeapon
        {
            public WeaponInfo Info;
            public GameObject Object;
        }
    }
}
