using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Player;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    public static class CharacterAnimationAssetBuilder
    {
        private const string OutputFolder = "Assets/_Project/Animation";
        private const string BaseControllerPath =
            OutputFolder + "/DeltatimeCharacter.controller";
        private const string PistolControllerPath =
            OutputFolder + "/Pistol.overrideController";
        private const string RifleControllerPath =
            OutputFolder + "/Rifle.overrideController";
        private const string MeleeControllerPath =
            OutputFolder + "/Melee.overrideController";
        private const string RollInPlaceClipPath =
            OutputFolder + "/DeltatimeRollInPlace.anim";
        private const string UpperBodyAttackMaskPath =
            OutputFolder + "/DeltatimeUpperBodyAttack.mask";
        private const string BaseballBatVisualPrefabPath =
            OutputFolder + "/BaseballBat_Raw_Wood_Clean.prefab";
        private const string TacticalPistolVisualPrefabPath =
            OutputFolder + "/TacticalPistol.prefab";
        private const string AssaultRifleVisualPrefabPath =
            OutputFolder + "/AssaultRifle.prefab";
        private const string PumpShotgunVisualPrefabPath =
            OutputFolder + "/PumpShotgun.prefab";
        private const string BaseballBatModelPath =
            "Assets/Modeling/Baseball Bat/FBX/Raw/" +
            "BaseballBat_Raw_Wood(Clean).fbx";
        private const string TacticalPistolModelPath =
            "Assets/MR POLY/Low Poly Weapons Set/Models/" +
            "Tactical Pistol.fbx";
        private const string AssaultRifleModelPath =
            "Assets/MR POLY/Low Poly Weapons Set/Models/" +
            "Assault Rifle.fbx";
        private const string PumpShotgunModelPath =
            "Assets/MR POLY/Low Poly Weapons Set/Models/" +
            "Pump Shotgun.fbx";
        private const string PistolWeaponDefinitionPath =
            "Assets/_Project/Pistol.asset";
        private const string AutomaticRifleWeaponDefinitionPath =
            "Assets/_Project/AutomaticRifle.asset";
        private const string ShotgunWeaponDefinitionPath =
            "Assets/_Project/Shotgun.asset";
        private const string MeleeWeaponDefinitionPath =
            "Assets/_Project/MeleeWeapon.asset";

        private const string Basic = "Assets/Animations/Basic/";
        private const string Pistol =
            "Assets/Animations/Pistol_Handgun Locomotion Pack/";
        private const string Rifle = "Assets/Animations/Shooter Pack/";
        private const string Melee =
            "Assets/Animations/Pro Melee Axe Pack/";

        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/Stage1.unity",
            "Assets/_Project/Scenes/Stage2.unity",
            "Assets/_Project/Scenes/Stage3.unity",
            "Assets/_Project/Scenes/Stage4.unity",
            "Assets/_Project/Scenes/Stage5.unity",
            "Assets/_Project/Scenes/Stage6.unity",
            GameBuildSceneCatalog.TutorialScenePath
        };

        private readonly struct MotionSet
        {
            public readonly AnimationClip Idle;
            public readonly AnimationClip Forward;
            public readonly AnimationClip Back;
            public readonly AnimationClip Left;
            public readonly AnimationClip Right;
            public readonly AnimationClip Roll;
            public readonly AnimationClip AttackA;
            public readonly AnimationClip AttackB;

            public MotionSet(
                AnimationClip idle,
                AnimationClip forward,
                AnimationClip back,
                AnimationClip left,
                AnimationClip right,
                AnimationClip roll,
                AnimationClip attackA,
                AnimationClip attackB)
            {
                Idle = idle;
                Forward = forward;
                Back = back;
                Left = left;
                Right = right;
                Roll = roll;
                AttackA = attackA;
                AttackB = attackB;
            }
        }

        [MenuItem("Tools/Prototype/Animation/Build And Apply Characters")]
        public static void BuildAndApply()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string previousScenePath = SceneManager.GetActiveScene().path;
            BuildAssets();
            int configuredCount = ApplyToScenes();
            AssetDatabase.SaveAssets();

            if (!Application.isBatchMode &&
                !string.IsNullOrEmpty(previousScenePath) &&
                File.Exists(Path.GetFullPath(previousScenePath)))
            {
                EditorSceneManager.OpenScene(
                    previousScenePath,
                    OpenSceneMode.Single);
            }

            Debug.Log(
                $"Character animation build completed. " +
                $"Configured actors: {configuredCount}.");
        }

        [MenuItem("Tools/Prototype/Animation/Build Weapon Models")]
        public static void BuildWeaponModels()
        {
            EnsureOutputFolder();
            BuildWeaponModelAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("Weapon model build completed.");
        }

        public static void BuildAndApplyFromCommandLine()
        {
            try
            {
                BuildAndApply();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildWeaponModelsFromCommandLine()
        {
            try
            {
                BuildWeaponModels();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static CharacterAnimationLibrary BuildAssets()
        {
            EnsureOutputFolder();
            NormalizeAnimationImports();

            AnimationClip rollInPlace = CreateInPlaceRollClip(
                LoadClip(Basic + "Ch03_nonPBR@Stand To Roll.fbx"));
            ValidateInPlaceRollClip(rollInPlace);
            AvatarMask upperBodyAttackMask = CreateUpperBodyAttackMask();

            MotionSet unarmed = new MotionSet(
                LoadClip(Melee + "unarmed idle.fbx"),
                LoadClip(Basic + "Ch03_nonPBR@Unarmed Walk Forward.fbx"),
                LoadClip(Melee + "unarmed walk back.fbx"),
                LoadClip(Basic + "Ch03_nonPBR@Left Strafe Walk.fbx"),
                LoadClip(Basic + "Ch03_nonPBR@Right Strafe Walk.fbx"),
                rollInPlace,
                LoadClip(Basic + "Ch03_nonPBR@LeftPunching.fbx"),
                LoadClip(Basic + "Ch03_nonPBR@RightPunching.fbx"));
            MotionSet pistol = new MotionSet(
                LoadClip(Pistol + "pistol idle.fbx"),
                LoadClip(Pistol + "pistol walk.fbx"),
                LoadClip(Pistol + "pistol walk backward.fbx"),
                LoadClip(Pistol + "pistol strafe.fbx"),
                LoadClip(Pistol + "pistol strafe (2).fbx"),
                unarmed.Roll,
                unarmed.AttackA,
                unarmed.AttackB);
            MotionSet rifle = new MotionSet(
                LoadClip(Rifle + "rifle aiming idle.fbx"),
                LoadClip(Rifle + "walking.fbx"),
                LoadClip(Rifle + "walking backwards.fbx"),
                LoadClip(Rifle + "strafe.fbx"),
                LoadClip(Rifle + "strafe (2).fbx"),
                unarmed.Roll,
                LoadClip(Rifle + "firing rifle.fbx"),
                LoadClip(Rifle + "firing rifle.fbx"));
            MotionSet melee = new MotionSet(
                LoadClip(Melee + "standing idle.fbx"),
                LoadClip(Melee + "standing walk forward.fbx"),
                LoadClip(Melee + "standing walk back.fbx"),
                LoadClip(Melee + "standing walk left.fbx"),
                LoadClip(Melee + "standing walk right.fbx"),
                unarmed.Roll,
                LoadClip(Melee + "standing melee attack horizontal.fbx"),
                LoadClip(Melee + "standing melee attack backhand.fbx"));

            DeleteGeneratedController(BaseControllerPath);
            DeleteGeneratedController(PistolControllerPath);
            DeleteGeneratedController(RifleControllerPath);
            DeleteGeneratedController(MeleeControllerPath);

            AnimatorController baseController = CreateBaseController(
                unarmed,
                upperBodyAttackMask,
                BaseControllerPath);
            AnimatorOverrideController pistolController =
                CreateOverrideController(
                    baseController,
                    unarmed,
                    pistol,
                    PistolControllerPath);
            AnimatorOverrideController rifleController =
                CreateOverrideController(
                    baseController,
                    unarmed,
                    rifle,
                    RifleControllerPath);
            AnimatorOverrideController meleeController =
                CreateOverrideController(
                    baseController,
                    unarmed,
                    melee,
                    MeleeControllerPath);

            CharacterAnimationLibrary library =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    CharacterAnimationEditorSetup.LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<
                    CharacterAnimationLibrary>();
                AssetDatabase.CreateAsset(
                    library,
                    CharacterAnimationEditorSetup.LibraryPath);
            }

            library.Configure(
                baseController,
                pistolController,
                rifleController,
                meleeController);
            EditorUtility.SetDirty(library);
            ConfigureWeaponAnimationStyles();
            BuildWeaponModelAssets();
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void BuildWeaponModelAssets()
        {
            GameObject baseballBatVisual = EnsureBaseballBatVisualPrefab();
            GameObject tacticalPistolVisual = EnsureFirearmVisualPrefab(
                TacticalPistolVisualPrefabPath,
                TacticalPistolModelPath,
                "Tactical Pistol",
                0.42f);
            GameObject assaultRifleVisual = EnsureFirearmVisualPrefab(
                AssaultRifleVisualPrefabPath,
                AssaultRifleModelPath,
                "Assault Rifle",
                0.96f);
            GameObject pumpShotgunVisual = EnsureFirearmVisualPrefab(
                PumpShotgunVisualPrefabPath,
                PumpShotgunModelPath,
                "Pump Shotgun",
                0.92f);

            ConfigureMeleeWeaponVisual(baseballBatVisual);
            ConfigureFirearmWeaponVisuals(
                tacticalPistolVisual,
                assaultRifleVisual,
                pumpShotgunVisual);
        }

        private static AnimatorController CreateBaseController(
            MotionSet motions,
            AvatarMask upperBodyAttackMask,
            string path)
        {
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackA", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackB", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            BlendTree locomotionTree = new BlendTree
            {
                name = "Directional Locomotion",
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            locomotionTree.AddChild(motions.Idle, Vector2.zero);
            locomotionTree.AddChild(motions.Forward, Vector2.up);
            locomotionTree.AddChild(motions.Back, Vector2.down);
            locomotionTree.AddChild(motions.Left, Vector2.left);
            locomotionTree.AddChild(motions.Right, Vector2.right);

            AnimatorState locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = locomotionTree;
            stateMachine.defaultState = locomotion;

            AnimatorState roll = stateMachine.AddState("Roll");
            roll.motion = motions.Roll;
            roll.speed = Mathf.Clamp(motions.Roll.length / 0.28f, 1f, 5f);

            AddAnyStateTriggerTransition(
                stateMachine,
                roll,
                "Roll",
                0.03f);
            AddExitTransition(roll, locomotion, 0.92f, 0.06f);

            CreateUpperBodyAttackLayer(
                controller,
                motions,
                upperBodyAttackMask);
            ValidateUpperBodyAttackLayer(controller, upperBodyAttackMask);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreateUpperBodyAttackLayer(
            AnimatorController controller,
            MotionSet motions,
            AvatarMask upperBodyAttackMask)
        {
            controller.AddLayer("Upper Body Attack");
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer attackLayer = layers[layers.Length - 1];
            attackLayer.defaultWeight = 1f;
            attackLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            attackLayer.iKPass = false;
            attackLayer.avatarMask = upperBodyAttackMask;
            layers[layers.Length - 1] = attackLayer;
            controller.layers = layers;

            AnimatorStateMachine attackStateMachine =
                controller.layers[controller.layers.Length - 1].stateMachine;
            AnimatorState empty = attackStateMachine.AddState("No Attack");
            attackStateMachine.defaultState = empty;

            AnimatorState attackA = attackStateMachine.AddState("Attack A");
            attackA.motion = motions.AttackA;
            AddMeleeImpactBehaviour(attackA, 0.48f);
            AnimatorState attackB = attackStateMachine.AddState("Attack B");
            attackB.motion = motions.AttackB;
            AddMeleeImpactBehaviour(attackB, 0.48f);
            AddAnyStateTriggerTransition(
                attackStateMachine,
                attackA,
                "AttackA",
                0.04f);
            AddAnyStateTriggerTransition(
                attackStateMachine,
                attackB,
                "AttackB",
                0.04f);
            AddExitTransition(attackA, empty, 0.88f, 0.08f);
            AddExitTransition(attackB, empty, 0.88f, 0.08f);
        }

        private static void AddMeleeImpactBehaviour(
            AnimatorState state,
            float normalizedImpactTime)
        {
            MeleeAttackImpactBehaviour behaviour =
                state.AddStateMachineBehaviour<MeleeAttackImpactBehaviour>();
            behaviour.ImpactNormalizedTime = normalizedImpactTime;
        }

        private static AvatarMask CreateUpperBodyAttackMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                UpperBodyAttackMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask
                {
                    name = "DeltatimeUpperBodyAttack"
                };
                AssetDatabase.CreateAsset(mask, UpperBodyAttackMaskPath);
            }

            for (int i = 0;
                 i < (int)AvatarMaskBodyPart.LastBodyPart;
                 i++)
            {
                mask.SetHumanoidBodyPartActive(
                    (AvatarMaskBodyPart)i,
                    false);
            }

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(
                AvatarMaskBodyPart.LeftFingers,
                true);
            mask.SetHumanoidBodyPartActive(
                AvatarMaskBodyPart.RightFingers,
                true);
            mask.transformCount = 0;
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static void ValidateUpperBodyAttackLayer(
            AnimatorController controller,
            AvatarMask expectedMask)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 2 ||
                layers[1].name != "Upper Body Attack" ||
                layers[1].avatarMask != expectedMask ||
                !Mathf.Approximately(layers[1].defaultWeight, 1f))
            {
                throw new InvalidOperationException(
                    "Character Animator requires a full-weight upper-body " +
                    "attack layer with the generated AvatarMask.");
            }
        }

        private static void AddAnyStateTriggerTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destination,
            string trigger,
            float duration)
        {
            AnimatorStateTransition transition =
                stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                trigger);
        }

        private static void AddExitTransition(
            AnimatorState source,
            AnimatorState destination,
            float exitTime,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
        }

        private static AnimatorOverrideController CreateOverrideController(
            AnimatorController baseController,
            MotionSet baseMotions,
            MotionSet replacement,
            string path)
        {
            AnimatorOverrideController controller =
                new AnimatorOverrideController(baseController)
                {
                    name = Path.GetFileNameWithoutExtension(path)
                };
            AssetDatabase.CreateAsset(controller, path);

            Dictionary<AnimationClip, AnimationClip> replacements =
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [baseMotions.Idle] = replacement.Idle,
                    [baseMotions.Forward] = replacement.Forward,
                    [baseMotions.Back] = replacement.Back,
                    [baseMotions.Left] = replacement.Left,
                    [baseMotions.Right] = replacement.Right,
                    [baseMotions.Roll] = replacement.Roll,
                    [baseMotions.AttackA] = replacement.AttackA,
                    [baseMotions.AttackB] = replacement.AttackB
                };
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
                new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip key = overrides[i].Key;
                if (key != null && replacements.TryGetValue(
                        key,
                        out AnimationClip value))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                        key,
                        value);
                }
            }

            controller.ApplyOverrides(overrides);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static int ApplyToScenes()
        {
            int configuredCount = 0;
            for (int i = 0; i < ScenePaths.Length; i++)
            {
                string scenePath = ScenePaths[i];
                if (!File.Exists(Path.GetFullPath(scenePath)))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                int sceneCount = ConfigureSceneActors(scene);
                if (sceneCount <= 0)
                {
                    continue;
                }

                configuredCount += sceneCount;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    throw new InvalidOperationException(
                        $"Failed to save animation setup in {scenePath}.");
                }
            }

            return configuredCount;
        }

        private static int ConfigureSceneActors(Scene scene)
        {
            HashSet<GameObject> owners = new HashSet<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                PlayerHealth[] players =
                    roots[i].GetComponentsInChildren<PlayerHealth>(true);
                for (int j = 0; j < players.Length; j++)
                {
                    owners.Add(players[j].gameObject);
                }

                EnemyHealth[] enemies =
                    roots[i].GetComponentsInChildren<EnemyHealth>(true);
                for (int j = 0; j < enemies.Length; j++)
                {
                    owners.Add(enemies[j].gameObject);
                }
            }

            int configuredCount = 0;
            foreach (GameObject owner in owners)
            {
                Animator animator = owner.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    continue;
                }

                if (CharacterAnimationEditorSetup.ConfigureCharacter(
                        owner,
                        animator.transform.root == owner.transform
                            ? animator.gameObject
                            : animator.transform
                                .GetComponentInParent<Animator>(true)
                                .gameObject))
                {
                    configuredCount++;
                }
            }

            return configuredCount;
        }

        private static void NormalizeAnimationImports()
        {
            Dictionary<string, bool> loopSettings = BuildLoopSettings();
            string[] guids = AssetDatabase.FindAssets(
                "t:Model",
                new[] { "Assets/Animations" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModelImporter importer =
                    AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup =
                        ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }

                if (loopSettings.TryGetValue(path, out bool loop))
                {
                    ModelImporterClipAnimation[] clips =
                        importer.defaultClipAnimations;
                    for (int j = 0; j < clips.Length; j++)
                    {
                        clips[j].loopTime = loop;
                        clips[j].loopPose = loop;
                        clips[j].lockRootRotation = true;
                        clips[j].lockRootHeightY = true;
                        clips[j].lockRootPositionXZ = true;
                    }

                    importer.clipAnimations = clips;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Dictionary<string, bool> BuildLoopSettings()
        {
            Dictionary<string, bool> settings =
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            string[] loopingNames =
            {
                Melee + "unarmed idle.fbx",
                Basic + "Ch03_nonPBR@Unarmed Walk Forward.fbx",
                Melee + "unarmed walk back.fbx",
                Basic + "Ch03_nonPBR@Left Strafe Walk.fbx",
                Basic + "Ch03_nonPBR@Right Strafe Walk.fbx",
                Pistol + "pistol idle.fbx",
                Pistol + "pistol walk.fbx",
                Pistol + "pistol walk backward.fbx",
                Pistol + "pistol strafe.fbx",
                Pistol + "pistol strafe (2).fbx",
                Rifle + "rifle aiming idle.fbx",
                Rifle + "walking.fbx",
                Rifle + "walking backwards.fbx",
                Rifle + "strafe.fbx",
                Rifle + "strafe (2).fbx",
                Melee + "standing idle.fbx",
                Melee + "standing walk forward.fbx",
                Melee + "standing walk back.fbx",
                Melee + "standing walk left.fbx",
                Melee + "standing walk right.fbx"
            };
            for (int i = 0; i < loopingNames.Length; i++)
            {
                settings[loopingNames[i]] = true;
            }

            settings[Basic + "Ch03_nonPBR@Stand To Roll.fbx"] = false;
            settings[Basic + "Ch03_nonPBR@LeftPunching.fbx"] = false;
            settings[Basic + "Ch03_nonPBR@RightPunching.fbx"] = false;
            settings[Rifle + "firing rifle.fbx"] = false;
            settings[Melee + "standing melee attack horizontal.fbx"] = false;
            settings[Melee + "standing melee attack backhand.fbx"] = false;
            return settings;
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate =>
                    !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Animation clip is missing at {path}.");
            }

            return clip;
        }

        private static AnimationClip CreateInPlaceRollClip(
            AnimationClip source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            DeleteGeneratedController(RollInPlaceClipPath);
            AnimationClip inPlace = new AnimationClip();
            EditorUtility.CopySerialized(source, inPlace);
            inPlace.name = "DeltatimeRollInPlace";

            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(inPlace);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.type != typeof(Animator) ||
                    (binding.propertyName != "RootT.x" &&
                     binding.propertyName != "RootT.z"))
                {
                    continue;
                }

                AnimationCurve sourceCurve =
                    AnimationUtility.GetEditorCurve(inPlace, binding);
                float startValue = sourceCurve == null ||
                    sourceCurve.length == 0
                    ? 0f
                    : sourceCurve.keys[0].value;
                AnimationUtility.SetEditorCurve(
                    inPlace,
                    binding,
                    AnimationCurve.Constant(
                        0f,
                        Mathf.Max(0.001f, inPlace.length),
                        startValue));
            }

            AssetDatabase.CreateAsset(inPlace, RollInPlaceClipPath);
            EditorUtility.SetDirty(inPlace);
            return inPlace;
        }

        private static void ValidateInPlaceRollClip(AnimationClip roll)
        {
            string[] rootPositionProperties = { "RootT.x", "RootT.z" };
            for (int propertyIndex = 0;
                 propertyIndex < rootPositionProperties.Length;
                 propertyIndex++)
            {
                string property = rootPositionProperties[propertyIndex];
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    path = string.Empty,
                    type = typeof(Animator),
                    propertyName = property
                };
                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(roll, binding);
                if (curve == null || curve.length == 0)
                {
                    throw new InvalidOperationException(
                        $"In-place roll is missing {property}.");
                }

                float expected = curve.keys[0].value;
                for (int keyIndex = 1;
                     keyIndex < curve.length;
                     keyIndex++)
                {
                    if (!Mathf.Approximately(
                            curve.keys[keyIndex].value,
                            expected))
                    {
                        throw new InvalidOperationException(
                            $"In-place roll still translates on {property}.");
                    }
                }
            }
        }

        private static void ConfigureWeaponAnimationStyles()
        {
            SetWeaponStyle(
                PistolWeaponDefinitionPath,
                CharacterAnimationStyle.Pistol);
            SetWeaponStyle(
                AutomaticRifleWeaponDefinitionPath,
                CharacterAnimationStyle.Rifle);
            SetWeaponStyle(
                ShotgunWeaponDefinitionPath,
                CharacterAnimationStyle.Rifle);
            SetWeaponStyle(
                "Assets/_Project/MeleeWeapon.asset",
                CharacterAnimationStyle.Melee);
        }

        private static GameObject EnsureBaseballBatVisualPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                BaseballBatModelPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "The selected baseball bat model is missing at " +
                    BaseballBatModelPath + ".");
            }

            DeleteGeneratedController(BaseballBatVisualPrefabPath);
            GameObject root = new GameObject("Baseball Bat Raw Wood Clean");
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (model == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    "Failed to instantiate the selected baseball bat model.");
            }

            model.transform.SetParent(root.transform, false);
            NormalizeBaseballBatModel(model);
            PrefabUtility.SaveAsPrefabAsset(root, BaseballBatVisualPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                BaseballBatVisualPrefabPath);
        }

        private static GameObject EnsureFirearmVisualPrefab(
            string visualPrefabPath,
            string sourceModelPath,
            string displayName,
            float targetLength)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                sourceModelPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"The selected {displayName} model is missing at " +
                    sourceModelPath + ".");
            }

            DeleteGeneratedController(visualPrefabPath);
            GameObject root = new GameObject(displayName);
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (model == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Failed to instantiate the selected {displayName} model.");
            }

            model.transform.SetParent(root.transform, false);
            NormalizeFirearmModel(model, targetLength);
            PrefabUtility.SaveAsPrefabAsset(root, visualPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);
        }

        private static void NormalizeFirearmModel(
            GameObject model,
            float targetLength)
        {
            Bounds initialBounds = CalculateRendererBounds(model);
            Vector3 initialSize = initialBounds.size;
            Vector3 longAxis = initialSize.x >= initialSize.y &&
                               initialSize.x >= initialSize.z
                ? Vector3.right
                : initialSize.y >= initialSize.z
                    ? Vector3.up
                    : Vector3.forward;
            model.transform.localRotation = Quaternion.FromToRotation(
                longAxis,
                Vector3.forward);

            Bounds rotatedBounds = CalculateRendererBounds(model);
            float length = Mathf.Max(
                0.001f,
                Mathf.Max(
                    rotatedBounds.size.x,
                    Mathf.Max(rotatedBounds.size.y, rotatedBounds.size.z)));
            model.transform.localScale *= targetLength / length;

            Bounds normalizedBounds = CalculateRendererBounds(model);
            model.transform.localPosition -= normalizedBounds.center;
            model.transform.localPosition +=
                Vector3.forward * normalizedBounds.extents.z;
        }

        private static void NormalizeBaseballBatModel(GameObject model)
        {
            Bounds initialBounds = CalculateRendererBounds(model);
            Vector3 initialSize = initialBounds.size;
            Vector3 longAxis = initialSize.x >= initialSize.y &&
                               initialSize.x >= initialSize.z
                ? Vector3.right
                : initialSize.y >= initialSize.z
                    ? Vector3.up
                    : Vector3.forward;
            model.transform.localRotation = Quaternion.FromToRotation(
                longAxis,
                Vector3.forward);

            Bounds rotatedBounds = CalculateRendererBounds(model);
            float length = Mathf.Max(
                0.001f,
                Mathf.Max(
                    rotatedBounds.size.x,
                    Mathf.Max(rotatedBounds.size.y, rotatedBounds.size.z)));
            model.transform.localScale *= 0.92f / length;

            Bounds normalizedBounds = CalculateRendererBounds(model);
            model.transform.localPosition -= normalizedBounds.center;
            model.transform.localPosition +=
                Vector3.forward * normalizedBounds.extents.z;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Baseball bat visual has no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void ConfigureMeleeWeaponVisual(GameObject baseballBat)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    MeleeWeaponDefinitionPath);
            if (definition == null || baseballBat == null)
            {
                throw new InvalidOperationException(
                    "Melee weapon visual setup requires the weapon definition " +
                    "and generated baseball bat prefab.");
            }

            if (!definition.HasCustomHeldVisual ||
                !definition.HasCustomWorldVisual)
            {
                definition.ConfigureModelVisuals(
                    baseballBat,
                    baseballBat,
                    new Vector3(0f, 0f, 0.04f),
                    Vector3.zero,
                    Vector3.one,
                    new Vector3(0f, 0.1f, 0f),
                    Vector3.zero,
                    Vector3.one,
                    Vector3.forward * 0.92f,
                    Vector3.zero);
            }
            else
            {
                definition.ConfigureModelVisualPrefabs(
                    baseballBat,
                    baseballBat);
                if (!definition.HasHeldMuzzleCalibration)
                {
                    definition.ConfigureHeldMuzzle(
                        Vector3.forward * 0.92f,
                        Vector3.zero);
                }
            }
            EditorUtility.SetDirty(definition);
        }

        private static void ConfigureFirearmWeaponVisuals(
            GameObject tacticalPistol,
            GameObject assaultRifle,
            GameObject pumpShotgun)
        {
            ConfigureWeaponVisual(
                PistolWeaponDefinitionPath,
                tacticalPistol,
                new Vector3(0.02f, 0.06f, -0.1f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0f, 0f, 0.42f));
            ConfigureWeaponVisual(
                AutomaticRifleWeaponDefinitionPath,
                assaultRifle,
                new Vector3(0.02f, 0.13f, -0.28f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0.13f, 0f),
                new Vector3(0f, 0f, 0.96f));
            ConfigureWeaponVisual(
                ShotgunWeaponDefinitionPath,
                pumpShotgun,
                new Vector3(0.02f, 0.14f, -0.26f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0.14f, 0f),
                new Vector3(0f, 0f, 0.92f));
        }

        private static void ConfigureWeaponVisual(
            string definitionPath,
            GameObject visualPrefab,
            Vector3 heldPosition,
            Vector3 heldEulerAngles,
            Vector3 worldPosition,
            Vector3 muzzlePosition)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(definitionPath);
            if (definition == null || visualPrefab == null)
            {
                throw new InvalidOperationException(
                    "Weapon visual setup requires the weapon definition and " +
                    "generated model prefab at " + definitionPath + ".");
            }

            if (!definition.HasCustomHeldVisual ||
                !definition.HasCustomWorldVisual)
            {
                definition.ConfigureModelVisuals(
                    visualPrefab,
                    visualPrefab,
                    heldPosition,
                    heldEulerAngles,
                    Vector3.one,
                    worldPosition,
                    Vector3.zero,
                    Vector3.one,
                    muzzlePosition,
                    Vector3.zero);
            }
            else
            {
                definition.ConfigureModelVisualPrefabs(
                    visualPrefab,
                    visualPrefab);
                if (!definition.HasHeldMuzzleCalibration)
                {
                    definition.ConfigureHeldMuzzle(muzzlePosition, Vector3.zero);
                }
            }
            EditorUtility.SetDirty(definition);
        }

        private static void SetWeaponStyle(
            string path,
            CharacterAnimationStyle style)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Weapon definition is missing at {path}.");
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty("animationStyle");
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Animation style property is missing on {path}.");
            }

            property.enumValueIndex = (int)style;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Animation");
            }
        }

        private static void DeleteGeneratedController(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    $"Failed to replace generated animation asset {path}.");
            }
        }
    }
}
