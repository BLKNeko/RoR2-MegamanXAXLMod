using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace MegamanXAXLMod.SkillStates
{
    public class axlBullet1 : BaseSkillState
    {
        public float damageCoefficient = 0.8f;
        public float baseDuration = 0.25f;
        public float recoil = 0.4f;
        public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/TracerBanditPistol");
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark1");

        private float duration;
        private float fireDuration;
        private bool hasFired;
        private Animator animator;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.25f * this.duration;
            base.characterBody.SetAimTimer(2f);
            this.animator = base.GetModelAnimator();


            base.PlayAnimation("AttackL", "AxlShootL", "attackSpeed", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireArrow()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, "ABLMuzzle", false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlBullet, base.gameObject);
                    //ProjectileManager.instance.FireProjectile(axlBullet1.tracerEffectPrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0.1f,
                        maxSpread = 0.4f,
                        damage = damageCoefficient * this.damageStat,
                        force = 1f,
                        tracerEffectPrefab = axlBullet1.tracerEffectPrefab,
                        muzzleName = muzzleString,
                        hitEffectPrefab = axlBullet1.hitEffectPrefab,
                        isCrit = Util.CheckRoll(this.critStat, base.characterBody.master)
                    }.Fire();
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireArrow();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                axlBullet2 AxlBullet2 = new axlBullet2();
                this.outer.SetNextState(AxlBullet2);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
