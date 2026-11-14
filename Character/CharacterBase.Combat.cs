using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        public void ReceiveDealtDamage(DamageResult result)
        {
            var target = result.Victim != null ? result.Victim.name : string.Empty;
            Debug.Log($"[CharacterBase] 피해를 줌. damage={result.Dealt}, target={target}", this);
        }

        public void ReceiveTakenDamage(DamageResult result)
        {
            if (_info == null || _info.IsDead)
                return;

            var source = result.Attacker != null ? result.Attacker.name : string.Empty;
            _info.AddHp(-result.Received);
            PlayHitFlash();
            Debug.Log($"[CharacterBase] 피해를 받음. damage={result.Received}, source={source}, hp={_info.Hp}/{_info.MaxHp}", this);
        }
    }
}
