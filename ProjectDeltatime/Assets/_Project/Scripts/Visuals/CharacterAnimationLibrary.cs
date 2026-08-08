using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Visuals
{
    public enum CharacterAnimationStyle
    {
        Unarmed,
        Pistol,
        Rifle,
        Melee
    }

    [CreateAssetMenu(
        fileName = "CharacterAnimationLibrary",
        menuName = "Deltatime/Character Animation Library")]
    public sealed class CharacterAnimationLibrary : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController unarmedController;
        [SerializeField] private RuntimeAnimatorController pistolController;
        [SerializeField] private RuntimeAnimatorController rifleController;
        [SerializeField] private RuntimeAnimatorController meleeController;

        public RuntimeAnimatorController GetController(
            CharacterAnimationStyle style)
        {
            return style switch
            {
                CharacterAnimationStyle.Pistol => pistolController,
                CharacterAnimationStyle.Rifle => rifleController,
                CharacterAnimationStyle.Melee => meleeController,
                _ => unarmedController
            };
        }

        public CharacterAnimationStyle ResolveStyle(
            WeaponDefinition definition)
        {
            return definition == null
                ? CharacterAnimationStyle.Unarmed
                : definition.AnimationStyle;
        }

        public bool SupportsAttack(CharacterAnimationStyle style)
        {
            // The imported handgun pack currently contains locomotion only.
            // Avoid falling through to the base unarmed punch while a pistol
            // is equipped until a dedicated handgun firing clip is supplied.
            return style != CharacterAnimationStyle.Pistol;
        }

        public void Configure(
            RuntimeAnimatorController unarmed,
            RuntimeAnimatorController pistol,
            RuntimeAnimatorController rifle,
            RuntimeAnimatorController melee)
        {
            unarmedController = unarmed;
            pistolController = pistol;
            rifleController = rifle;
            meleeController = melee;
        }
    }
}
