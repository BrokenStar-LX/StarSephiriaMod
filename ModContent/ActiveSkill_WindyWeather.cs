using System.Collections;
using SPMod.Tool;
using UnityEngine;
using static SPMod.Tool.ModuleComponents;

namespace SPMod.ModContent
{
    /// <summary>
    /// 大风天：持续 15 秒，每秒对全屏敌人造成风属性伤害并施加风蚀；
    /// 期间随机在屏幕上播放水母弹粒子作为天气特效。
    /// </summary>
    public class ActiveSkill_WindyWeather : ActiveSkill
    {
        public string damageId = "[Star's Sephiria Mod]WindyWeather";
        public float[] defaultDamageByLevel = [6f, 12f, 18f, 24f, 30f];
        public string relatedDamage = "WindDamage";
        public float[] damagePercentByLevel = [20f, 40f, 60f, 80f, 100f];
        public EDamageElementalType elementalType = EffectConstant.Wind;
        public float duration = 15f;
        public float tickInterval = 1f;
        public float damageRadius = 20f;
        public float fxRadius = 10f;
        public int fxPerSecond = 10;

        private float calculatedDamage;

        public override int GetPossibleMaxLevel()
        {
            return Mathf.Max(base.GetPossibleMaxLevel(), defaultDamageByLevel.Length, damagePercentByLevel.Length);
        }

        protected override void OnStartMagic()
        {
            base.OnStartMagic();
            float damage = defaultDamageByLevel.SafeRandomAccess(Level) + NetworkCaster.GetCustomStat(relatedDamage) * damagePercentByLevel.SafeRandomAccess(Level) * 0.01f;
            damage += damage * (NetworkCaster.GetCustomStat(ECustomStat.MagicDamageBonus) * 0.01f);
            damage += damage * (MpSkillDamage * 0.01f);
            if (Cost > 0)
            {
                float costRatio = Cost / 10f;
                damage += damage * (costRatio * (NetworkCaster.GetCustomStatUnsafe("MAGICMP") * 0.01f));
            }
            calculatedDamage = damage * power;
            if (isClient) StartCoroutine(ClientFxRoutine());
            StartCoroutine(WeatherRoutine());
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer) StartCoroutine(ClientFxRoutine());
        }

        private IEnumerator ClientFxRoutine()
        {
            int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / Mathf.Max(0.05f, tickInterval)));
            WaitForSeconds wait = new(Mathf.Max(0.05f, tickInterval));
            int perTick = Mathf.Max(1, fxPerSecond);
            for (int i = 0; i < ticks; i++)
            {
                for (int j = 0; j < perTick; j++)
                {
                    PlayFx(GetRandomFxPosition(NetworkCaster));
                }
                yield return wait;
            }
        }

        private IEnumerator WeatherRoutine()
        {
            UnitAvatar? caster = NetworkCaster;
            if (caster == null)
            {
                Despawn();
                yield break;
            }

            int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / Mathf.Max(0.05f, tickInterval)));
            WaitForSeconds wait = new(Mathf.Max(0.05f, tickInterval));
            for (int i = 0; i < ticks; i++)
            {
                if (caster == null) break;

                if (caster.IsInBattle)
                {
                    foreach (UnitAvatar target in Tools.SearchEnemiesInRange(caster, caster.transform.position, damageRadius, -1))
                    {
                        DamageInstance tick = DamageInstance.GetDamage(caster, damageId, target.transform.position, 4294967295L, calculatedDamage, EDamageType.ElementalEffectDamage, EDamageFromType.None, Vector2.zero, 0, 0f);
                        tick.elementalType = elementalType;
                        tick.flag = damageId;
                        target.ApplyDamage(tick);
                        target.ApplyDebuff(LoadModContent.WindErosion.Prefab, caster);
                    }
                }
                yield return wait;
            }
            Despawn();
        }

        private Vector3 GetRandomFxPosition(UnitAvatar? caster)
        {
            Camera? cam = Camera.main;
            if (cam != null)
            {
                float depth = Mathf.Abs(cam.transform.position.z);
                Vector3 viewport = new(
                    UnityEngine.Random.Range(0.05f, 0.95f),
                    UnityEngine.Random.Range(0.05f, 0.95f),
                    depth);
                Vector3 world = cam.ViewportToWorldPoint(viewport);
                if (caster != null) world.z = caster.transform.position.z;
                return world;
            }
            Vector3 center = caster != null ? caster.transform.position : transform.position;
            return center + (Vector3)(UnityEngine.Random.insideUnitCircle * fxRadius);
        }

        private void PlayFx(Vector3 pos)
        {
            int ownerIndex = NetworkCaster != null && NetworkCaster.isOwned ? 1 : 0;
            ModFxPool.Spawn("WindyWeatherParticle", pos, ownerIndex);
        }

        public override Loc.KeywordValue[] BuildKeywords(int level)
        {
            KeywordEntity entity = KeywordDatabase.GetEntity(relatedDamage);
            return
            [
                new("STAT", entity.Name),
                new("DEFAULT_DAMAGE", defaultDamageByLevel.SafeRandomAccess(level).ToString()),
                new("SCALE_DAMAGE", damagePercentByLevel.SafeRandomAccess(level).ToString()),
            ];
        }

        public override bool IsAttackMagic() => true;

        public override bool Weaved() => true;
    }
}
