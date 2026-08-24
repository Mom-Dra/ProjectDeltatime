using System.Collections;
using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Player;
using Deltatime.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.Audio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        private const string LibraryResourceName = "DeltatimeSoundLibrary";
        private const int SpatialSourceCount = 16;
        private const float BgmVolume = 0.55f;
        private const float StageBgmVolume = 0.35f;
        private const float GlobalSfxVolume = 0.9f;
        private const float DeadlineDuckMultiplier = 0.4f;
        private const float BgmCrossfadeDuration = 0.25f;
        private const float DeadlineMixDuration = 0.12f;

        private readonly List<DeadlineController> deadlineControllers = new List<DeadlineController>();
        private readonly List<AudioSource> spatialSources = new List<AudioSource>();
        private SoundLibrary library;
        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource globalSfxSource;
        private AudioSource deadlineWarpSource;
        private Coroutine bgmTransition;
        private float bgmWeightA;
        private float bgmWeightB;
        private float currentDuckMultiplier = 1f;
        private float targetDuckMultiplier = 1f;
        private float userMasterVolume = 1f;
        private float userBgmVolume = 1f;
        private float userSfxVolume = 1f;
        private int nextSpatialSource;
        private bool deadlineActive;
        private AudioClip requestedBgmClip;
        private int uiClickPlayCount;
        private int meleeSwingPlayCount;
        private int meleeImpactPlayCount;

        public static SoundManager Instance { get; private set; }
        public SoundLibrary Library => library;
        public AudioClip CurrentBgmClip => requestedBgmClip;
        public bool IsDeadlineAudioActive => deadlineActive;
        public bool IsDeadlineTimeWarpLooping =>
            deadlineWarpSource != null && deadlineWarpSource.loop;
        public int UiClickPlayCount => uiClickPlayCount;
        public int MeleeSwingPlayCount => meleeSwingPlayCount;
        public int MeleeImpactPlayCount => meleeImpactPlayCount;
        public float UserMasterVolume => userMasterVolume;
        public float UserBgmVolume => userBgmVolume;
        public float UserSfxVolume => userSfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
            {
                new GameObject("SoundManager").AddComponent<SoundManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameSettingsSnapshot settings = GameSettingsService.Current;
            userMasterVolume = settings.MasterVolume;
            userBgmVolume = settings.BgmVolume;
            userSfxVolume = settings.SfxVolume;
            library = Resources.Load<SoundLibrary>(LibraryResourceName);
            CreateAudioSources();
            ApplyUserVolumes(
                userMasterVolume,
                userBgmVolume,
                userSfxVolume);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            if (library == null)
            {
                Debug.LogError($"Sound library Resources/{LibraryResourceName} is missing.", this);
            }
        }

        private void Update()
        {
            float mixStep = Time.unscaledDeltaTime / Mathf.Max(0.01f, DeadlineMixDuration);
            currentDuckMultiplier = Mathf.MoveTowards(
                currentDuckMultiplier,
                targetDuckMultiplier,
                mixStep);
            ApplyBgmVolumes();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnbindDeadlineControllers();
            Instance = null;
        }

        public void PlayWeaponFire(WeaponDefinition definition, Vector3 position)
        {
            PlaySpatial(
                library == null ? null : library.GetWeaponFireClip(definition),
                position,
                1f,
                0.96f,
                1.04f,
                34f);
        }

        public void PlayMeleeImpact(MeleeImpactKind impactKind, Vector3 position)
        {
            AudioClip clip = library == null
                ? null
                : library.GetMeleeImpactClip(impactKind);
            if (clip == null)
            {
                return;
            }

            meleeImpactPlayCount++;
            PlaySpatial(
                clip,
                position,
                impactKind == MeleeImpactKind.Bat ? 1f : 0.88f,
                0.96f,
                1.04f,
                22f);
        }

        public void PlayMeleeSwing(Vector3 position)
        {
            AudioClip clip = library == null ? null : library.GetBatSwingClip();
            if (clip == null)
            {
                return;
            }

            meleeSwingPlayCount++;
            PlaySpatial(
                clip,
                position,
                0.72f,
                0.94f,
                1.06f,
                18f);
        }

        public void PlayWeaponThrow(Vector3 position)
        {
            PlaySpatial(
                library == null ? null : library.WeaponThrowClip,
                position,
                0.82f,
                0.97f,
                1.03f,
                20f);
        }

        public void PlayUiClick()
        {
            if (library == null || library.UiClickClip == null)
            {
                return;
            }

            uiClickPlayCount++;
            PlayGlobal(library.UiClickClip, 0.72f);
        }

        public void ApplyUserVolumes(float master, float bgm, float sfx)
        {
            userMasterVolume = Mathf.Clamp01(master);
            userBgmVolume = Mathf.Clamp01(bgm);
            userSfxVolume = Mathf.Clamp01(sfx);
            ApplyBgmVolumes();

            if (globalSfxSource != null)
            {
                globalSfxSource.volume = userMasterVolume * userSfxVolume;
            }

            if (deadlineWarpSource != null)
            {
                deadlineWarpSource.volume =
                    0.68f * userMasterVolume * userSfxVolume;
            }
        }

        public void PlayDeadlineEnter()
        {
            if (deadlineActive || library == null)
            {
                return;
            }

            deadlineActive = true;
            targetDuckMultiplier = DeadlineDuckMultiplier;
            PlayGlobal(library.DeadlineEnterImpactClip, 1f);

            AudioClip warpClip = library.DeadlineTimeWarpClip;
            if (warpClip != null)
            {
                deadlineWarpSource.clip = warpClip;
                deadlineWarpSource.loop = false;
                deadlineWarpSource.volume =
                    0.68f * userMasterVolume * userSfxVolume;
                deadlineWarpSource.Play();
            }
        }

        public void PlayDeadlineRelease()
        {
            if (!deadlineActive)
            {
                return;
            }

            deadlineActive = false;
            targetDuckMultiplier = 1f;
            deadlineWarpSource.Stop();
            deadlineWarpSource.clip = null;
            PlayGlobal(library == null ? null : library.GetDeadlineReleaseClip(), 0.95f);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            deadlineActive = false;
            targetDuckMultiplier = 1f;
            currentDuckMultiplier = 1f;
            if (deadlineWarpSource != null)
            {
                deadlineWarpSource.Stop();
            }

            BindDeadlineControllers();
            PlayBgmForScene(scene.name);
        }

        private void PlayBgmForScene(string sceneName)
        {
            if (library == null)
            {
                return;
            }

            AudioClip clip = null;
            bool loop = true;
            if (sceneName == "MainScene")
            {
                clip = library.MainMenuBgm;
            }
            else if (sceneName == "Tutorial")
            {
                clip = library.TutorialBgm;
            }
            else if (sceneName.StartsWith("Stage"))
            {
                clip = library.StageBgm;
            }
            else if (sceneName == "EndingScene" || sceneName == "Ending" || sceneName == "Credits")
            {
                clip = library.EndingBgm;
                loop = false;
            }

            TransitionToBgm(clip, loop);
        }

        private void TransitionToBgm(AudioClip clip, bool loop)
        {
            requestedBgmClip = clip;
            AudioSource current = GetDominantBgmSource();
            if (clip != null && current != null && current.isPlaying && current.clip == clip)
            {
                current.loop = loop;
                return;
            }

            if (bgmTransition != null)
            {
                StopCoroutine(bgmTransition);
            }

            bgmTransition = StartCoroutine(CrossfadeBgm(current, clip, loop));
        }

        private IEnumerator CrossfadeBgm(AudioSource current, AudioClip nextClip, bool loop)
        {
            AudioSource next = current == bgmSourceA ? bgmSourceB : bgmSourceA;
            float startWeightA = bgmWeightA;
            float startWeightB = bgmWeightB;
            float targetWeightA = next == bgmSourceA && nextClip != null ? 1f : 0f;
            float targetWeightB = next == bgmSourceB && nextClip != null ? 1f : 0f;

            if (nextClip != null)
            {
                next.clip = nextClip;
                next.loop = loop;
                next.Play();
            }

            float elapsed = 0f;
            while (elapsed < BgmCrossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / BgmCrossfadeDuration);
                bgmWeightA = Mathf.Lerp(startWeightA, targetWeightA, progress);
                bgmWeightB = Mathf.Lerp(startWeightB, targetWeightB, progress);
                yield return null;
            }

            bgmWeightA = targetWeightA;
            bgmWeightB = targetWeightB;
            if (current != null && current != next)
            {
                current.Stop();
                current.clip = null;
            }

            ApplyBgmVolumes();
            bgmTransition = null;
        }

        private AudioSource GetDominantBgmSource()
        {
            if (bgmSourceA != null && bgmSourceA.isPlaying && bgmWeightA >= bgmWeightB)
            {
                return bgmSourceA;
            }

            if (bgmSourceB != null && bgmSourceB.isPlaying)
            {
                return bgmSourceB;
            }

            return bgmSourceA;
        }

        private void ApplyBgmVolumes()
        {
            if (bgmSourceA != null)
            {
                bgmSourceA.volume = GetBgmVolume(bgmSourceA.clip) * bgmWeightA *
                    currentDuckMultiplier * userMasterVolume * userBgmVolume;
            }

            if (bgmSourceB != null)
            {
                bgmSourceB.volume = GetBgmVolume(bgmSourceB.clip) * bgmWeightB *
                    currentDuckMultiplier * userMasterVolume * userBgmVolume;
            }
        }

        private float GetBgmVolume(AudioClip clip)
        {
            return library != null && clip == library.StageBgm ? StageBgmVolume : BgmVolume;
        }

        private void BindDeadlineControllers()
        {
            UnbindDeadlineControllers();
            DeadlineController[] controllers = FindObjectsByType<DeadlineController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                DeadlineController controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                controller.Activated += HandleDeadlineActivated;
                controller.Released += HandleDeadlineReleased;
                deadlineControllers.Add(controller);
            }
        }

        private void UnbindDeadlineControllers()
        {
            for (int i = 0; i < deadlineControllers.Count; i++)
            {
                DeadlineController controller = deadlineControllers[i];
                if (controller != null)
                {
                    controller.Activated -= HandleDeadlineActivated;
                    controller.Released -= HandleDeadlineReleased;
                }
            }

            deadlineControllers.Clear();
        }

        private void HandleDeadlineActivated()
        {
            PlayDeadlineEnter();
        }

        private void HandleDeadlineReleased()
        {
            PlayDeadlineRelease();
        }

        private void PlayGlobal(AudioClip clip, float volumeScale)
        {
            if (clip != null && globalSfxSource != null)
            {
                globalSfxSource.PlayOneShot(
                    clip,
                    GlobalSfxVolume * Mathf.Clamp01(volumeScale));
            }
        }

        private void PlaySpatial(
            AudioClip clip,
            Vector3 position,
            float volumeScale,
            float minimumPitch,
            float maximumPitch,
            float maximumDistance)
        {
            if (clip == null || spatialSources.Count == 0)
            {
                return;
            }

            AudioSource source = null;
            for (int i = 0; i < spatialSources.Count; i++)
            {
                int index = (nextSpatialSource + i) % spatialSources.Count;
                if (!spatialSources[index].isPlaying)
                {
                    source = spatialSources[index];
                    nextSpatialSource = (index + 1) % spatialSources.Count;
                    break;
                }
            }

            if (source == null)
            {
                source = spatialSources[nextSpatialSource];
                nextSpatialSource = (nextSpatialSource + 1) % spatialSources.Count;
            }

            source.transform.position = position;
            source.clip = clip;
            source.volume = GlobalSfxVolume * Mathf.Clamp01(volumeScale) *
                userMasterVolume * userSfxVolume;
            source.pitch = Random.Range(minimumPitch, maximumPitch);
            source.maxDistance = maximumDistance;
            source.Play();
        }

        private void CreateAudioSources()
        {
            bgmSourceA = CreateSource("BGM A", 0f);
            bgmSourceB = CreateSource("BGM B", 0f);
            bgmSourceA.ignoreListenerPause = true;
            bgmSourceB.ignoreListenerPause = true;
            globalSfxSource = CreateSource("Global SFX", 0f);
            globalSfxSource.ignoreListenerPause = true;
            deadlineWarpSource = CreateSource("DEADLINE Time Warp", 0f);
            deadlineWarpSource.ignoreListenerPause = true;

            for (int i = 0; i < SpatialSourceCount; i++)
            {
                AudioSource source = CreateSource($"Spatial SFX {i + 1}", 1f);
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 2f;
                source.maxDistance = 28f;
                spatialSources.Add(source);
            }
        }

        private AudioSource CreateSource(string sourceName, float spatialBlend)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = spatialBlend > 0f ? 0.15f : 0f;
            return source;
        }
    }
}
