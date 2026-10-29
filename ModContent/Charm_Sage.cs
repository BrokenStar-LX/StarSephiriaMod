using Mirror;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_Sage : Charm_Basic, IModValueReceiver
    {
        private readonly Timer CheckTime = new Timer(0.5f);
        private StatusInstance? EffectInstance = null;
        private StatusInstance? EffectInstance2 = null;
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
            return new Loc.KeywordValue[]
            {
                new Loc.KeywordValue("Value", "+1", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Value2", "+2%", GetPositiveColor(virtualLevelOffset)),
                new Loc.KeywordValue("Energy", Energy.ToString(), Color.yellow),
                new Loc.KeywordValue("Stack", Add.ToString(), Color.yellow),
                new Loc.KeywordValue("UseEnergy", Required.ToString(), Color.yellow)
            };
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
            if (EffectInstance2 != null)
            {
                EffectInstance2.RemoveStatus();
                EffectInstance2.ClearTarget();
                EffectInstance2 = null;
            }
            Add = 0;
        }
        private void LoadInstance()
        {
            if (NetworkAvatar)
            {
                Add = 1;
                int Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.DrinkPotionStack);
                while (Value >= Add)
                {
                    Value -= Add;
                    Add += 1;
                    if (Add > 100) break;
                }
                int NextCost = Add;
                if (Add >= 100) Required = 0;
                else Required = NextCost - Value;
                if (Add > 0)
                {
                    EffectInstance = StatusDatabase.CreateStatusEntity("HIGHEST_ELEMENTAL_DAMAGE", Add);
                    EffectInstance.SetTarget(NetworkAvatar);
                    EffectInstance.ApplyStatus(true);
                    EffectInstance2 = StatusDatabase.CreateStatusEntity("FINAL_DAMAGE", Add * 2);
                    EffectInstance2.SetTarget(NetworkAvatar);
                    EffectInstance2.ApplyStatus(true);
                }
                BroadcastValues();
            }
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

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            ReInstance();
            BroadcastValues();
        }

        protected override void OnUpdate()
        {
            if (NetworkAvatar && CheckTime.Update(Time.deltaTime))
            {
                int Count = 1;
                int Value = NetworkAvatar.GetCustomStatUnsafe(TextFile.DrinkPotionStack);
                while (Value >= Count)
                {
                    Value -= Count;
                    Count += 1;
                    if (Count > 100) break;
                }
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