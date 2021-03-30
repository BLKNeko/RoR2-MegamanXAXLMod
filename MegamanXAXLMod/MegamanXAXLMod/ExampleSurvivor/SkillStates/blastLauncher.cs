using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace MegamanXAXLMod.SkillStates
{
    public class blastLauncher : BaseSkillState
    {
        public float damageCoefficient = 1.8f;
        public float baseDuration = 0.30f;
        public float recoil = 0.5f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/TracerGolem");
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
            this.fireDuration = 0.20f * this.duration;
            base.characterBody.SetAimTimer(2f);
            this.animator = base.GetModelAnimator();



            base.PlayAnimation("AttackL", "AxlShootL", "attackSpeed", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireBL()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, "ABLMuzzle", false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlAttacks, base.gameObject);
                    Util.PlaySound(Sounds.axlBlastLauncher, base.gameObject);
                    ProjectileManager.instance.FireProjectile(AXLMod.MegamanXAXLMod.blastL, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireBL();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                blastLauncher2 BL2 = new blastLauncher2();
                this.outer.SetNextState(BL2);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
