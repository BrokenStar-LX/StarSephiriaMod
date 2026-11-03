using Mirror;
using SPMod.Tool;
using Unity.VisualScripting;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_HarmfulValue : Charm_StatusInstance, IAttackableCharm
    {
        private readonly Timer CheckTime = new(0.5f);
        private StatusInstance? EffectInstance = null;
        public int[] BaseValueByLevel = [];
        public string StatusName = "";
        public string Ending = "";
        public float StatusValue = 1f;
        public float FindMult = 1f;//稀释鹿3加成
        public string AddValue = "";
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            float Value = GetValue(avatar, level) / FindMult;
            string text = Value.ToString() + Ending;
            return
            [
                new("Value", text.ToString(), GetPositiveColor(virtualLevelOffset)),
            ];
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            ReInstance();
            LoadInstance();
        }

        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            StatusValue = 1f;
        }

        private void LoadInstance()
        {
            float Value = CalculateDamage(this) / FindMult;
            EffectInstance = StatusDatabase.CreateStatusEntity(StatusName, (int)Value);
            EffectInstance.SetTarget(NetworkAvatar);
            EffectInstance.ApplyStatus(true);
            StatusValue = (int)Value;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            ReInstance();
        }

        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            base.OnUpdatedLevel(oldLevel, newLevel);
            if (NetworkAvatar)
            {
                ReInstance();
                LoadInstance();
            }
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                float Value = CalculateDamage(this);
                if ((int)StatusValue != (int)Value)
                {
                    ReInstance();
                    LoadInstance();
                }
            }
            base.OnUpdate();
        }

        public float GetValue(UnitAvatar avatar, int Level)
        {
            float Value = BaseValueByLevel.SafeRandomAccess(LevelToIdx(Level));
            if (avatar != null && AddValue != "")
            {
                Value += avatar.GetCustomStatUnsafe(AddValue);
            }
            Value *= FindMult;
            return Value;
        }

        public float GetDamage(UnitAvatar avatar)
        {
            return GetValue(avatar, CurrentLevelToIdx());
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}