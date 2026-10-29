using HarmonyLib;
using Mirror;
using SPMod.Tool;
using System.Reflection;
using UnityEngine;

namespace SPMod.ModContent
{
    public class CharacterDebuff_WindErosion : CharacterDebuff, IModEventReceiver
    {
        private const byte FxWindSailExplosion = 1;
        public string id = "WINDEROSION";
        public string damageId = "Debuff_WindErosion";
        public Timer TickTimer = new Timer(0.5f);
        public int DamageCount = 0;
        public int MaxDamageCount
        {
            get
            {
                if (NetworkAttacker)
                    return 4 + NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionCount);
                return 4;
            }
        }
        public override string ID => id;
        public override int MaxStackCount => 5;
        public static float GetWindErosionDamage(UnitAvatar Player)
        {
            if (!Player)
            {
                return 1f;
            }
            float Mult = 0.24f + Player.GetCustomStatUnsafe(TextFile.WindErosionMult) * 0.01f;
            float Damage = Player.GetCustomStatUnsafe(TextFile.WindDamage);
            Damage *= Mult;
            Damage *= 1f + Player.GetCustomStatUnsafe(TextFile.WindErosionDamage) * 0.01f;
            return Damage;
        }

        public void DownWindErosionStack()
        {
            if (!isServer || IsEndBuff) return;
            if (--DamageCount > 0) return;
            NetworkCurrentStack -= 1;
            if (CurrentStack <= 0)
            {
                Destroy();
                return;
            }
            DamageCount = MaxDamageCount;
        }

        protected override void InitializeInner(UnitAvatar caster, UnitAvatar target, float amplified, float defaultDuration, float durationBonusPercent)
        {
            base.InitializeInner(caster, target, amplified, defaultDuration, durationBonusPercent);
            DamageCount = MaxDamageCount;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (NetworkTarget != null) NetworkTarget.OnDamagedServerside += OnTargetDamaged;
        }

        protected override void OnUpdate_Server()
        {
            base.OnUpdate_Server();
            if (NetworkAttacker && NetworkTarget && !NetworkTarget.IsDead)
            {
                if (!NetworkAttacker.IsInBattle) return;
                float Mult = NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionDot) * 0.01f;
                if (Mult <= 0) return;

                if (TickTimer.Update(Time.deltaTime))
                {
                    float Value = NetworkAttacker.GetCustomStatUnsafe(TextFile.WindDamage) * Mult;
                    string TickDamageId = "[Star's Sephiria Mod]空气钟";
                    DamageInstance damage = DamageInstance.GetDamage(NetworkAttacker, TickDamageId, NetworkTarget.transform.position, -1, Value,
                        EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                    damage.elementalType = EffectConstant.Wind;
                    NetworkTarget.ApplyDamage(damage);
                }
            }
        }

        protected override void DestroyInner()
        {
            base.DestroyInner();
            if (NetworkTarget != null) NetworkTarget.OnDamagedServerside -= OnTargetDamaged;
        }

        protected override void AddStackFailedByMaxStackCount()
        {
            DamageCount = MaxDamageCount;
            OverFlowDamage();
        }

        private void OverFlowDamage()
        {
            if (NetworkTarget == null) return;
            if (NetworkAttacker == null) return;
            if (NetworkTarget.IsDead) return;
            float Mult = NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionOverFlow) * 0.01f;
            if (Mult <= 0) return;
            float Dam = GetWindErosionDamage(NetworkAttacker) * MaxDamageCount * Mult;
            Dam += NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionOverFlowBase);
            if (Dam <= 0) return;

            string OverFlowDamageId = "[Star's Sephiria Mod]风帆";
            DamageInstance damage = DamageInstance.GetDamage(NetworkAttacker, OverFlowDamageId, NetworkTarget.transform.position, -1, Dam,
                EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
            damage.elementalType = EffectConstant.Wind;
            NetworkTarget.ApplyDamage(damage);
            PlayOverFlowFx();
        }

        private void PlayOverFlowFx()
        {
            if (NetworkTarget == null) return;

            Vector3 Pos = NetworkTarget.TopdownActor != null
                ? NetworkTarget.transform.position + new Vector3(0f, NetworkTarget.TopdownActor.size.y * 0.5f)
                : NetworkTarget.transform.position;

            if (isClient) ModuleComponents.ModFxPool.Spawn("WindSail_Explosion", Pos, FxOwnerIndex());
            ModNetwork.SendEvent(netId, FxWindSailExplosion, Pos);
        }

        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (isServer) return;
            if (Event == FxWindSailExplosion) ModuleComponents.ModFxPool.Spawn("WindSail_Explosion", Pos, FxOwnerIndex());
        }

        private int FxOwnerIndex()
        {
            return NetworkAttacker != null && NetworkAttacker.isOwned ? 1 : 0;
        }

        private void OnTargetDamaged(DamageInstance damage)
        {
            if (NetworkTarget == null) return;
            if (NetworkAttacker == null) return;
            if (NetworkTarget.IsDead) return;
            if (damage.origin == NetworkAttacker)
            {
                if (damage.IsSameElementalType(EffectConstant.Wind) && damage.damageType != EDamageType.ElementalEffectDamage)
                {
                    float Dam = GetWindErosionDamage(NetworkAttacker);
                    if (Dam <= 0) return;

                    int MaxStack = 1;
                    if (NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionLuck) > Random.Range(0, 100)) MaxStack += 1;
                    for (int i = 0; i < MaxStack; i++)
                    {
                        DamageInstance New = DamageInstance.GetDamage(NetworkAttacker, damageId, NetworkTarget.transform.position, -1, Amplified * Dam, EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
                        New.elementalType = EffectConstant.Wind;
                        New.criticalDamageRate += NetworkAttacker.GetCustomStatUnsafe(TextFile.WindErosionCritDamage);
                        NetworkTarget.ApplyDamage(New);
                    }
                    DownWindErosionStack();
                }
            }
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}