using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using EntityStates;
using EntityStates.ExampleSurvivorStates;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using KinematicCharacterController;
using System.IO;

namespace ExampleSurvivor
{

    [BepInDependency("com.bepis.r2api")]

    [BepInPlugin(MODUID, "MegamanXAXLMod", "1.0.0")] // put your own name and version here
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(SurvivorAPI), nameof(LoadoutAPI), nameof(ItemAPI), nameof(DifficultyAPI), nameof(BuffAPI))] // need these dependencies for the mod to work properly


    public class MegamanXAXLMod : BaseUnityPlugin
    {
        public const string MODUID = "com.BLKNeko.MegamanXAXLMod"; // put your own names here

        public static GameObject characterPrefab; // the survivor body prefab
        public GameObject characterDisplay; // the prefab used for character select
        public GameObject doppelganger; // umbra shit

        public static GameObject axlBullet; // prefab for our survivor's primary attack projectile
        public static GameObject blastL; // prefab for our survivor's BlastLauncher attack projectile
        public static GameObject rayG; // prefab for our survivor's RayGun attack projectile

        private static readonly Color characterColor = new Color(0.10f, 0.20f, 0.44f); // color used for the survivor

        private void Awake()
        {
            Assets.PopulateAssets(); // first we load the assets from our assetbundle
            CreatePrefab(); // then we create our character's body prefab
            RegisterStates(); // register our skill entitystates for networking
            RegisterCharacter(); // and finally put our new survivor in the game
            CreateDoppelganger(); // not really mandatory, but it's simple and not having an umbra is just kinda lame
        }

        private static GameObject CreateModel(GameObject main)
        {
            Destroy(main.transform.Find("ModelBase").gameObject);
            Destroy(main.transform.Find("CameraPivot").gameObject);
            Destroy(main.transform.Find("AimOrigin").gameObject);

            // make sure it's set up right in the unity project
            GameObject model = Assets.MainAssetBundle.LoadAsset<GameObject>("mdlSurvivalAXLN");

            return model;
        }

        internal static void CreatePrefab()
        {
            // first clone the commando prefab so we can turn that into our own survivor
            characterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), "AxlBody", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreatePrefab", 151);

            characterPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

            // create the model here, we're gonna replace commando's model with our own
            GameObject model = CreateModel(characterPrefab);

            GameObject gameObject = new GameObject("ModelBase");
            gameObject.transform.parent = characterPrefab.transform;
            gameObject.transform.localPosition = new Vector3(0f, -0.81f, 0f);
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject gameObject2 = new GameObject("CameraPivot");
            gameObject2.transform.parent = gameObject.transform;
            gameObject2.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            gameObject2.transform.localRotation = Quaternion.identity;
            gameObject2.transform.localScale = Vector3.one;

            GameObject gameObject3 = new GameObject("AimOrigin");
            gameObject3.transform.parent = gameObject.transform;
            gameObject3.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            gameObject3.transform.localRotation = Quaternion.identity;
            gameObject3.transform.localScale = Vector3.one;

            Transform transform = model.transform;
            transform.parent = gameObject.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            transform.localRotation = Quaternion.identity;

            CharacterDirection characterDirection = characterPrefab.GetComponent<CharacterDirection>();
            characterDirection.moveVector = Vector3.zero;
            characterDirection.targetTransform = gameObject.transform;
            characterDirection.overrideAnimatorForwardTransform = null;
            characterDirection.rootMotionAccumulator = null;
            characterDirection.modelAnimator = model.GetComponentInChildren<Animator>();
            characterDirection.driveFromRootRotation = false;
            characterDirection.turnSpeed = 720f;

            // set up the character body here
            CharacterBody bodyComponent = characterPrefab.GetComponent<CharacterBody>();
            bodyComponent.bodyIndex = -1;
            bodyComponent.baseNameToken = "AXL_NAME"; // name token
            bodyComponent.subtitleNameToken = "AXL_SUBTITLE"; // subtitle token- used for umbras
            bodyComponent.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent.rootMotionInMainState = false;
            bodyComponent.mainRootSpeed = 0;
            bodyComponent.baseMaxHealth = 95;
            bodyComponent.levelMaxHealth = 24;
            bodyComponent.baseRegen = 0.5f;
            bodyComponent.levelRegen = 0.3f;
            bodyComponent.baseMaxShield = 0;
            bodyComponent.levelMaxShield = 0;
            bodyComponent.baseMoveSpeed = 9;
            bodyComponent.levelMoveSpeed = 0.4f;
            bodyComponent.baseAcceleration = 90;
            bodyComponent.baseJumpPower = 15;
            bodyComponent.levelJumpPower = 0.30f;
            bodyComponent.baseDamage = 15.5f;
            bodyComponent.levelDamage = 3.5f;
            bodyComponent.baseAttackSpeed = 1.7f;
            bodyComponent.levelAttackSpeed = 0.3f;
            bodyComponent.baseCrit = 1.8f;
            bodyComponent.levelCrit = 0.45f;
            bodyComponent.baseArmor = 0;
            bodyComponent.levelArmor = 0;
            bodyComponent.baseJumpCount = 1;
            bodyComponent.sprintingSpeedMultiplier = 1.55f;
            bodyComponent.wasLucky = false;
            bodyComponent.hideCrosshair = false;
            bodyComponent.aimOriginTransform = gameObject3.transform;
            bodyComponent.hullClassification = HullClassification.Human;
            bodyComponent.portraitIcon = Assets.charPortrait;
            bodyComponent.isChampion = false;
            bodyComponent.currentVehicle = null;
            bodyComponent.skinIndex = 0U;

            // the charactermotor controls the survivor's movement and stuff
            CharacterMotor characterMotor = characterPrefab.GetComponent<CharacterMotor>();
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.characterDirection = characterDirection;
            characterMotor.muteWalkMotion = false;
            characterMotor.mass = 100f;
            characterMotor.airControl = 0.55f;
            characterMotor.disableAirControlUntilCollision = false;
            characterMotor.generateParametersOnAwake = true;
            //characterMotor.useGravity = true;
            //characterMotor.isFlying = false;

            InputBankTest inputBankTest = characterPrefab.GetComponent<InputBankTest>();
            inputBankTest.moveVector = Vector3.zero;

            CameraTargetParams cameraTargetParams = characterPrefab.GetComponent<CameraTargetParams>();
            cameraTargetParams.cameraParams = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<CameraTargetParams>().cameraParams;
            cameraTargetParams.cameraPivotTransform = null;
            cameraTargetParams.aimMode = CameraTargetParams.AimType.Standard;
            cameraTargetParams.recoil = Vector2.zero;
            cameraTargetParams.idealLocalCameraPos = Vector3.zero;
            cameraTargetParams.dontRaycastToPivot = false;

            // this component is used to locate the character model(duh), important to set this up here
            ModelLocator modelLocator = characterPrefab.GetComponent<ModelLocator>();
            modelLocator.modelTransform = transform;
            modelLocator.modelBaseTransform = gameObject.transform;
            modelLocator.dontReleaseModelOnDeath = false;
            modelLocator.autoUpdateModelTransform = true;
            modelLocator.dontDetatchFromParent = false;
            modelLocator.noCorpse = false;
            modelLocator.normalizeToFloor = false; // set true if you want your character to rotate on terrain like acrid does
            modelLocator.preserveModel = false;

            // childlocator is something that must be set up in the unity project, it's used to find any child objects for things like footsteps or muzzle flashes
            // also important to set up if you want quality
            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            // this component is used to handle all overlays and whatever on your character, without setting this up you won't get any cool effects like burning or freeze on the character
            // it goes on the model object of course
            CharacterModel characterModel = model.AddComponent<CharacterModel>();
            characterModel.body = bodyComponent;
            characterModel.baseRendererInfos = new CharacterModel.RendererInfo[]
            {
                // set up multiple rendererinfos if needed, but for this example there's only the one
                new CharacterModel.RendererInfo
                {
                    defaultMaterial = model.GetComponentInChildren<SkinnedMeshRenderer>().material,
                    renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(),
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false
                }
            };

            characterModel.autoPopulateLightInfos = true;
            characterModel.invisibilityCount = 0;
            characterModel.temporaryOverlays = new List<TemporaryOverlay>();

            TeamComponent teamComponent = null;
            if (characterPrefab.GetComponent<TeamComponent>() != null) teamComponent = characterPrefab.GetComponent<TeamComponent>();
            else teamComponent = characterPrefab.GetComponent<TeamComponent>();
            teamComponent.hideAllyCardDisplay = false;
            teamComponent.teamIndex = TeamIndex.None;

            HealthComponent healthComponent = characterPrefab.GetComponent<HealthComponent>();
            healthComponent.health = 90f;
            healthComponent.shield = 0f;
            healthComponent.barrier = 0f;
            healthComponent.magnetiCharge = 0f;
            healthComponent.body = null;
            healthComponent.dontShowHealthbar = false;
            healthComponent.globalDeathEventChanceCoefficient = 1f;

            characterPrefab.GetComponent<Interactor>().maxInteractionDistance = 3f;
            characterPrefab.GetComponent<InteractionDriver>().highlightInteractor = true;

            // this disables ragdoll since the character's not set up for it, and instead plays a death animation
            CharacterDeathBehavior characterDeathBehavior = characterPrefab.GetComponent<CharacterDeathBehavior>();
            characterDeathBehavior.deathStateMachine = characterPrefab.GetComponent<EntityStateMachine>();
            characterDeathBehavior.deathState = new SerializableEntityStateType(typeof(GenericCharacterDeath));

            // edit the sfxlocator if you want different sounds
            SfxLocator sfxLocator = characterPrefab.GetComponent<SfxLocator>();
            sfxLocator.deathSound = Sounds.axlDie;
            sfxLocator.barkSound = "";
            sfxLocator.openSound = "";
            sfxLocator.landingSound = "Play_char_land";
            sfxLocator.fallDamageSound = "Play_char_land_fall_damage";
            sfxLocator.aliveLoopStart = "";
            sfxLocator.aliveLoopStop = "";

            Rigidbody rigidbody = characterPrefab.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            CapsuleCollider capsuleCollider = characterPrefab.GetComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = false;
            capsuleCollider.material = null;
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;

            KinematicCharacterMotor kinematicCharacterMotor = characterPrefab.GetComponent<KinematicCharacterMotor>();
            kinematicCharacterMotor.CharacterController = characterMotor;
            kinematicCharacterMotor.Capsule = capsuleCollider;
            kinematicCharacterMotor.Rigidbody = rigidbody;

            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.center = new Vector3(0, 0, 0);
            capsuleCollider.material = null;

            kinematicCharacterMotor.DetectDiscreteCollisions = false;
            kinematicCharacterMotor.GroundDetectionExtraDistance = 0f;
            kinematicCharacterMotor.MaxStepHeight = 0.2f;
            kinematicCharacterMotor.MinRequiredStepDepth = 0.1f;
            kinematicCharacterMotor.MaxStableSlopeAngle = 55f;
            kinematicCharacterMotor.MaxStableDistanceFromLedge = 0.5f;
            kinematicCharacterMotor.PreventSnappingOnLedges = false;
            kinematicCharacterMotor.MaxStableDenivelationAngle = 55f;
            kinematicCharacterMotor.RigidbodyInteractionType = RigidbodyInteractionType.None;
            kinematicCharacterMotor.PreserveAttachedRigidbodyMomentum = true;
            kinematicCharacterMotor.HasPlanarConstraint = false;
            kinematicCharacterMotor.PlanarConstraintAxis = Vector3.up;
            kinematicCharacterMotor.StepHandling = StepHandlingMethod.None;
            kinematicCharacterMotor.LedgeHandling = true;
            kinematicCharacterMotor.InteractiveRigidbodyHandling = true;
            kinematicCharacterMotor.SafeMovement = false;

            // this sets up the character's hurtbox, kinda confusing, but should be fine as long as it's set up in unity right
            HurtBoxGroup hurtBoxGroup = model.AddComponent<HurtBoxGroup>();

            HurtBox componentInChildren = model.GetComponentInChildren<CapsuleCollider>().gameObject.AddComponent<HurtBox>();
            componentInChildren.gameObject.layer = LayerIndex.entityPrecise.intVal;
            componentInChildren.healthComponent = healthComponent;
            componentInChildren.isBullseye = true;
            componentInChildren.damageModifier = HurtBox.DamageModifier.Normal;
            componentInChildren.hurtBoxGroup = hurtBoxGroup;
            componentInChildren.indexInGroup = 0;

            hurtBoxGroup.hurtBoxes = new HurtBox[]
            {
                componentInChildren
            };

            hurtBoxGroup.mainHurtBox = componentInChildren;
            hurtBoxGroup.bullseyeCount = 1;

            // this is for handling footsteps, not needed but polish is always good
            FootstepHandler footstepHandler = model.AddComponent<FootstepHandler>();
            footstepHandler.baseFootstepString = "Play_player_footstep";
            footstepHandler.sprintFootstepOverrideString = "";
            footstepHandler.enableFootstepDust = true;
            footstepHandler.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericFootstepDust");

            // ragdoll controller is a pain to set up so we won't be doing that here..
            RagdollController ragdollController = model.AddComponent<RagdollController>();
            ragdollController.bones = null;
            ragdollController.componentsToDisableOnRagdoll = null;

            // this handles the pitch and yaw animations, but honestly they are nasty and a huge pain to set up so i didn't bother
            AimAnimator aimAnimator = model.AddComponent<AimAnimator>();
            aimAnimator.inputBank = inputBankTest;
            aimAnimator.directionComponent = characterDirection;
            aimAnimator.pitchRangeMax = 55f;
            aimAnimator.pitchRangeMin = -50f;
            aimAnimator.yawRangeMin = -44f;
            aimAnimator.yawRangeMax = 44f;
            aimAnimator.pitchGiveupRange = 30f;
            aimAnimator.yawGiveupRange = 10f;
            aimAnimator.giveupDuration = 8f;
        }

        private void RegisterCharacter()
        {
            // now that the body prefab's set up, clone it here to make the display prefab
            characterDisplay = PrefabAPI.InstantiateClone(characterPrefab.GetComponent<ModelLocator>().modelBaseTransform.gameObject, "AxlDisplay", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 153);
            characterDisplay.AddComponent<NetworkIdentity>();

            //-----------------------------------------------------------------------------------------------------------

            // clone rex's syringe projectile prefab here to use as our own projectile
            axlBullet = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/TitanRockProjectile"), "Prefabs/Projectiles/ExampleArrowProjectile", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 155);

            // just setting the numbers to 1 as the entitystate will take care of those
            axlBullet.GetComponent<ProjectileController>().procCoefficient = 1f;
            axlBullet.GetComponent<ProjectileDamage>().damage = 1f;
            axlBullet.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;

            // register it for networking
            if (axlBullet) PrefabAPI.RegisterNetworkPrefab(axlBullet);

            //-------------------------------------------------------------------------------------------------------------

            // clone rex's syringe projectile prefab here to use as our own projectile
            blastL = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/EngiGrenadeProjectile"), "Prefabs/Projectiles/blastLProjectile", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 155);

            // just setting the numbers to 1 as the entitystate will take care of those
            blastL.GetComponent<ProjectileController>().procCoefficient = 1f;
            blastL.GetComponent<ProjectileDamage>().damage = 1f;
            blastL.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;

            // register it for networking
            if (blastL) PrefabAPI.RegisterNetworkPrefab(blastL);

            //-------------------------------------------------------------------------------------------------------------

            // clone rex's syringe projectile prefab here to use as our own projectile
            rayG = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/Arrow"), "Prefabs/Projectiles/blastLProjectile", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 155);

            // just setting the numbers to 1 as the entitystate will take care of those
            rayG.GetComponent<ProjectileController>().procCoefficient = 1f;
            rayG.GetComponent<ProjectileDamage>().damage = 1f;
            rayG.GetComponent<ProjectileDamage>().damageType = DamageType.Shock5s;

            // register it for networking
            if (rayG) PrefabAPI.RegisterNetworkPrefab(rayG);

            // add it to the projectile catalog or it won't work in multiplayer
            ProjectileCatalog.getAdditionalEntries += list =>
            {
                list.Add(axlBullet);
                list.Add(blastL);
                list.Add(rayG);
            };


            // write a clean survivor description here!
            string desc = "New generation reploid, Axl.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Axl Bullets is Axl's standard weapon. It's capable of rapid fire shots." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Axl is really fast has high Attack Speed and Movement Speed." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Axl's Emergency Acceleration System(DASH) is a move that temporarily speeds up the character." + Environment.NewLine + Environment.NewLine;
            //desc = desc + "< ! > Sample Text 4.</color>" + Environment.NewLine + Environment.NewLine;

            // add the language tokens
            LanguageAPI.Add("AXL_NAME", "AXL");
            LanguageAPI.Add("AXL_DESCRIPTION", desc);
            LanguageAPI.Add("AXL_SUBTITLE", "AXL as a survivor for Risk Of Rain 2");

            // add our new survivor to the game~
            SurvivorDef survivorDef = new SurvivorDef
            {
                name = "AXL_NAME",
                unlockableName = "",
                descriptionToken = "AXL_DESCRIPTION",
                primaryColor = characterColor,
                bodyPrefab = characterPrefab,
                displayPrefab = characterDisplay
            };


            SurvivorAPI.AddSurvivor(survivorDef);

            // set up the survivor's skills here
            SkillSetup();

            // gotta add it to the body catalog too
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(characterPrefab);
            };
        }

        void SkillSetup()
        {
            // get rid of the original skills first, otherwise we'll have commando's loadout and we don't want that
            foreach (GenericSkill obj in characterPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }

            PassiveSetup();
            PrimarySetup();
            SecondarySetup();
            UtilitySetup();
            SpecialSetup();
        }

        void RegisterStates()
        {
            // register the entitystates for networking reasons
            LoadoutAPI.AddSkill(typeof(axlBullet1));
        }

        void PassiveSetup()
        {
            // set up the passive skill here if you want
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("AXL_PASSIVE_NAME", "AXLleration");
            LanguageAPI.Add("AXL_PASSIVE_DESCRIPTION", "<style=cIsUtility>Axl get super fast by leveling up!</style> <style=cIsHealing>Move Speed, Attack Speed and Crit grows on level up</style>.");

            component.passiveSkill.enabled = true;
            component.passiveSkill.skillNameToken = "AXL_PASSIVE_NAME";
            component.passiveSkill.skillDescriptionToken = "AXL_PASSIVE_DESCRIPTION";
            component.passiveSkill.icon = Assets.iconP;
        }

        void PrimarySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("AXL_PRIMARY_NAME", "AXL-Bullet");
            LanguageAPI.Add("AXL_PRIMARY_DESCRIPTION", "Dual pistols, dealing <style=cIsDamage>80% damage</style> and <style=cIsDamage>90% damage</style>. ");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(axlBullet1));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon1;
            mySkillDef.skillDescriptionToken = "AXL_PRIMARY_DESCRIPTION";
            mySkillDef.skillName = "AXL_PRIMARY_NAME";
            mySkillDef.skillNameToken = "AXL_PRIMARY_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.primary = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };


            // add this code after defining a new skilldef if you're adding an alternate skill

            /*Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(newSkillDef.skillNameToken, false, null)
            };*/
        }

        void SecondarySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("AXL_SECONDARY_NAME", "Blast Launcher");
            LanguageAPI.Add("AXL_SECONDARY_DESCRIPTION", "This weapon launches time-activated grenades which bounce for a few seconds before detonating, dealing <style=cIsDamage>180% damage</style> and <style=cIsDamage>200% damage</style>. ");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(blastLauncher));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 3;
            mySkillDef.baseRechargeInterval = 5f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0.5f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon2;
            mySkillDef.skillDescriptionToken = "AXL_SECONDARY_DESCRIPTION";
            mySkillDef.skillName = "AXL_SECONDARY_NAME";
            mySkillDef.skillNameToken = "AXL_SECONDARY_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.secondary = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.secondary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.secondary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };


            // add this code after defining a new skilldef if you're adding an alternate skill

            /*Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(newSkillDef.skillNameToken, false, null)
            };*/
        }

        void UtilitySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("AXL_UTILITY_NAME", "Dash");
            LanguageAPI.Add("AXL_UTILITY_DESCRIPTION", "<style=cIsDamage>Perform a Dash</style>.");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Dash));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 2f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon3;
            mySkillDef.skillDescriptionToken = "AXL_UTILITY_DESCRIPTION";
            mySkillDef.skillName = "AXL_UTILITY_NAME";
            mySkillDef.skillNameToken = "AXL_UTILITY_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.utility = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };


            // add this code after defining a new skilldef if you're adding an alternate skill

            /*Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(newSkillDef.skillNameToken, false, null)
            };*/
        }

        void SpecialSetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("AXL_SPECIAL_NAME", "Ray Gun");
            LanguageAPI.Add("AXL_SPECIAL_DESCRIPTION", "A rapid firing laser that can shock some enemies, dealing <style=cIsDamage>50% damage</style>, <style=cIsDamage>60% damage</style> and <style=cIsDamage>80% damage</style>. ");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(rayGun));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 2;
            mySkillDef.baseRechargeInterval = 4.8f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0.2f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon4;
            mySkillDef.skillDescriptionToken = "AXL_SPECIAL_DESCRIPTION";
            mySkillDef.skillName = "AXL_SPECIAL_NAME";
            mySkillDef.skillNameToken = "AXL_SPECIAL_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.special = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.special.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.special.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };


            // add this code after defining a new skilldef if you're adding an alternate skill

            /*Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(newSkillDef.skillNameToken, false, null)
            };*/
        }

        private void CreateDoppelganger()
        {
            // set up the doppelganger for artifact of vengeance here
            // quite simple, gets a bit more complex if you're adding your own ai, but commando ai will do

            doppelganger = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), "AXLMonsterMaster", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreateDoppelganger", 159);

            MasterCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(doppelganger);
            };

            CharacterMaster component = doppelganger.GetComponent<CharacterMaster>();
            component.bodyPrefab = characterPrefab;
        }
    }



    // get the assets from your assetbundle here
    // if it's returning null, check and make sure you have the build action set to "Embedded Resource" and the file names are right because it's not gonna work otherwise
    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static AssetBundleResourcesProvider Provider;

        public static Texture charPortrait;

        public static Sprite iconP;
        public static Sprite icon1;
        public static Sprite icon2;
        public static Sprite icon3;
        public static Sprite icon4;

        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MegamanXAXLMod.megamanxaxlmodbundle"))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                    Provider = new AssetBundleResourcesProvider("@ExampleSurvivor", MainAssetBundle);
                }
            }

            // include this if you're using a custom soundbank
            using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("MegamanXAXLMod.AXLSB.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }

            // and now we gather the assets
            charPortrait = MainAssetBundle.LoadAsset<Sprite>("AxlIcon").texture;

            iconP = MainAssetBundle.LoadAsset<Sprite>("AxlIcon");
            icon1 = MainAssetBundle.LoadAsset<Sprite>("Skill1Icon");
            icon2 = MainAssetBundle.LoadAsset<Sprite>("Skill2Icon");
            icon3 = MainAssetBundle.LoadAsset<Sprite>("Skill3Icon");
            icon4 = MainAssetBundle.LoadAsset<Sprite>("Skill4Icon");
        }
    }
}

