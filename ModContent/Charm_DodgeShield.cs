using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using SPMod.Tool;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static UnitAvatar;

namespace SPMod.ModContent
{
    public class Charm_DodgeShield : Charm_StatusInstance
    {
        private StatusInstance? EffectInstance = null;
        public int[] BaseShieldByLevel = new int[] { 2, 4, 6, 8 };
        public int[] DodgeToShieldByLevel = new int[] { 3, 6, 9, 12 };
        public Timer CooldownTimer = new Timer(12f);
        public float AddValue = 0;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = BaseShieldByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string text2 = DodgeToShieldByLevel.SafeRandomAccess(LevelToIdx(level)).ToString() + "%";
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Base", text.ToString(), GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Dodge", text2.ToString(), GetPositiveColor(virtualLevelOffset)),
            };
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            ReInstance();
            LoadInstance();
            NetworkAvatar.OnShieldChanged += OnShieldChanged;
            CooldownTimer.Ratio = 1f;
        }

        private void OnShieldChanged()
        {
            ReInstance();
            LoadInstance();
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            ReInstance();
            NetworkAvatar.RemoveShield("Charm_DodgeShield");
            NetworkAvatar.OnShieldChanged -= OnShieldChanged;
            CooldownTimer.Ratio = 1f;
        }

        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            AddValue = 0;
        }

        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                float Value = NetworkAvatar.Shield;
                if (Value > 0)
                {
                    EffectInstance = StatusDatabase.CreateStatusEntity("EVASION", (int)Value * 100);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                    AddValue = Value;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CooldownTimer.Update(Time.deltaTime))
            {
                float Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.Evasion) * 0.01f;
                Value -= AddValue;
                Value *= DodgeToShieldByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
                Value += BaseShieldByLevel.SafeRandomAccess(CurrentLevelToIdx());
                if ((int)Value > 0)
                {
                    NetworkAvatar.AddShield(Value, "Charm_DodgeShield");
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}