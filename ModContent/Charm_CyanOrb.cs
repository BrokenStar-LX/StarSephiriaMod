using Mirror;
using SPMod.Tool;
using System.Collections.Generic;
using UnityEngine;

namespace SPMod.ModContent
{
    public class Charm_CyanOrb : Charm_StatusInstance, IAttackableCharm, IModEventReceiver
    {
        private readonly string DamageId = "[Star's Sephiria Mod]青色宝珠";
        public float[] MultByLevel = [75, 150, 225];
        public Timer CooldownTimer = new(1f);
        public override Loc.KeywordValue[] BuildKeywords(UnitAvatar avatar, int level, int virtualLevelOffset, bool showAllLevel, bool ignoreAvatarStatus)
        {
            string text = MultByLevel.SafeRandomAccess(LevelToIdx(level)).ToString();
            string text2 = CooldownTimer.time.ToString("#0.##");
            return
            [
                new("Mult", text + "%", GetPositiveColor(virtualLevelOffset)),
                new("Time", text2 + "s", Color.yellow)
            ];
        }
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            CooldownTimer.Ratio = 0f;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            CooldownTimer.Ratio = 0f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isServer) return;
            if (!Avatar.IsInBattle) return;
            if (CooldownTimer.Update(Time.deltaTime))
            {
                Attack();
            }
        }

        private void Attack()
        {
            float Damage = CalculateDamage(this);

            float Value = 1f;
            if (Avatar.GetCustomStatUnsafe(TextFile.CyanOrbUP) > 0)
            {
                Value /= Avatar.GetMoveSpeedMultiplier();
                Value /= Damage / GetDamage(Avatar);
            }
            CooldownTimer.time = Value;

            List<UnitAvatar> TargetList = Tools.SearchEnemiesInRange(Avatar, Avatar.transform.position, 5.6f);
            foreach (UnitAvatar Target in TargetList)
            {
                var Dam = DamageInstance.GetDamage(NetworkAvatar, DamageId, Target.transform.position, 4294967295L, Damage, EDamageType.Slice, EDamageFromType.None, Vector2.zero, 0, 0f);
                Dam.elementalType = EffectConstant.Wind;
                Dam.flag = DamageId;
                Target.ApplyDamage(Dam);
                Target.ApplyDebuff(LoadModContent.WindErosion.Prefab, Avatar);
            }
            if (isClient) PlayFx(Avatar.transform.position);
            ModNetwork.SendEvent(netId, 1, Avatar.transform.position);
        }

        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (isServer) return;
            if (Event == 1) PlayFx(Pos);
        }

        private void PlayFx(Vector3 position)
        {
            var fx = ModuleComponents.ModFxPool.Spawn("CyanOrb", position);
            fx?.SetScale(Vector3.one * 3f);
            fx?.SetColor(new Color(1f, 1f, 1f, 0.1f));
        }

        public float GetDamage(UnitAvatar avatar)
        {
            float Base = avatar.GetCustomStatUnsafe(TextFile.WindDamage);
            float Damage = Base * MultByLevel.SafeRandomAccess(CurrentLevelToIdx()) * 0.01f;
            return Damage;
        }

        public override bool Weaved()
        {
            return true;
        }
    }
}