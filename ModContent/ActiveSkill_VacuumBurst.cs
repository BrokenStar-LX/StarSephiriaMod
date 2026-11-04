using System.Collections;
using System.Collections.Generic;
using SPMod.Tool;
using UnityEngine;
using static SPMod.Tool.ModuleComponents;

namespace SPMod.ModContent
{
    public class ActiveSkill_VacuumBurst : ActiveSkill, IModEventReceiver
    {
        public GameObject? boltBulletPrefab;
        public string damageId = "[Star's Sephiria Mod]VacuumBurst";
        public float[] defaultDamageByLevel = [6f, 9f, 12f, 15f, 18f];
        public string relatedDamage = "WindDamage";
        public float[] damagePercentByLevel = [20f, 30f, 40f, 50f, 60f];
        public float externalForcePower = 1f;
        public EDamageElementalType elementalType = EffectConstant.Wind;
        public float explosionRadius = 2f;
        public float ExplosionOffsetYPercent = 0.25f;
        public const byte FxEvent = 1;

        private static readonly WaitForSeconds TickWait = new(0.03f);
        private static readonly WaitForSeconds DespawnWait = new(8f);
        private float calculatedDamage;
        private float explosionOffsetY = float.NaN;
        private bool fired;
        private bool exploding;
        private readonly Timer fireTimer = new(0.1f);

        public override int GetPossibleMaxLevel()
        {
            return Mathf.Max(base.GetPossibleMaxLevel(), defaultDamageByLevel.Length, damagePercentByLevel.Length);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            if (fired) return;
            if (!fireTimer.Update(Time.deltaTime)) return;
            if (boltBulletPrefab == null || NetworkCaster == null)
            {
                Despawn();
                return;
            }
            fired = true;
            Bullet? bullet = Bullet.Pool.Spawn(boltBulletPrefab, CastStartPos, true, EDamageFromType.Magic, damageId, calculatedDamage, 25, externalForcePower, NetworkCaster, NetworkCaster.GetHostileFactionLayers(EDamageFromType.Magic), CastStartHeight, transform.position, CastTargetPos, null, OnHit, 0f, elementalType);
            if (bullet != null)
            {
                bullet.onDespawn = OnBulletDespawn;
                OnCreateProjectile?.Invoke(bullet);
            }
            StartCoroutine(DelayDespawn());
        }

        protected override void OnStartMagic()
        {
            base.OnStartMagic();
            fireTimer.Ratio = 1f;
            float damage = defaultDamageByLevel.SafeRandomAccess(Level) + NetworkCaster.GetCustomStat(relatedDamage) * damagePercentByLevel.SafeRandomAccess(Level) * 0.01f;
            damage += damage * (NetworkCaster.GetCustomStat(ECustomStat.MagicDamageBonus) * 0.01f);
            damage += damage * (MpSkillDamage * 0.01f);
            if (Cost > 0)
            {
                float costRatio = Cost / 10f;
                damage += damage * (costRatio * (NetworkCaster.GetCustomStatUnsafe("MAGICMP") * 0.01f));
            }
            calculatedDamage = damage * power;
        }

        private void OnHit(CombatBehaviour? victim, DamageInstance? damage, ProjectileBase? projectile)
        {
            if (!isServer || exploding) return;
            ExplodeAt(projectile != null ? projectile.transform.position : CastTargetPos);
        }

        private void OnBulletDespawn(Bullet bullet)
        {
            if (!isServer || exploding) return;
            ExplodeAt(bullet != null ? bullet.transform.position : CastTargetPos);
        }

        private void ExplodeAt(Vector3 pos)
        {
            if (exploding) return;
            exploding = true;
            Vector3 fxPos = pos + Vector3.up * GetExplosionOffsetY();
            if (isClient) PlayFx(fxPos);
            ModNetwork.SendEvent(netId, FxEvent, fxPos);
            StartCoroutine(ExplosionRoutine(pos));
        }

        private IEnumerator ExplosionRoutine(Vector3 pos)
        {
            UnitAvatar? caster = NetworkCaster;
            float innerRadiusSqr = explosionRadius * explosionRadius;
            float outerRadius = explosionRadius * 1.5f;
            for (int i = 0; i < 20; i++)
            {
                if (i > 0) yield return TickWait;
                if (caster == null) yield break;
                foreach (UnitAvatar target in Tools.SearchEnemiesInRange(caster, pos, outerRadius, -1, true))
                {
                    Vector2 offset = (Vector2)target.transform.position - (Vector2)pos;
                    float bodyRadius = target.TopdownActor != null ? Mathf.Max(target.TopdownActor.size.x, target.TopdownActor.size.y) * 0.5f : 0f;
                    float damageMult = offset.sqrMagnitude <= (explosionRadius + bodyRadius) * (explosionRadius + bodyRadius) ? 1f : 0.5f;

                    DamageInstance tick = DamageInstance.GetDamage(caster, damageId, target.transform.position, 4294967295L, calculatedDamage * damageMult, EDamageType.Projectile, EDamageFromType.Magic, Vector2.zero, 0, 0f);

                    tick.elementalType = elementalType;
                    tick.flag = damageId;
                    target.ApplyDamage(tick);
                }
            }
        }

        private IEnumerator DelayDespawn()
        {
            yield return DespawnWait;
            if (this != null) Despawn();
        }

        private float GetExplosionOffsetY()
        {
            if (float.IsNaN(explosionOffsetY))
            {
                Sprite? Fx = Tools.LoadModSpriteForPath("EffectFx", "VacuumBurstFX_0");
                explosionOffsetY = Fx != null ? Fx.bounds.size.y * ExplosionOffsetYPercent : 0f;
            }
            return explosionOffsetY;
        }

        private void PlayFx(Vector3 pos)
        {
            int ownerIndex = NetworkCaster != null && NetworkCaster.isOwned ? 1 : 0;
            ModFxPool.Spawn("VacuumBurstFX", pos, ownerIndex);
        }

        public void OnClientEvent(byte Event, Vector3 Pos, string S1, string S2)
        {
            if (isServer || Event != FxEvent) return;
            PlayFx(Pos);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            fired = false;
            exploding = false;
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
