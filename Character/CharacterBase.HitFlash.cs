using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        private const float HitFlashDuration = 0.16f;
        private const string HitLayerName = "Hit";

        private static readonly int HitId = Shader.PropertyToID("_Hit");
        private static readonly int HitStateHash = Animator.StringToHash("GetHit");

        private readonly List<SkinnedMeshRenderer> _hitRenderers = new List<SkinnedMeshRenderer>();
        private MaterialPropertyBlock _hitBlock;
        private int _hitSerial;
        private int _hitLayerIndex = -1;
        private bool _hitPlaying;

        /// <summary>
        /// 맞은 머리와 몸만 잠깐 붉게 올렸다가 되돌린다. 공유 머티리얼은 바꾸지 않는다.
        /// </summary>
        private void PlayHitFlash()
        {
            CacheHitRenderers();
            if (_hitRenderers.Count > 0)
            {
                _hitSerial++;
                StartCoroutine(PlayHitFlashRoutine(_hitSerial));
            }

            PlayHitAnimation();
        }

        private void ResetHitFlash()
        {
            _hitSerial++;
            ApplyHitAmount(0f);
            _hitRenderers.Clear();
            StopHitLayer();
        }

        /// <summary>
        /// 히트 레이어만 재생한다. Base 이동과 Top 사격 웨이트는 건드리지 않는다.
        /// </summary>
        private void PlayHitAnimation()
        {
            if (_animator == null || _info == null || _info.IsDead)
                return;

            if (_hitLayerIndex < 0)
            {
                _hitLayerIndex = _animator.GetLayerIndex(HitLayerName);
                if (_hitLayerIndex < 0)
                    return;
            }

            _hitPlaying = true;
            _animator.SetLayerWeight(_hitLayerIndex, 1f);
            _animator.CrossFade(HitStateHash, 0.05f, _hitLayerIndex, 0f);
        }

        private void UpdateHitLayer()
        {
            if (_animator == null || !_hitPlaying)
                return;

            if (_hitLayerIndex < 0)
                _hitLayerIndex = _animator.GetLayerIndex(HitLayerName);
            if (_hitLayerIndex < 0)
                return;

            if (_info != null && _info.IsDead)
            {
                StopHitLayer();
                return;
            }

            if (_animator.IsInTransition(_hitLayerIndex))
                return;

            var state = _animator.GetCurrentAnimatorStateInfo(_hitLayerIndex);
            if (state.shortNameHash == HitStateHash && state.normalizedTime >= 1f)
                StopHitLayer();
        }

        private void StopHitLayer()
        {
            _hitPlaying = false;
            if (_animator == null)
                return;

            if (_hitLayerIndex < 0)
                _hitLayerIndex = _animator.GetLayerIndex(HitLayerName);
            if (_hitLayerIndex >= 0)
                _animator.SetLayerWeight(_hitLayerIndex, 0f);
        }

        private IEnumerator PlayHitFlashRoutine(int serial)
        {
            ApplyHitAmount(1f);
            var elapsed = 0f;
            while (elapsed < HitFlashDuration)
            {
                if (serial != _hitSerial)
                    yield break;

                elapsed += Time.deltaTime;
                var fade = 1f - Mathf.Clamp01(elapsed / HitFlashDuration);
                ApplyHitAmount(fade);
                yield return null;
            }

            if (serial == _hitSerial)
                ApplyHitAmount(0f);
        }

        private void CacheHitRenderers()
        {
            if (_hitRenderers.Count > 0 || _appearance == null)
                return;

            var renderers = _appearance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer.gameObject.activeInHierarchy && IsAppearancePart(renderer.gameObject.name))
                    _hitRenderers.Add(renderer);
            }
        }

        private void ApplyHitAmount(float amount)
        {
            if (_hitRenderers.Count == 0)
                return;

            if (_hitBlock == null)
                _hitBlock = new MaterialPropertyBlock();

            for (var i = 0; i < _hitRenderers.Count; i++)
            {
                var renderer = _hitRenderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_hitBlock);
                _hitBlock.SetFloat(HitId, amount);
                renderer.SetPropertyBlock(_hitBlock);
            }
        }
    }
}
