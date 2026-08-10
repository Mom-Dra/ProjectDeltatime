using System;
using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Audio
{
    [CreateAssetMenu(fileName = "DeltatimeSoundLibrary", menuName = "Deltatime/Audio/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public struct WeaponFireSet
        {
            [SerializeField] private WeaponDefinition weapon;
            [SerializeField] private AudioClip[] clips;

            public WeaponFireSet(WeaponDefinition weaponDefinition, AudioClip[] fireClips)
            {
                weapon = weaponDefinition;
                clips = fireClips;
            }

            public WeaponDefinition Weapon => weapon;
            public AudioClip[] Clips => clips;
        }

        [Header("BGM")]
        [SerializeField] private AudioClip mainMenuBgm;
        [SerializeField] private AudioClip tutorialBgm;
        [SerializeField] private AudioClip stageBgm;
        [SerializeField] private AudioClip endingBgm;

        [Header("Weapons")]
        [SerializeField] private WeaponFireSet[] weaponFireSets;
        [SerializeField] private AudioClip[] punchHitClips;
        [SerializeField] private AudioClip[] batHitClips;
        [SerializeField] private AudioClip[] batSwingClips;
        [SerializeField] private AudioClip weaponThrowClip;

        [Header("UI")]
        [SerializeField] private AudioClip uiClickClip;

        [Header("DEADLINE")]
        [SerializeField] private AudioClip deadlineEnterImpactClip;
        [SerializeField] private AudioClip deadlineTimeWarpClip;
        [SerializeField] private AudioClip[] deadlineReleaseClips;

        public AudioClip MainMenuBgm => mainMenuBgm;
        public AudioClip TutorialBgm => tutorialBgm;
        public AudioClip StageBgm => stageBgm;
        public AudioClip EndingBgm => endingBgm;
        public AudioClip WeaponThrowClip => weaponThrowClip;
        public AudioClip UiClickClip => uiClickClip;
        public AudioClip DeadlineEnterImpactClip => deadlineEnterImpactClip;
        public AudioClip DeadlineTimeWarpClip => deadlineTimeWarpClip;

        public AudioClip GetWeaponFireClip(WeaponDefinition definition)
        {
            if (definition == null || weaponFireSets == null)
            {
                return null;
            }

            for (int i = 0; i < weaponFireSets.Length; i++)
            {
                if (weaponFireSets[i].Weapon == definition)
                {
                    return GetRandomClip(weaponFireSets[i].Clips);
                }
            }

            return null;
        }

        public AudioClip GetMeleeImpactClip(MeleeImpactKind impactKind)
        {
            return GetRandomClip(impactKind == MeleeImpactKind.Bat ? batHitClips : punchHitClips);
        }

        public AudioClip GetBatSwingClip()
        {
            return GetRandomClip(batSwingClips);
        }

        public AudioClip GetDeadlineReleaseClip()
        {
            return GetRandomClip(deadlineReleaseClips);
        }

        public void Configure(
            AudioClip menu,
            AudioClip tutorial,
            AudioClip stage,
            AudioClip ending,
            WeaponFireSet[] fireSets,
            AudioClip[] punchHits,
            AudioClip[] batHits,
            AudioClip[] batSwings,
            AudioClip throwClip,
            AudioClip uiClick,
            AudioClip deadlineImpact,
            AudioClip deadlineTimeWarp,
            AudioClip[] deadlineReleases)
        {
            mainMenuBgm = menu;
            tutorialBgm = tutorial;
            stageBgm = stage;
            endingBgm = ending;
            weaponFireSets = fireSets;
            punchHitClips = punchHits;
            batHitClips = batHits;
            batSwingClips = batSwings;
            weaponThrowClip = throwClip;
            uiClickClip = uiClick;
            deadlineEnterImpactClip = deadlineImpact;
            deadlineTimeWarpClip = deadlineTimeWarp;
            deadlineReleaseClips = deadlineReleases;
        }

        public bool IsConfigured(out string error)
        {
            if (mainMenuBgm == null || tutorialBgm == null || stageBgm == null || endingBgm == null)
            {
                error = "One or more BGM clips are missing.";
                return false;
            }

            if (weaponFireSets == null || weaponFireSets.Length < 3)
            {
                error = "Weapon fire sets are incomplete.";
                return false;
            }

            for (int i = 0; i < weaponFireSets.Length; i++)
            {
                if (weaponFireSets[i].Weapon == null || !HasClips(weaponFireSets[i].Clips))
                {
                    error = $"Weapon fire set {i} is incomplete.";
                    return false;
                }
            }

            if (!HasClips(punchHitClips) || !HasClips(batHitClips) ||
                !HasClips(batSwingClips) ||
                weaponThrowClip == null ||
                uiClickClip == null ||
                deadlineEnterImpactClip == null || deadlineTimeWarpClip == null ||
                !HasClips(deadlineReleaseClips))
            {
                error = "One or more combat or DEADLINE clips are missing.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (!HasClips(clips))
            {
                return null;
            }

            int startIndex = UnityEngine.Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(startIndex + i) % clips.Length];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static bool HasClips(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
