using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        private const string CharacterLitShader = "SHIN/CharacterLit";
        private const float DeathDissolveDuration = 2f;

        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int DissolveMinId = Shader.PropertyToID("_DissolveMin");
        private static readonly int DissolveSpanId = Shader.PropertyToID("_DissolveSpan");

        private readonly List<Material> _dissolveMaterials = new List<Material>();
        private readonly List<Material> _dissolveOriginals = new List<Material>();
        private readonly List<SkinnedMeshRenderer> _dissolveRenderers = new List<SkinnedMeshRenderer>();
        private bool _deathDissolvePlayed;
        private int _dissolveSerial;

        /// <summary>
        /// 적의 켜진 머리와 몸만 공용 셰이더 인스턴스를 쓴다. 디졸브 값은 0이라 재질은 그대로다.
        /// </summary>
        private void PrepareEnemyDeathDissolve()
        {
            if (_characterType != CharacterType.Enemy || _appearance == null || _dissolveRenderers.Count > 0)
                return;

            var shader = Shader.Find(CharacterLitShader);
            if (shader == null)
            {
                Debug.LogWarning("[CharacterBase] 캐릭터 셰이더를 찾지 못했습니다.", this);
                return;
            }

            var renderers = _appearance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.gameObject.activeInHierarchy || !IsAppearancePart(renderer.gameObject.name))
                    continue;

                var source = renderer.sharedMaterial;
                if (source == null)
                    continue;

                var copy = new Material(source);
                copy.shader = shader;
                copy.SetFloat(DissolveId, 0f);
                renderer.sharedMaterial = copy;
                _dissolveRenderers.Add(renderer);
                _dissolveOriginals.Add(source);
                _dissolveMaterials.Add(copy);
            }

            if (_dissolveRenderers.Count == 0)
                Debug.LogWarning("[CharacterBase] 디졸브할 머리·몸이 없습니다.", this);
        }

        private void PlayEnemyDeathDissolve()
        {
            if (_characterType != CharacterType.Enemy || _dissolveRenderers.Count == 0 || _deathDissolvePlayed)
                return;

            _deathDissolvePlayed = true;
            _dissolveSerial++;
            StartCoroutine(PlayDeathDissolves(_dissolveSerial));
        }

        private IEnumerator PlayDeathDissolves(int serial)
        {
            yield return WaitForDeathPose();
            if (serial != _dissolveSerial)
                yield break;

            for (var i = 0; i < _dissolveRenderers.Count; i++)
            {
                var renderer = _dissolveRenderers[i];
                var material = _dissolveMaterials[i];
                if (renderer == null || material == null)
                    continue;

                var bounds = renderer.bounds;
                material.SetFloat(DissolveMinId, bounds.min.y);
                material.SetFloat(DissolveSpanId, Mathf.Max(bounds.size.y, 0.2f));
                material.SetFloat(DissolveId, 0f);
            }

            var elapsed = 0f;
            while (elapsed < DeathDissolveDuration)
            {
                if (serial != _dissolveSerial)
                    yield break;

                elapsed += Time.deltaTime;
                var amount = Mathf.Clamp01(elapsed / DeathDissolveDuration);
                for (var i = 0; i < _dissolveMaterials.Count; i++)
                {
                    if (_dissolveMaterials[i] != null)
                        _dissolveMaterials[i].SetFloat(DissolveId, amount);
                }

                yield return null;
            }

            if (serial != _dissolveSerial)
                yield break;

            for (var i = 0; i < _dissolveRenderers.Count; i++)
            {
                if (_dissolveRenderers[i] != null)
                    _dissolveRenderers[i].enabled = false;
            }
        }

        private IEnumerator WaitForDeathPose()
        {
            const int layer = 0;
            var elapsed = 0f;
            while (elapsed < 4f)
            {
                if (_animator == null)
                    yield break;

                if (!_animator.IsInTransition(layer))
                {
                    var state = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (state.shortNameHash == DieHash && state.normalizedTime >= 1f)
                        yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 디졸브로 바뀐 머티리얼과 꺼진 렌더러를 초기 상태로 되돌린다.
        /// </summary>
        private void ResetDeathDissolve()
        {
            _dissolveSerial++;
            _deathDissolvePlayed = false;

            for (var i = 0; i < _dissolveRenderers.Count; i++)
            {
                var renderer = _dissolveRenderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = true;
                if (i < _dissolveOriginals.Count && _dissolveOriginals[i] != null)
                    renderer.sharedMaterial = _dissolveOriginals[i];
            }

            for (var i = 0; i < _dissolveMaterials.Count; i++)
            {
                if (_dissolveMaterials[i] != null)
                    Destroy(_dissolveMaterials[i]);
            }

            _dissolveRenderers.Clear();
            _dissolveOriginals.Clear();
            _dissolveMaterials.Clear();
        }

        private void ReleaseEnemyDeathDissolve()
        {
            ResetDeathDissolve();
        }

        private static bool IsAppearancePart(string objectName)
        {
            return StartsWithDigits(objectName, "Head") || StartsWithDigits(objectName, "Body");
        }

        private static bool StartsWithDigits(string objectName, string prefix)
        {
            if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith(prefix) || objectName.Length == prefix.Length)
                return false;

            for (var i = prefix.Length; i < objectName.Length; i++)
            {
                if (!char.IsDigit(objectName[i]))
                    return false;
            }

            return true;
        }
    }
}
