using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Sage : Charm_Basic, IModValueReceiver
    {
        private readonly Timer CheckTime = new(0.5f);
        private StatusInstance? EffectInstance = null;
        private int Add = 0;
        private int Required = 0;
        private float NextBroadcastTime = 0f;
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            int Energy = 0;
            if (avatar)
            {
                Energy = avatar.GetCustomStatUnsafe(TextFile.DrinkPotionStack);
            }
            return
            [
                new("Value", "+1", GetPositiveColor(virtualLevelOffset)),
                new("Value2", "+2%", GetPositiveColor(virtualLevelOffset)),
                new("Energy", Energy.ToString(), Color.yellow),
                new("Stack", Add.ToString(), Color.yellow),
                new("UseEnergy", Required.ToString(), Color.yellow)
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            ReInstance();
            LoadInstance();
            if (NetworkAvatar)
            {
                NetworkAvatar.OnAttackUnitBeforeOperation += AddDamage;
                NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader += AddDamage_AsLeader;
            }
        }
        protected override void OnDisabledEffect()
        {
            if (NetworkAvatar)
            {
                NetworkAvatar.OnAttackUnitBeforeOperation -= AddDamage;
                NetworkAvatar.OnAttackUnitBeforeOperation_AsLeader -= AddDamage_AsLeader;
            }
            base.OnDisabledEffect();
            ReInstance();
            BroadcastValues();
        }

        private void AddDamage_AsLeader(UnitAvatar Follower, UnitAvatar Target, DamageInstance instance)
        {
            AddDamage(Target, instance);
        }

        private void AddDamage(UnitAvatar Target, DamageInstance instance)
        {
            if (Add > 0)
                instance.damage *= 1f + Add * 0.02f;
        }
        private void ReInstance()
        {
            if (EffectInstance != null)
            {
                EffectInstance.RemoveStatus();
                EffectInstance.ClearTarget();
                EffectInstance = null;
            }
            Add = 0;
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                Add = CalculateStack(NetworkAvatar.GetCustomStatUnsafe(TextFile.DrinkPotionStack), out int Value);
                int NextCost = Add + 1;
                if (Add >= 100) Required = 0;
                else Required = NextCost - Value;
                if (Add > 0)
                {
                    EffectInstance = StatusDatabase.CreateStatusEntity("HIGHEST_ELEMENTAL_DAMAGE", Add);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                }
                BroadcastValues();
            }
        }

        private static int CalculateStack(int Energy, out int Remain)
        {
            int Stack = 0;
            Remain = Energy;
            while (Remain > Stack)
            {
                Stack += 1;
                Remain -= Stack;
                if (Stack >= 100) break;
            }
            return Stack;
        }

        public void OnClientValues(int A, int B)
        {
            Add = A;
            Required = B;
        }

        private void BroadcastValues()
        {
            ModNetwork.SendValues(netId, Add, Required);
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                int Count = CalculateStack(NetworkAvatar.GetCustomStatUnsafe(TextFile.DrinkPotionStack), out _);
                if (Add != Count)
                {
                    ReInstance();
                    LoadInstance();
                }
            }
            if (NetworkTime.time >= NextBroadcastTime)
            {
                NextBroadcastTime = (float)NetworkTime.time + 2f;
                BroadcastValues();
            }
            base.OnUpdate();
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}