public static class Sounds 
{
    public static readonly string axlBullet = "CallAXLBullets";
    public static readonly string axlAttacks = "CallAXLAttack";
    public static readonly string axlRayGun = "CallAXLRayGun";
    public static readonly string axlDie = "CallAXLDie";
    public static readonly string axlDash = "CallAXLDash";
    public static readonly string axlBlastLauncher = "CallBlastLauncher";
}


// the entitystates namespace is used to make the skills, i'm not gonna go into detail here but it's easy to learn through trial and error
namespace EntityStates.ExampleSurvivorStates
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
            this.muzzleString = "Muzzle";
            

            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
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
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

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


namespace EntityStates.ExampleSurvivorStates
{
    public class axlBullet2 : BaseSkillState
    {
        public float damageCoefficient = 0.9f;
        public float baseDuration = 0.30f;
        public float recoil = 0.5f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/TracerBanditPistol");
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
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
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlBullet, base.gameObject);
                    // ProjectileManager.instance.FireProjectile(axlBullet2.tracerEffectPrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
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
                        tracerEffectPrefab = axlBullet2.tracerEffectPrefab,
                        muzzleName = muzzleString,
                        hitEffectPrefab = axlBullet2.hitEffectPrefab,
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
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}

namespace EntityStates.ExampleSurvivorStates
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
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
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlAttacks, base.gameObject);
                    Util.PlaySound(Sounds.axlBlastLauncher, base.gameObject);
                    ProjectileManager.instance.FireProjectile(ExampleSurvivor.MegamanXAXLMod.blastL, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    
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

namespace EntityStates.ExampleSurvivorStates
{
    public class blastLauncher2 : BaseSkillState
    {
        public float damageCoefficient = 2f;
        public float baseDuration = 0.38f;
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
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
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlBlastLauncher, base.gameObject);
                    ProjectileManager.instance.FireProjectile(ExampleSurvivor.MegamanXAXLMod.blastL, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

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
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}

namespace EntityStates.ExampleSurvivorStates
{
    public class rayGun : BaseSkillState
    {
        public float damageCoefficient = 0.5f;
        public float baseDuration = 0.10f;
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireRG()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlAttacks, base.gameObject);
                    Util.PlaySound(Sounds.axlRayGun, base.gameObject);
                    ProjectileManager.instance.FireProjectile(ExampleSurvivor.MegamanXAXLMod.rayG, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireRG();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                rayGun2 RG2 = new rayGun2();
                this.outer.SetNextState(RG2);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}

namespace EntityStates.ExampleSurvivorStates
{
    public class rayGun2 : BaseSkillState
    {
        public float damageCoefficient = 0.6f;
        public float baseDuration = 0.15f;
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireRG()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlRayGun, base.gameObject);
                    ProjectileManager.instance.FireProjectile(ExampleSurvivor.MegamanXAXLMod.rayG, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireRG();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                rayGun3 RG3 = new rayGun3();
                this.outer.SetNextState(RG3);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}


namespace EntityStates.ExampleSurvivorStates
{
    public class rayGun3 : BaseSkillState
    {
        public float damageCoefficient = 0.8f;
        public float baseDuration = 0.25f;
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
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireRG()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    Util.PlaySound(Sounds.axlRayGun, base.gameObject);
                    ProjectileManager.instance.FireProjectile(ExampleSurvivor.MegamanXAXLMod.rayG, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireRG();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}

namespace EntityStates.ExampleSurvivorStates
{
    public class Dash : BaseState
    {
        public float baseDuration = 0.75f;
        private float duration = 0.9f;
        private Animator animator;


        // Token: 0x06003E1F RID: 15903 RVA: 0x00102CA0 File Offset: 0x00100EA0
        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(Sounds.axlDash, base.gameObject);
            this.animator = base.GetModelAnimator();
            ChildLocator component = this.animator.GetComponent<ChildLocator>();
            if (base.isAuthority && base.inputBank && base.characterDirection)
            {
                this.forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
            }
            Vector3 rhs = base.characterDirection ? base.characterDirection.forward : this.forwardDirection;
            Vector3 rhs2 = Vector3.Cross(Vector3.up, rhs);
            float num = Vector3.Dot(this.forwardDirection, rhs);
            float num2 = Vector3.Dot(this.forwardDirection, rhs2);
            this.animator.SetFloat("forwardSpeed", num, 0.1f, Time.fixedDeltaTime);
            this.animator.SetFloat("rightSpeed", num2, 0.1f, Time.fixedDeltaTime);
            if (Mathf.Abs(num) > Mathf.Abs(num2))
            {
                base.PlayAnimation("Body", (num > 0f) ? "Dash" : "Dash", "Dodge.playbackRate", this.duration);
            }
            else
            {
                base.PlayAnimation("Body", (num2 > 0f) ? "Dash" : "Dash", "Dodge.playbackRate", this.duration);
            }
            if (Dash.jetEffect)
            {
                Transform transform = component.FindChild("LeftJet");
                Transform transform2 = component.FindChild("RightJet");
                if (transform)
                {
                    UnityEngine.Object.Instantiate<GameObject>(Dash.jetEffect, transform);
                }
                if (transform2)
                {
                    UnityEngine.Object.Instantiate<GameObject>(Dash.jetEffect, transform2);
                }

            }
            this.RecalculateRollSpeed();
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity.y = 0f;
                base.characterMotor.velocity = this.forwardDirection * this.rollSpeed;
            }
            Vector3 b = base.characterMotor ? base.characterMotor.velocity : Vector3.zero;
            this.previousPosition = base.transform.position - b;
        }

        // Token: 0x06003E20 RID: 15904 RVA: 0x00102EFD File Offset: 0x001010FD
        private void RecalculateRollSpeed()
        {
            this.rollSpeed = this.moveSpeedStat * Mathf.Lerp(this.initialSpeedCoefficient, this.finalSpeedCoefficient, base.fixedAge / this.duration);
        }

        // Token: 0x06003E21 RID: 15905 RVA: 0x00102F2C File Offset: 0x0010112C
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.RecalculateRollSpeed();
            if (base.cameraTargetParams)
            {
                //base.cameraTargetParams.fovOverride = Mathf.Lerp(DodgeState.dodgeFOV, 60f, base.fixedAge / this.duration);
                base.cameraTargetParams.fovOverride = -2.5f;
            }
            Vector3 normalized = (base.transform.position - this.previousPosition).normalized;
            if (base.characterMotor && base.characterDirection && normalized != Vector3.zero)
            {
                Vector3 vector = normalized * this.rollSpeed;
                float y = vector.y;
                vector.y = 0f;
                float d = Mathf.Max(Vector3.Dot(vector, this.forwardDirection), 0f);
                vector = this.forwardDirection * d;
                vector.y += Mathf.Max(y, 0f);
                base.characterMotor.velocity = vector;
            }
            this.previousPosition = base.transform.position;
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        // Token: 0x06003E22 RID: 15906 RVA: 0x0010305D File Offset: 0x0010125D
        public override void OnExit()
        {
            if (base.cameraTargetParams)
            {
                base.cameraTargetParams.fovOverride = -1f;
            }
            base.OnExit();
        }

        // Token: 0x06003E23 RID: 15907 RVA: 0x00103082 File Offset: 0x00101282
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.forwardDirection);
        }

        // Token: 0x06003E24 RID: 15908 RVA: 0x00103097 File Offset: 0x00101297
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.forwardDirection = reader.ReadVector3();
        }

        // Token: 0x0400392E RID: 14638

        public float initialSpeedCoefficient = 6f;

        // Token: 0x0400392F RID: 14639

        public float finalSpeedCoefficient = 5f;

        // Token: 0x04003930 RID: 14640
        public static string dodgeSoundString;

        // Token: 0x04003931 RID: 14641
        public static GameObject jetEffect;

        // Token: 0x04003932 RID: 14642
        public static float dodgeFOV;

        // Token: 0x04003933 RID: 14643
        private float rollSpeed = 5.8f;

        // Token: 0x04003934 RID: 14644
        private Vector3 forwardDirection;

        // Token: 0x04003936 RID: 14646
        private Vector3 previousPosition;
    }
}

