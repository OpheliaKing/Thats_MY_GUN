using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        private const string ReloadSeAddress = "Assets/Addressable/Sound/SE/Gun/se_gun_reload_001.wav";

        private bool _reloading;

        private void BindReloadInput()
        {
            if (_input == null)
                return;

            _input.ReloadPerformed += OnReloadPerformed;
        }

        private void UnbindReloadInput()
        {
            if (_input == null)
                return;

            _input.ReloadPerformed -= OnReloadPerformed;
        }

        private void OnReloadPerformed()
        {
            TryReload();
        }

        /// <summary>
        /// 장착 중인 총의 탄알 Tid로, 비어 있는 칸만 채운다. 걷는 이동은 막지 않는다.
        /// </summary>
        private void TryReload()
        {
            if (_reloading || _info == null || _info.IsDead)
                return;

            if (_equippedWeapon == null || _equippedWeapon.Data == null)
            {
                Debug.Log("[CharacterBase] 장전할 무기가 없습니다.", this);
                return;
            }

            var data = _equippedWeapon.Data;
            if (_equippedWeapon.Ammo >= data.MaxMagazine)
                return;

            if (string.IsNullOrEmpty(data.AmmoTid))
            {
                Debug.Log($"[CharacterBase] 무기에 탄알 Tid가 없습니다. tid={_equippedItem?.Tid}", this);
                return;
            }

            if (_info.CountItem(data.AmmoTid) <= 0)
            {
                Debug.Log($"[CharacterBase] 장전할 탄알이 없습니다. tid={data.AmmoTid}", this);
                return;
            }

            _reloading = true;
            _equippedWeaponView?.SetFireSignal(false);
            var sound = GameManager.Instance != null ? GameManager.Instance.SoundManager : null;
            sound?.Play(SoundType.Se, ReloadSeAddress);
        }

        private void CancelReload()
        {
            _reloading = false;
        }

        private bool IsReloadClipDone()
        {
            if (_animator == null || _topLayerIndex < 0 || _animator.IsInTransition(_topLayerIndex))
                return false;

            var state = _animator.GetCurrentAnimatorStateInfo(_topLayerIndex);
            return state.shortNameHash == ReloadHash && state.normalizedTime >= 1f;
        }

        /// <summary>
        /// 장전 애니메이션이 끝난 뒤에 모자란 발수만 탄알에서 빼서 총에 넣는다.
        /// </summary>
        private void FinishReload()
        {
            _reloading = false;
            var weapon = _equippedWeapon;
            var data = weapon != null ? weapon.Data : null;
            if (weapon == null || data == null || _info == null)
                return;

            var need = data.MaxMagazine - weapon.Ammo;
            if (need <= 0 || string.IsNullOrEmpty(data.AmmoTid))
                return;

            var taken = _info.ConsumeItem(data.AmmoTid, need);
            if (taken <= 0)
            {
                Debug.Log($"[CharacterBase] 장전할 탄알이 없습니다. tid={data.AmmoTid}", this);
                return;
            }

            weapon.AddAmmo(taken);
            Debug.Log($"[CharacterBase] 장전. tid={data.AmmoTid}, +{taken}, 탄약={weapon.Ammo}/{data.MaxMagazine}, 남은 탄알={_info.CountItem(data.AmmoTid)}", this);
            if (_isFireHeld)
                _equippedWeaponView?.SetFireSignal(true);
        }
    }

    public enum TopPose
    {
        None,
        Shoot,
        Reload
    }
}
