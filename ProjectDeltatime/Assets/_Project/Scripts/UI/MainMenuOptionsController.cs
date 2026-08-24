using System;
using System.Collections.Generic;
using Deltatime.InputSystem;
using Deltatime.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Deltatime.UI
{
    public sealed class MainMenuOptionsController : MonoBehaviour
    {
        [Serializable]
        public sealed class RebindEntry
        {
            public string ActionName;
            public string BindingName;
            public bool AllowMouseButton;
            public Button Button;
            public TextMeshProUGUI ValueLabel;
        }

        [SerializeField] private MainMenuController owner;
        [SerializeField] private GameObject graphicsPage;
        [SerializeField] private GameObject keysPage;
        [SerializeField] private GameObject audioPage;
        [SerializeField] private TextMeshProUGUI resolutionValue;
        [SerializeField] private TextMeshProUGUI fullscreenValue;
        [SerializeField] private TextMeshProUGUI qualityValue;
        [SerializeField] private TextMeshProUGUI vSyncValue;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI masterValue;
        [SerializeField] private TextMeshProUGUI bgmValue;
        [SerializeField] private TextMeshProUGUI sfxValue;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private RebindEntry[] rebindEntries;
        [SerializeField] private Button graphicsTab;

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private GameSettingsSnapshot draft;
        private PlayerControls draftControls;
        private InputActionRebindingExtensions.RebindingOperation rebindOperation;
        private int resolutionIndex;
        private int activeRebindIndex = -1;
        private string previousOverridePath;

        public bool IsRebinding => rebindOperation != null;

        public void Configure(
            MainMenuController targetOwner,
            GameObject targetGraphicsPage,
            GameObject targetKeysPage,
            GameObject targetAudioPage,
            TextMeshProUGUI targetResolutionValue,
            TextMeshProUGUI targetFullscreenValue,
            TextMeshProUGUI targetQualityValue,
            TextMeshProUGUI targetVSyncValue,
            Slider targetMasterSlider,
            Slider targetBgmSlider,
            Slider targetSfxSlider,
            TextMeshProUGUI targetMasterValue,
            TextMeshProUGUI targetBgmValue,
            TextMeshProUGUI targetSfxValue,
            TextMeshProUGUI targetStatusLabel,
            RebindEntry[] targetRebindEntries,
            Button targetGraphicsTab)
        {
            owner = targetOwner;
            graphicsPage = targetGraphicsPage;
            keysPage = targetKeysPage;
            audioPage = targetAudioPage;
            resolutionValue = targetResolutionValue;
            fullscreenValue = targetFullscreenValue;
            qualityValue = targetQualityValue;
            vSyncValue = targetVSyncValue;
            masterSlider = targetMasterSlider;
            bgmSlider = targetBgmSlider;
            sfxSlider = targetSfxSlider;
            masterValue = targetMasterValue;
            bgmValue = targetBgmValue;
            sfxValue = targetSfxValue;
            statusLabel = targetStatusLabel;
            rebindEntries = targetRebindEntries;
            graphicsTab = targetGraphicsTab;
        }

        public void Open()
        {
            CancelRebind(false);
            draft = GameSettingsService.CreateDraft();
            RecreateDraftControls(draft.BindingOverridesJson);
            BuildResolutionList();
            PopulateUi();
            ShowGraphics();
            SetStatus(string.Empty);
        }

        private void Update()
        {
            if (draft == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            RefreshAudioLabels();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (IsRebinding)
            {
                rebindOperation.Cancel();
            }
            else
            {
                Cancel();
            }
        }

        private void OnDisable()
        {
            CancelRebind(false);
            draftControls?.Dispose();
            draftControls = null;
            draft = null;
        }

        public void ShowGraphics() => ShowPage(graphicsPage);
        public void ShowKeys() => ShowPage(keysPage);
        public void ShowAudio() => ShowPage(audioPage);

        public void PreviousResolution()
        {
            if (resolutions.Count == 0) return;
            resolutionIndex = (resolutionIndex - 1 + resolutions.Count) % resolutions.Count;
            ApplySelectedResolution();
        }

        public void NextResolution()
        {
            if (resolutions.Count == 0) return;
            resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
            ApplySelectedResolution();
        }

        public void ToggleFullscreen()
        {
            draft.Fullscreen = !draft.Fullscreen;
            RefreshGraphicsLabels();
        }

        public void PreviousQuality()
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            draft.QualityLevel = (draft.QualityLevel - 1 + count) % count;
            RefreshGraphicsLabels();
        }

        public void NextQuality()
        {
            int count = Mathf.Max(1, QualitySettings.names.Length);
            draft.QualityLevel = (draft.QualityLevel + 1) % count;
            RefreshGraphicsLabels();
        }

        public void ToggleVSync()
        {
            draft.VSyncCount = draft.VSyncCount > 0 ? 0 : 1;
            RefreshGraphicsLabels();
        }

        public void BeginRebind(int entryIndex)
        {
            if (IsRebinding || rebindEntries == null || entryIndex < 0 ||
                entryIndex >= rebindEntries.Length || draftControls == null)
            {
                return;
            }

            RebindEntry entry = rebindEntries[entryIndex];
            InputAction action = draftControls.asset.FindAction(entry.ActionName, true);
            int bindingIndex = InputBindingDisplay.FindBindingIndex(action, entry.BindingName);
            if (bindingIndex < 0)
            {
                SetStatus($"Binding not found: {entry.ActionName} {entry.BindingName}");
                return;
            }

            activeRebindIndex = entryIndex;
            previousOverridePath = action.bindings[bindingIndex].overridePath;
            entry.ValueLabel.text = "PRESS A KEY";
            SetStatus("ESC TO CANCEL");

            rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .OnCancel(operation => FinishRebind(action, bindingIndex, false))
                .OnComplete(operation => FinishRebind(action, bindingIndex, true));

            if (!entry.AllowMouseButton)
            {
                rebindOperation.WithControlsExcluding("<Mouse>");
            }

            rebindOperation.Start();
        }

        public void Apply()
        {
            if (draft == null || IsRebinding)
            {
                return;
            }

            ReadAudioValues();
            draft.BindingOverridesJson = draftControls.asset.SaveBindingOverridesAsJson();
            GameSettingsService.Apply(draft);
            InputBindingDisplay.Invalidate();
            owner.CloseOptions(true);
        }

        public void Cancel()
        {
            if (IsRebinding)
            {
                rebindOperation.Cancel();
                return;
            }

            owner.CloseOptions();
        }

        public void ResetDefaults()
        {
            if (IsRebinding)
            {
                rebindOperation.Cancel();
            }

            draft = GameSettingsService.CreateDefaults();
            RecreateDraftControls(string.Empty);
            BuildResolutionList();
            PopulateUi();
            SetStatus("DEFAULTS READY - APPLY TO SAVE");
        }

        public void RefreshAudioLabels()
        {
            if (masterSlider == null) return;
            if (masterValue != null) masterValue.text = $"{Mathf.RoundToInt(masterSlider.value * 100f)}%";
            if (bgmValue != null) bgmValue.text = $"{Mathf.RoundToInt(bgmSlider.value * 100f)}%";
            if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
        }

        private void FinishRebind(InputAction action, int bindingIndex, bool completed)
        {
            RebindEntry entry = rebindEntries[activeRebindIndex];
            string path = action.bindings[bindingIndex].effectivePath;
            bool valid = completed && IsAllowedPath(entry.AllowMouseButton, path) &&
                !HasDuplicatePath(draftControls.asset, action, bindingIndex, path);
            string message;
            if (valid)
            {
                message = "BINDING UPDATED - APPLY TO SAVE";
            }
            else
            {
                RestoreOverride(action, bindingIndex, previousOverridePath);
                message = completed
                    ? "KEY NOT ALLOWED OR ALREADY IN USE"
                    : "REBIND CANCELED";
            }

            rebindOperation.Dispose();
            rebindOperation = null;
            activeRebindIndex = -1;
            previousOverridePath = null;
            RefreshKeyLabels();
            SetStatus(message);
        }

        private void CancelRebind(bool showMessage)
        {
            if (rebindOperation == null)
            {
                return;
            }

            rebindOperation.Cancel();
            if (!showMessage)
            {
                SetStatus(string.Empty);
            }
        }

        public static bool HasDuplicatePath(
            InputActionAsset asset,
            InputAction selectedAction,
            int selectedIndex,
            string path)
        {
            if (asset == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (InputActionMap actionMap in asset.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    if (action.name == "Point")
                    {
                        continue;
                    }

                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        InputBinding binding = action.bindings[i];
                        if (binding.isComposite || (action == selectedAction && i == selectedIndex))
                        {
                            continue;
                        }

                        if (string.Equals(binding.effectivePath, path, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsAllowedPath(bool allowMouseButton, string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                path.Equals("<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!allowMouseButton ||
                !path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return path.EndsWith("Button", StringComparison.OrdinalIgnoreCase);
        }

        private static void RestoreOverride(InputAction action, int index, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                action.RemoveBindingOverride(index);
            }
            else
            {
                action.ApplyBindingOverride(index, path);
            }
        }

        private void RecreateDraftControls(string json)
        {
            draftControls?.Dispose();
            draftControls = new PlayerControls();
            GameSettingsService.TryApplyBindingOverrides(draftControls.asset, json);
        }

        private void BuildResolutionList()
        {
            resolutions.Clear();
            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
            {
                AddResolution(available[i].width, available[i].height);
            }

            AddResolution(draft.Width, draft.Height);
            resolutions.Sort((left, right) =>
            {
                int area = (left.x * left.y).CompareTo(right.x * right.y);
                return area != 0 ? area : left.x.CompareTo(right.x);
            });
            resolutionIndex = Mathf.Max(0, resolutions.FindIndex(
                value => value.x == draft.Width && value.y == draft.Height));
        }

        private void AddResolution(int width, int height)
        {
            if (width <= 0 || height <= 0 || resolutions.Contains(new Vector2Int(width, height)))
            {
                return;
            }

            resolutions.Add(new Vector2Int(width, height));
        }

        private void ApplySelectedResolution()
        {
            Vector2Int selected = resolutions[resolutionIndex];
            draft.Width = selected.x;
            draft.Height = selected.y;
            RefreshGraphicsLabels();
        }

        private void PopulateUi()
        {
            masterSlider.value = draft.MasterVolume;
            bgmSlider.value = draft.BgmVolume;
            sfxSlider.value = draft.SfxVolume;
            RefreshGraphicsLabels();
            RefreshAudioLabels();
            RefreshKeyLabels();
        }

        private void RefreshGraphicsLabels()
        {
            resolutionValue.text = $"{draft.Width} x {draft.Height}";
            fullscreenValue.text = draft.Fullscreen ? "FULLSCREEN WINDOW" : "WINDOWED";
            qualityValue.text = QualitySettings.names.Length == 0
                ? "DEFAULT"
                : QualitySettings.names[Mathf.Clamp(draft.QualityLevel, 0, QualitySettings.names.Length - 1)].ToUpperInvariant();
            vSyncValue.text = draft.VSyncCount > 0 ? "ON" : "OFF";
        }

        private void RefreshKeyLabels()
        {
            if (rebindEntries == null || draftControls == null) return;
            for (int i = 0; i < rebindEntries.Length; i++)
            {
                RebindEntry entry = rebindEntries[i];
                InputAction action = draftControls.asset.FindAction(entry.ActionName, true);
                int bindingIndex = InputBindingDisplay.FindBindingIndex(action, entry.BindingName);
                entry.ValueLabel.text = bindingIndex < 0
                    ? "?"
                    : InputBindingDisplay.ToCompactName(action.bindings[bindingIndex].effectivePath);
            }
        }

        private void ReadAudioValues()
        {
            draft.MasterVolume = masterSlider.value;
            draft.BgmVolume = bgmSlider.value;
            draft.SfxVolume = sfxSlider.value;
        }

        private void ShowPage(GameObject page)
        {
            graphicsPage.SetActive(page == graphicsPage);
            keysPage.SetActive(page == keysPage);
            audioPage.SetActive(page == audioPage);
            Select(page == graphicsPage ? graphicsTab : FindFirstButton(page));
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message ?? string.Empty;
        }

        private static Button FindFirstButton(GameObject root)
        {
            return root == null ? null : root.GetComponentInChildren<Button>(true);
        }

        private static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }
    }
}
