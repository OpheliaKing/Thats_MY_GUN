using System.Threading.Tasks;
using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        public async Task AcquireItem(ItemInfo item, ResourceManager resources, ProjectilePool projectilePool, WeaponData weaponData = null)
        {
            if (_info == null)
            {
                Debug.LogError("[CharacterBase] 캐릭터 정보가 없습니다.", this);
                return;
            }

            if (item == null || string.IsNullOrEmpty(item.Tid))
            {
                Debug.LogError("[CharacterBase] 획득할 아이템이 없습니다.", this);
                return;
            }

            if (projectilePool != null)
                _projectilePool = projectilePool;

            if (item.Type == ItemType.Weapon && weaponData != null)
                _projectilePool?.Ensure(weaponData.ProjectileAddress);

            var replaced = _info.ContainsItem(item.Tid);
            _info.AddItem(item);
            Debug.Log($"[CharacterBase] 아이템 획득. tid={item.Tid}, type={item.Type}, 인벤토리={_info.ItemCount}, {(replaced ? "같은 Tid를 덮어씀" : "새로 넣음")}", this);

            if (_info.EquippedItem != null)
            {
                Debug.Log($"[CharacterBase] 이미 장착 중이라 장착하지 않음. 장착중={_info.EquippedItem.Tid}, 획득={item.Tid}", this);
                return;
            }

            if (item.Type == ItemType.Weapon)
                await EquipWeapon(item, weaponData, resources, _projectilePool);
        }
    }
}
