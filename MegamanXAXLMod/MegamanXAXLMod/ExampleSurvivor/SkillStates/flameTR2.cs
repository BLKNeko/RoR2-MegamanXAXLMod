using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace MegamanXAXLMod.SkillStates
{
    public class flameTR2 : BaseSkillState
    {
        public float damageCoefficient = 1f;
        public float baseDuration = 0.9f;
        public float recoil = 0.5f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/TracerEmbers");
        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/FireballVehicleDamageEffect");

        private int repeat;
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



        }

        public override void OnExit()
        {
            base.PlayAnimation("Attack", "FlameTOut", "FireArrow.playbackRate", this.duration);
            base.OnExit();
        }

        private void FireFT()
        {
            if (!this.hasFired)
            {
                //this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.15f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, "ABRMuzzle", false);
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, "ABLMuzzle", false);

                if (base.isAuthority)
                {
                    if (repeat == 1)
                        Util.PlaySound(Sounds.axlFireShot, base.gameObject);

                    if (repeat % 10 == 0 && repeat > 10)
                        Util.PlaySound(Sounds.axlFireShot, base.gameObject);

                    // ProjectileManager.instance.FireProjectile(axlBullet2.tracerEffectPrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0.1f,
                        maxSpread = 0.4f,
                        damage = ((damageCoefficient * this.damageStat) / (100 - base.characterBody.level)),
                        force = 20f,
                        tracerEffectPrefab = flameTR.tracerEffectPrefab,
                        muzzleName = muzzleString,
                        hitEffectPrefab = flameTR.hitEffectPrefab,
                        maxDistance = 30f,
                        smartCollision = true,
                        damageType = (Util.CheckRoll(0.88f, base.characterBody.master) ? DamageType.IgniteOnHit : DamageType.Generic)
                    }.Fire();
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireFT();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                if (repeat <= 100)
                {
                    repeat++;
                    FireFT();
                }
                else
                    this.outer.SetNextStateToMain();
                //flameTR2 FTR2 = new flameTR2();
                //this.outer.SetNextState(FTR2);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
