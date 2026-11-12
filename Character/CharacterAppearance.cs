using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    public enum CharacterPreset
    {
        Default
    }

    /// <summary>
    /// 모델의 헤드와 바디를 켠다. 무기 오브젝트는 건드리지 않는다.
    /// </summary>
    public class CharacterAppearance : MonoBehaviour
    {
        private readonly struct PartNames
        {
            public PartNames(string head, string body)
            {
                Head = head;
                Body = body;
            }

            public string Head { get; }
            public string Body { get; }
        }

        private static readonly Dictionary<CharacterPreset, PartNames> Presets = new()
        {
            { CharacterPreset.Default, new PartNames("Head14", "Body14") }
        };

        public void Apply(CharacterPreset preset)
        {
            if (!Presets.TryGetValue(preset, out var parts))
            {
                Debug.LogError($"[CharacterAppearance] 프리셋이 없습니다: {preset}", this);
                return;
            }

            ApplyCustom(parts.Head, parts.Body);
        }

        public void ApplyCustom(string headName, string bodyName)
        {
            var foundHead = false;
            var foundBody = false;
            var parts = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part == transform)
                    continue;

                if (IsPart(part.name, "Head"))
                {
                    var enabled = part.name == headName;
                    part.gameObject.SetActive(enabled);
                    if (enabled)
                        foundHead = true;
                }
                else if (IsPart(part.name, "Body"))
                {
                    var enabled = part.name == bodyName;
                    part.gameObject.SetActive(enabled);
                    if (enabled)
                        foundBody = true;
                }
            }

            if (!foundHead)
                Debug.LogError($"[CharacterAppearance] 헤드가 없습니다: {headName}", this);
            if (!foundBody)
                Debug.LogError($"[CharacterAppearance] 바디가 없습니다: {bodyName}", this);
        }

        private static bool IsPart(string objectName, string prefix)
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
