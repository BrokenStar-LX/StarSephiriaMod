using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Telescope : Charm_PlanetModule
    {
        private static readonly FieldInfo? planetsField;
        private StatusInstance? EffectInstance = null;
        private StatusInstance? EffectInstance2 = null;
        private StatusInstance? EffectInstance3 = null;
        private readonly Timer CheckTime = new Timer(0.5f);
        [SyncVar]
        private int DamageAdd = 0;
        private int AttackAdd = 0;
        private readonly int[] SpeedbyLevel = new int[] { 5, 10 };
        static Charm_Telescope()
        {
            planetsField = AccessTools.Field(typeof(Charm_PlanetModule), "planets");
        }
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Speed", "+" + SpeedbyLevel.SafeRandomAccess(level).ToString() + "%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Value", "+1%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Damage", DamageAdd.ToString() + "%", Color.yellow)
            };
        }
        private void ReAttackSpeed()
        {
            if (EffectInstance3 != null)
            {
                EffectInstance3.RemoveStatus();
                EffectInstance3.ClearTarget();
                EffectInstance3 = null;
            }
            AttackAdd = 0;
        }
        private void LoadAttackSpeed()
        {
            if (NetworkAvatar)
            {
                int Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.TelescopeCount);
                if (Value > 0)
                {
                    EffectInstance3 = StatusDatabase.CreateStatusEntity("PLANET_ATTACK_SPEED", Value * SpeedbyLevel.SafeRandomAccess(CurrentLevelToIdx()));
                    EffectInstance3.SetTarget(NetworkAvatar);
                    EffectInstance3.ApplyStatus(true);
                    AttackAdd = Value;
                }
            }
        }
        private void ReDamage()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            if (EffectInstance2 != null)
            {
                EffectInstance2.RemoveStatus();
                EffectInstance2.ClearTarget();
                EffectInstance2 = null;
            }
            DamageAdd = 0;
        }
        private void LoadDamage()
        {
            if (planetsField == null) return;
            List<Charm_SummonGreenBat> Planets = (List<Charm_SummonGreenBat>)planetsField.GetValue(this);
            if (Planets != null && Planets.Count > 0)
            {
                if (NetworkAvatar)
                {
                    int Value = 0;
                    foreach (Charm_SummonGreenBat Target in Planets)
                    {
                        Value += Target.CurrentLevelToIdx();
                    }
                    if (Value > 0)
                    {
                        EffectInstance = StatusDatabase.CreateStatusEntity("FINAL_DAMAGE", Value);
                        EffectInstance.SetTarget(NetworkAvatar);
                        EffectInstance.ApplyStatus(true);
                        EffectInstance2 = StatusDatabase.CreateStatusEntity("PLANET_DAMAGE", Value);
                        EffectInstance2.SetTarget(NetworkAvatar);
                        EffectInstance2.ApplyStatus(true);
                        DamageAdd = Value;
                    }
                }
            }
        }
        protected override void OnUpdatedLevel(int oldLevel, int newLevel)
        {
            ReAttackSpeed();
            LoadAttackSpeed();
        }

        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            ReDamage();
            LoadDamage();
            ReAttackSpeed();
            LoadAttackSpeed();
            NetworkAvatar?.AddCustomStatUnsafe(TextFile.TelescopeCount, 1);
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            ReDamage();
            ReAttackSpeed();
            NetworkAvatar?.AddCustomStatUnsafe(TextFile.TelescopeCount, -1);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CheckTime.Update(Time.deltaTime))
            {
                ReDamage();
                LoadDamage();
                int Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.TelescopeCount);
                if (Value != AttackAdd)
                {
                    ReAttackSpeed();
                    LoadAttackSpeed();
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}