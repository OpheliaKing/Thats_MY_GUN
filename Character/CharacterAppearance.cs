using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    public enum CharacterPreset
    {
        [InspectorName("1 남자")]
        Man = 1,

        [InspectorName("2 여자")]
        Woman = 2,

        [InspectorName("3 외계인")]
        Alien = 3,

        [InspectorName("4 곰")]
        Bear = 4,

        [InspectorName("5 길리슈트")]
        Ghillie = 5,

        [InspectorName("6 닭")]
        Chicken = 6,

        [InspectorName("7 광대")]
        Clown = 7,

        [InspectorName("8 빵봉투")]
        BreadBag = 8,

        [InspectorName("9 마스크 남자")]
        MaskMan = 9,

        [InspectorName("10 카우보이")]
        Cowboy = 10,

        [InspectorName("11 용병")]
        Mercenary = 11,

        [InspectorName("12 병사")]
        Soldier = 12,

        [InspectorName("13 강도")]
        Robber = 13,

        [InspectorName("14 캐미컬 맨")]
        ChemicalMan = 14,

        [InspectorName("15 기사")]
        Knight = 15,

        [InspectorName("16 닌자")]
        Ninja = 16,

        [InspectorName("17 로봇")]
        Robot = 17,

        [InspectorName("18 사무라이")]
        Samurai = 18,

        [InspectorName("19 닌자2")]
        Ninja2 = 19,

        [InspectorName("20 우주인")]
        Astronaut = 20,

        Default = ChemicalMan
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
            { CharacterPreset.Man, new PartNames("Head01", "Body01") },
            { CharacterPreset.Woman, new PartNames("Head02", "Body02") },
            { CharacterPreset.Alien, new PartNames("Head03", "Body03") },
            { CharacterPreset.Bear, new PartNames("Head04", "Body04") },
            { CharacterPreset.Ghillie, new PartNames("Head05", "Body05") },
            { CharacterPreset.Chicken, new PartNames("Head06", "Body06") },
            { CharacterPreset.Clown, new PartNames("Head07", "Body07") },
            { CharacterPreset.BreadBag, new PartNames("Head08", "Body08") },
            { CharacterPreset.MaskMan, new PartNames("Head09", "Body09") },
            { CharacterPreset.Cowboy, new PartNames("Head10", "Body10") },
            { CharacterPreset.Mercenary, new PartNames("Head11", "Body11") },
            { CharacterPreset.Soldier, new PartNames("Head12", "Body12") },
            { CharacterPreset.Robber, new PartNames("Head13", "Body13") },
            { CharacterPreset.ChemicalMan, new PartNames("Head14", "Body14") },
            { CharacterPreset.Knight, new PartNames("Head15", "Body15") },
            { CharacterPreset.Ninja, new PartNames("Head16", "Body16") },
            { CharacterPreset.Robot, new PartNames("Head17", "Body17") },
            { CharacterPreset.Samurai, new PartNames("Head18", "Body18") },
            { CharacterPreset.Ninja2, new PartNames("Head19", "Body19") },
            { CharacterPreset.Astronaut, new PartNames("Head20", "Body20") }
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
