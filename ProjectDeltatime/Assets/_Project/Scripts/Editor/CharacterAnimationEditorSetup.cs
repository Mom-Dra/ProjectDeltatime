using Deltatime.Combat;
using Deltatime.Visuals;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    public static class CharacterAnimationEditorSetup
    {
        public const string LibraryPath =
            "Assets/_Project/Animation/CharacterAnimationLibrary.asset";

        public static bool ConfigureCharacter(
            GameObject owner,
            GameObject visual)
        {
            if (owner == null || visual == null)
            {
                return false;
            }

            CharacterAnimationLibrary library =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    LibraryPath);
            if (library == null)
            {
                return false;
            }

            Animator[] animators =
                visual.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0)
            {
                return false;
            }

            Animator primaryAnimator = animators[0];
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                candidate.applyRootMotion = false;
                candidate.enabled = candidate == primaryAnimator;
                if (candidate == primaryAnimator)
                {
                    candidate.updateMode = AnimatorUpdateMode.UnscaledTime;
                    candidate.cullingMode =
                        AnimatorCullingMode.CullUpdateTransforms;
                }

                EditorUtility.SetDirty(candidate);
            }

            WeaponController weapon = owner.GetComponent<WeaponController>();
            CharacterAnimationStyle initialStyle = library.ResolveStyle(
                weapon == null ? null : weapon.StartingDefinition);

            CharacterAnimationController driver =
                owner.GetComponent<CharacterAnimationController>();
            if (driver == null)
            {
                driver = owner.AddComponent<CharacterAnimationController>();
            }

            driver.Configure(primaryAnimator, library);
            primaryAnimator.runtimeAnimatorController =
                library.GetController(initialStyle);

            if (weapon != null &&
                owner.GetComponent<MeleeAttackExecution>() == null)
            {
                owner.AddComponent<MeleeAttackExecution>();
            }

            if (weapon != null &&
                owner.GetComponent<WeaponVisualPresenter>() == null)
            {
                owner.AddComponent<WeaponVisualPresenter>();
            }

            EditorUtility.SetDirty(primaryAnimator);
            EditorUtility.SetDirty(driver);
            return primaryAnimator.runtimeAnimatorController != null;
        }
    }
}
