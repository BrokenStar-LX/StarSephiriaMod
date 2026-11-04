using System.Collections;
using SPMod.Tool;
using UnityEngine;

namespace SPMod.ModContent
{
    public class ActiveSkill_WindBlade : ActiveSkill
    {
        public GameObject? boltBulletPrefab;
        public string damageId = "[Star's Sephiria Mod]WindBlade";
        public float[] defaultDamageByLevel = [10f, 15f, 20f, 25f, 30f];
        public string relatedDamage = "WindDamage";
        public float[] damagePercentByLevel = [70f, 105f, 140f, 175f, 210f];
        public float externalForcePower = 1f;
        public EDamageElementalType elementalType = EffectConstant.Wind;
        public float fireInterval = 0.05f;

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

            int charges = 1;
            if (Charm != null)
            {
                charges = Mathf.Clamp(Charm.currentAmmo + 1, 1, Mathf.Max(1, Charm.maxAmmo));
                Charm.NetworkcurrentAmmo = 0;
            }
            StartCoroutine(FireBlades(charges));
        }

        private IEnumerator FireBlades(int count)
        {
            UnitAvatar? caster = NetworkCaster;
            if (caster == null || boltBulletPrefab == null)
            {
                Despawn();
                yield break;
            }

            Vector2 baseDir = (Vector2)CastTargetPos - (Vector2)CastStartPos;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;
            baseDir.Normalize();
            float baseAngle = HorayUtility.GetAngleFromVector(baseDir);
            WaitForSeconds wait = new(Mathf.Max(0.01f, fireInterval));
            for (int i = 0; i < count; i++)
            {
                float Offset = Random.Range(-10f, 10f);
                Vector3 dir = HorayUtility.GetVector3FromAngle(baseAngle + Offset).normalized;
                Vector3 start = CastStartPos + dir;
                Bullet? bullet = Bullet.Pool.Spawn(boltBulletPrefab, start, true, EDamageFromType.Magic, damageId, calculatedDamage, 0, externalForcePower, caster, caster.GetHostileFactionLayers(EDamageFromType.Magic), CastStartHeight, start, start + dir * 5f, null, null, 0f, elementalType);
                if (bullet != null) OnCreateProjectile?.Invoke(bullet);
                if (i < count - 1) yield return wait;
            }
            Despawn();
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
