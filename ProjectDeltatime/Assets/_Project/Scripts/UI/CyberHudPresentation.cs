using Deltatime.Combat;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.Player;
using UnityEngine;

namespace Deltatime.UI
{
    internal static class CyberHudPalette
    {
        public static readonly Color Panel =
            new Color(0.004f, 0.012f, 0.019f, 0.84f);
        public static readonly Color PanelInner =
            new Color(0.018f, 0.035f, 0.045f, 0.66f);
        public static readonly Color Accent =
            new Color(0.12f, 0.82f, 0.86f, 1f);
        public static readonly Color AccentDim =
            new Color(0.12f, 0.42f, 0.46f, 0.72f);
        public static readonly Color Frame =
            new Color(0.39f, 0.53f, 0.56f, 0.72f);
        public static readonly Color Text =
            new Color(0.82f, 0.87f, 0.89f, 1f);
        public static readonly Color Icon =
            new Color(0.66f, 0.72f, 0.74f, 0.86f);
        public static readonly Color Muted =
            new Color(0.24f, 0.34f, 0.36f, 0.38f);
        public static readonly Color Amber =
            new Color(1f, 0.64f, 0.08f, 1f);
    }

    internal readonly struct HudControlHint
    {
        public HudControlHint(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }

    internal readonly struct CyberHudLayout
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float MinimumScale = 0.65f;
        private const float MaximumScale = 1.5f;

        public CyberHudLayout(
            Rect safeArea,
            float scale,
            Rect stageLabel,
            Rect vitalsPanel,
            Rect weaponPanel,
            Rect timePanel,
            Rect controlsPanel,
            Rect tutorialPanel,
            Rect topMessagePanel,
            Rect centerMessagePanel)
        {
            SafeArea = safeArea;
            Scale = scale;
            StageLabel = stageLabel;
            VitalsPanel = vitalsPanel;
            WeaponPanel = weaponPanel;
            TimePanel = timePanel;
            ControlsPanel = controlsPanel;
            TutorialPanel = tutorialPanel;
            TopMessagePanel = topMessagePanel;
            CenterMessagePanel = centerMessagePanel;
        }

        public Rect SafeArea { get; }
        public float Scale { get; }
        public Rect StageLabel { get; }
        public Rect VitalsPanel { get; }
        public Rect WeaponPanel { get; }
        public Rect TimePanel { get; }
        public Rect ControlsPanel { get; }
        public Rect TutorialPanel { get; }
        public Rect TopMessagePanel { get; }
        public Rect CenterMessagePanel { get; }

        public static CyberHudLayout Calculate(Rect safeArea)
        {
            float scale = Mathf.Clamp(
                Mathf.Min(
                    safeArea.width / ReferenceWidth,
                    safeArea.height / ReferenceHeight),
                MinimumScale,
                MaximumScale);
            float margin = 24f * scale;
            float panelGap = 10f * scale;
            float leftPanelWidth = 310f * scale;
            float rowHeight = 84f * scale;
            float timePanelSize = 188f * scale;

            Rect weaponPanel = new Rect(
                safeArea.xMin + margin,
                safeArea.yMax - margin - rowHeight,
                leftPanelWidth,
                rowHeight);
            Rect vitalsPanel = new Rect(
                weaponPanel.x,
                weaponPanel.y - panelGap - rowHeight,
                leftPanelWidth,
                rowHeight);
            Rect timePanel = new Rect(
                safeArea.xMax - margin - timePanelSize,
                safeArea.yMax - margin - timePanelSize,
                timePanelSize,
                timePanelSize);
            Rect stageLabel = new Rect(
                safeArea.xMin + 30f * scale,
                safeArea.yMin + 20f * scale,
                300f * scale,
                58f * scale);

            float centerLeft = weaponPanel.xMax + margin;
            float centerRight = timePanel.xMin - margin;
            float availableCenterWidth = Mathf.Max(0f, centerRight - centerLeft);
            float controlsWidth = Mathf.Min(1040f * scale, availableCenterWidth);
            Rect controlsPanel = new Rect(
                centerLeft + (availableCenterWidth - controlsWidth) * 0.5f,
                safeArea.yMax - margin - 58f * scale,
                controlsWidth,
                58f * scale);

            float tutorialWidth = Mathf.Min(760f * scale, availableCenterWidth);
            float tutorialHeight = 154f * scale;
            Rect tutorialPanel = new Rect(
                centerLeft + (availableCenterWidth - tutorialWidth) * 0.5f,
                safeArea.yMax - margin - tutorialHeight,
                tutorialWidth,
                tutorialHeight);
            float topMessageWidth = Mathf.Min(
                360f * scale,
                safeArea.width - margin * 2f);
            Rect topMessagePanel = new Rect(
                safeArea.center.x - topMessageWidth * 0.5f,
                safeArea.yMin + 18f * scale,
                topMessageWidth,
                144f * scale);
            float centerMessageWidth = Mathf.Min(
                460f * scale,
                safeArea.width);
            float centerMessageHeight = Mathf.Min(
                168f * scale,
                safeArea.height);
            Rect centerMessagePanel = new Rect(
                safeArea.center.x - centerMessageWidth * 0.5f,
                safeArea.center.y - centerMessageHeight * 0.5f,
                centerMessageWidth,
                centerMessageHeight);

            return new CyberHudLayout(
                safeArea,
                scale,
                stageLabel,
                vitalsPanel,
                weaponPanel,
                timePanel,
                controlsPanel,
                tutorialPanel,
                topMessagePanel,
                centerMessagePanel);
        }

        public static Rect GetGuiSafeArea(Rect screenSafeArea, float screenHeight)
        {
            return new Rect(
                screenSafeArea.x,
                screenHeight - screenSafeArea.yMax,
                screenSafeArea.width,
                screenSafeArea.height);
        }

        public bool Contains(Rect rect)
        {
            return rect.xMin >= SafeArea.xMin - 0.01f &&
                   rect.yMin >= SafeArea.yMin - 0.01f &&
                   rect.xMax <= SafeArea.xMax + 0.01f &&
                   rect.yMax <= SafeArea.yMax + 0.01f;
        }

        public Rect GetWeaponInteractionPromptPanel(bool isTutorial)
        {
            Rect anchor = isTutorial ? TutorialPanel : ControlsPanel;
            float horizontalMargin = 12f * Scale;
            float width = Mathf.Min(
                246f * Scale,
                Mathf.Max(0f, SafeArea.width - horizontalMargin * 2f));
            float height = 46f * Scale;
            float x = Mathf.Clamp(
                anchor.center.x - width * 0.5f,
                SafeArea.xMin + horizontalMargin,
                SafeArea.xMax - horizontalMargin - width);
            float y = Mathf.Max(
                SafeArea.yMin,
                anchor.yMin - height - 10f * Scale);
            return new Rect(x, y, width, height);
        }
    }

    internal static class HudDisplayFormatter
    {
        public static string FormatStageLabel(string sceneName)
        {
            return StageSceneFlow.TryGetDisplayStageNumber(
                sceneName,
                out int stageNumber)
                ? $"STAGE {stageNumber}"
                : string.IsNullOrWhiteSpace(sceneName)
                    ? "STAGE"
                    : sceneName.ToUpperInvariant();
        }

        public static string FormatAmmunition(
            bool hasWeapon,
            bool isFirearm,
            int ammunition)
        {
            return hasWeapon && isFirearm
                ? Mathf.Max(0, ammunition).ToString()
                : "—";
        }

        public static bool IsHealthSlotFilled(
            int currentHealth,
            int maximumHealth,
            int slotIndex)
        {
            int maximum = Mathf.Max(1, maximumHealth);
            int current = Mathf.Clamp(currentHealth, 0, maximum);
            return slotIndex >= 0 &&
                   slotIndex < maximum &&
                   slotIndex < current;
        }

        public static string FormatChargeCount(int charges)
        {
            return Mathf.Max(0, charges).ToString();
        }

        public static string FormatTimeScale(float timeScale, bool isReplay)
        {
            return isReplay
                ? "REPLAY 1.00x"
                : $"SCALE {Mathf.Max(0f, timeScale):0.00}x";
        }
    }

    internal sealed class CyberHudRenderer
    {
        private static bool missingAssetErrorReported;
        private static bool missingWeaponIconErrorReported;

        private HudIconSet icons;
        private Texture2D solidTexture;
        private GUIStyle stageStyle;
        private GUIStyle valueStyle;
        private GUIStyle timeStyle;
        private GUIStyle fallbackStyle;
        private GUIStyle controlKeyStyle;
        private GUIStyle controlLabelStyle;
        private float styledScale = -1f;

        public bool HasRequiredIcons
        {
            get
            {
                EnsureResources();
                return icons != null && icons.IsConfigured;
            }
        }

        public CyberHudLayout DrawPersistentHud(
            string sceneLabel,
            PlayerHealth health,
            DeadlineController deadline,
            WeaponController weapon,
            float currentTimeScale,
            bool isReplay)
        {
            EnsureResources();
            Rect safeArea = CyberHudLayout.GetGuiSafeArea(
                Screen.safeArea,
                Screen.height);
            CyberHudLayout layout = CyberHudLayout.Calculate(safeArea);
            EnsureStyles(layout.Scale);

            DrawStageLabel(layout.StageLabel, sceneLabel, layout.Scale);
            DrawVitals(layout.VitalsPanel, health, deadline, layout.Scale);
            DrawWeapon(layout.WeaponPanel, weapon, layout.Scale);
            DrawTime(
                layout.TimePanel,
                currentTimeScale,
                isReplay,
                layout.Scale);
            return layout;
        }

        public void DrawPanel(Rect rect, float scale)
        {
            DrawPanel(rect, scale, CyberHudPalette.Accent);
        }

        public void DrawPanel(Rect rect, float scale, Color accent)
        {
            float cut = Mathf.Min(
                11f * scale,
                Mathf.Min(rect.width, rect.height) * 0.25f);
            DrawCutPanelBackground(rect, cut, CyberHudPalette.Panel);
            DrawCutFrame(
                rect,
                cut,
                Mathf.Max(1f, 1.25f * scale),
                CyberHudPalette.Frame);
            DrawLine(
                new Vector2(rect.x + cut + 7f * scale, rect.y),
                new Vector2(
                    Mathf.Min(rect.xMax - cut, rect.x + 96f * scale),
                    rect.y),
                Mathf.Max(1f, 1.5f * scale),
                accent);
            DrawSolid(
                new Rect(
                    rect.x + cut,
                    rect.y + 5f * scale,
                    Mathf.Max(0f, 2f * scale),
                    Mathf.Max(0f, 10f * scale)),
                accent);
        }

        public void DrawControlHints(
            Rect rect,
            float scale,
            HudControlHint[] hints)
        {
            if (hints == null || hints.Length == 0)
            {
                return;
            }

            DrawFooterRail(rect, scale);
            int rowCount = hints.Length > 4 ? 2 : 1;
            int itemsPerRow = Mathf.CeilToInt(hints.Length / (float)rowCount);
            float rowHeight = rect.height / rowCount;
            float itemGap = 13f * scale;
            float labelGap = 5f * scale;

            for (int row = 0; row < rowCount; row++)
            {
                int start = row * itemsPerRow;
                int end = Mathf.Min(hints.Length, start + itemsPerRow);
                if (start >= end)
                {
                    continue;
                }

                float totalWidth = 0f;
                for (int i = start; i < end; i++)
                {
                    Vector2 keySize = controlKeyStyle.CalcSize(
                        new GUIContent(hints[i].Key));
                    Vector2 labelSize = controlLabelStyle.CalcSize(
                        new GUIContent(hints[i].Label));
                    totalWidth += Mathf.Max(25f * scale, keySize.x + 10f * scale) +
                                  labelGap + labelSize.x;
                    if (i < end - 1)
                    {
                        totalWidth += itemGap;
                    }
                }

                float x = rect.center.x - totalWidth * 0.5f;
                float keyHeight = Mathf.Min(
                    23f * scale,
                    rowHeight - 4f * scale);
                float y = rect.y + row * rowHeight +
                          (rowHeight - keyHeight) * 0.5f;
                for (int i = start; i < end; i++)
                {
                    Vector2 keySize = controlKeyStyle.CalcSize(
                        new GUIContent(hints[i].Key));
                    Vector2 labelSize = controlLabelStyle.CalcSize(
                        new GUIContent(hints[i].Label));
                    float keyWidth = Mathf.Max(
                        25f * scale,
                        keySize.x + 10f * scale);
                    Rect keyRect = new Rect(x, y, keyWidth, keyHeight);
                    DrawKeyCap(keyRect, scale);
                    GUI.Label(keyRect, hints[i].Key, controlKeyStyle);

                    x += keyWidth + labelGap;
                    Rect labelRect = new Rect(
                        x,
                        y,
                        labelSize.x,
                        keyHeight);
                    GUI.Label(labelRect, hints[i].Label, controlLabelStyle);
                    x += labelSize.x + itemGap;
                }
            }
        }

        public void DrawWeaponInteractionPrompt(
            CyberHudLayout layout,
            bool isTutorial,
            PlayerWeaponInteractionPrompt prompt)
        {
            string action = GetWeaponInteractionAction(prompt);
            if (string.IsNullOrEmpty(action))
            {
                return;
            }

            float scale = layout.Scale;
            Rect panel = layout.GetWeaponInteractionPromptPanel(isTutorial);
            if (panel.width <= 0f || panel.height > layout.SafeArea.height)
            {
                return;
            }
            DrawPanel(panel, scale, CyberHudPalette.Accent);

            Rect key = new Rect(
                panel.x + 10f * scale,
                panel.y + 9f * scale,
                30f * scale,
                28f * scale);
            DrawKeyCap(key, scale);
            GUI.Label(key, InputBindingDisplay.Get("Interact"), controlKeyStyle);
            GUI.Label(
                new Rect(
                    key.xMax + 12f * scale,
                    panel.y,
                    Mathf.Max(0f, panel.xMax - key.xMax - 22f * scale),
                    panel.height),
                action,
                controlLabelStyle);
        }

        public void Dispose()
        {
            if (solidTexture != null)
            {
                Object.Destroy(solidTexture);
                solidTexture = null;
            }
        }

        private static string GetWeaponInteractionAction(
            PlayerWeaponInteractionPrompt prompt)
        {
            switch (prompt)
            {
                case PlayerWeaponInteractionPrompt.PickUp:
                    return "무기 줍기";
                case PlayerWeaponInteractionPrompt.Swap:
                    return "무기 교체";
                case PlayerWeaponInteractionPrompt.Catch:
                    return "무기 캐치";
                default:
                    return null;
            }
        }

        private void DrawStageLabel(Rect rect, string text, float scale)
        {
            DrawPanel(rect, scale);
            GUI.Label(
                new Rect(
                    rect.x + 18f * scale,
                    rect.y + 4f * scale,
                    rect.width - 30f * scale,
                    rect.height - 8f * scale),
                text,
                stageStyle);
            DrawSolid(
                new Rect(
                    rect.x + 9f * scale,
                    rect.y + 17f * scale,
                    Mathf.Max(1f, 2f * scale),
                    rect.height - 34f * scale),
                CyberHudPalette.Accent);
        }

        private void DrawVitals(
            Rect panel,
            PlayerHealth health,
            DeadlineController deadline,
            float scale)
        {
            DrawPanel(panel, scale);
            float separatorX = panel.xMax - 94f * scale;
            DrawSolid(
                new Rect(
                    separatorX,
                    panel.y + 12f * scale,
                    1f * scale,
                    panel.height - 24f * scale),
                CyberHudPalette.Frame);

            int maximumHealth = health == null
                ? 1
                : Mathf.Max(1, health.MaximumHealth);
            int currentHealth = health == null
                ? 0
                : Mathf.Clamp(health.CurrentHealth, 0, maximumHealth);
            Rect healthArea = new Rect(
                panel.x + 14f * scale,
                panel.y + 12f * scale,
                separatorX - panel.x - 26f * scale,
                panel.height - 24f * scale);
            float gap = 6f * scale;
            float iconSize = Mathf.Min(
                46f * scale,
                (healthArea.width - gap * (maximumHealth - 1)) /
                maximumHealth);
            iconSize = Mathf.Max(1f, iconSize);
            float totalWidth =
                iconSize * maximumHealth + gap * (maximumHealth - 1);
            float healthX = healthArea.x +
                            Mathf.Max(0f, (healthArea.width - totalWidth) * 0.5f);
            float healthY = healthArea.y +
                            (healthArea.height - iconSize) * 0.5f;
            for (int i = 0; i < maximumHealth; i++)
            {
                DrawIcon(
                    icons == null ? null : icons.HealthIcon,
                    new Rect(
                        healthX + i * (iconSize + gap),
                        healthY,
                        iconSize,
                        iconSize),
                    HudDisplayFormatter.IsHealthSlotFilled(
                        currentHealth,
                        maximumHealth,
                        i)
                        ? currentHealth <= 1
                            ? CyberHudPalette.Amber
                            : CyberHudPalette.Icon
                        : CyberHudPalette.Muted,
                    "+");
            }

            int charges = deadline == null
                ? 0
                : Mathf.Max(0, deadline.ChargesRemaining);
            Rect deadlineIconRect = new Rect(
                separatorX + 7f * scale,
                panel.y + 20f * scale,
                42f * scale,
                42f * scale);
            DrawIcon(
                icons == null ? null : icons.DeadlineIcon,
                deadlineIconRect,
                charges <= 0
                    ? CyberHudPalette.Amber
                    : deadline != null && deadline.IsActive
                        ? CyberHudPalette.Accent
                        : CyberHudPalette.Icon,
                "D");
            Color previousColor = GUI.color;
            if (charges <= 0)
            {
                GUI.color = CyberHudPalette.Amber;
            }
            GUI.Label(
                new Rect(
                    deadlineIconRect.xMax + 2f * scale,
                    panel.y + 16f * scale,
                    39f * scale,
                    50f * scale),
                HudDisplayFormatter.FormatChargeCount(charges),
                valueStyle);
            GUI.color = previousColor;
        }

        private void DrawWeapon(
            Rect panel,
            WeaponController weapon,
            float scale)
        {
            DrawPanel(panel, scale);
            float separatorX = panel.xMax - 82f * scale;
            DrawSolid(
                new Rect(
                    separatorX,
                    panel.y + 12f * scale,
                    1f * scale,
                    panel.height - 24f * scale),
                CyberHudPalette.Frame);

            WeaponDefinition definition = weapon == null
                ? null
                : weapon.Definition;
            if (definition != null &&
                definition.HudIcon == null &&
                !missingWeaponIconErrorReported)
            {
                missingWeaponIconErrorReported = true;
                Debug.LogError(
                    $"HUD icon is missing for {definition.name}. " +
                    "The unarmed icon fallback will be used.");
            }
            Sprite weaponIcon = definition == null
                ? icons == null ? null : icons.UnarmedIcon
                : definition.HudIcon != null
                    ? definition.HudIcon
                    : icons == null ? null : icons.UnarmedIcon;
            string fallback = definition == null
                ? "빈손"
                : definition.DisplayName;
            DrawIcon(
                weaponIcon,
                new Rect(
                    panel.x + 14f * scale,
                    panel.y + 9f * scale,
                    separatorX - panel.x - 28f * scale,
                    panel.height - 18f * scale),
                CyberHudPalette.Icon,
                fallback);

            bool hasWeapon = definition != null;
            bool isFirearm = definition != null && definition.IsFirearm;
            int ammunition = weapon == null ? 0 : weapon.Ammunition;
            Color previousColor = GUI.color;
            if (hasWeapon && isFirearm && ammunition <= 0)
            {
                GUI.color = CyberHudPalette.Amber;
            }
            GUI.Label(
                new Rect(
                    separatorX + 4f * scale,
                    panel.y + 16f * scale,
                    panel.xMax - separatorX - 8f * scale,
                    50f * scale),
                HudDisplayFormatter.FormatAmmunition(
                    hasWeapon,
                    isFirearm,
                    ammunition),
                valueStyle);
            GUI.color = previousColor;
        }

        private void DrawTime(
            Rect panel,
            float currentTimeScale,
            bool isReplay,
            float scale)
        {
            DrawPanel(panel, scale);
            GUI.Label(
                new Rect(
                    panel.x + 8f * scale,
                    panel.y + 8f * scale,
                    panel.width - 16f * scale,
                    28f * scale),
                HudDisplayFormatter.FormatTimeScale(
                    currentTimeScale,
                    isReplay),
                timeStyle);

            float dialSize = 132f * scale;
            Rect dialRect = new Rect(
                panel.center.x - dialSize * 0.5f,
                panel.yMax - dialSize - 12f * scale,
                dialSize,
                dialSize);
            DrawIcon(
                icons == null ? null : icons.ClockDialIcon,
                dialRect,
                CyberHudPalette.Icon,
                "◷");

            Vector2 pivot = dialRect.center;
            float displayedScale = isReplay
                ? 1f
                : Mathf.Clamp01(currentTimeScale);
            float angle = Mathf.Lerp(-125f, 125f, displayedScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, pivot);
            GUI.color = CyberHudPalette.Accent;
            GUI.DrawTexture(
                new Rect(
                    pivot.x - 1.5f * scale,
                    pivot.y - 45f * scale,
                    3f * scale,
                    50f * scale),
                solidTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            DrawSolid(
                new Rect(
                    pivot.x - 4f * scale,
                    pivot.y - 4f * scale,
                    8f * scale,
                    8f * scale),
                CyberHudPalette.Accent);
        }

        private void DrawCutPanelBackground(Rect rect, float cut, Color color)
        {
            DrawSolid(
                new Rect(
                    rect.x + cut,
                    rect.y,
                    Mathf.Max(0f, rect.width - cut * 2f),
                    cut),
                color);
            DrawSolid(
                new Rect(
                    rect.x,
                    rect.y + cut,
                    rect.width,
                    Mathf.Max(0f, rect.height - cut * 2f)),
                color);
            DrawSolid(
                new Rect(
                    rect.x + cut,
                    rect.yMax - cut,
                    Mathf.Max(0f, rect.width - cut * 2f),
                    cut),
                color);
        }

        private void DrawCutFrame(
            Rect rect,
            float cut,
            float thickness,
            Color color)
        {
            Vector2 topLeft = new Vector2(rect.x + cut, rect.y);
            Vector2 topRight = new Vector2(rect.xMax - cut, rect.y);
            Vector2 rightTop = new Vector2(rect.xMax, rect.y + cut);
            Vector2 rightBottom = new Vector2(rect.xMax, rect.yMax - cut);
            Vector2 bottomRight = new Vector2(rect.xMax - cut, rect.yMax);
            Vector2 bottomLeft = new Vector2(rect.x + cut, rect.yMax);
            Vector2 leftBottom = new Vector2(rect.x, rect.yMax - cut);
            Vector2 leftTop = new Vector2(rect.x, rect.y + cut);

            DrawLine(topLeft, topRight, thickness, color);
            DrawLine(topRight, rightTop, thickness, color);
            DrawLine(rightTop, rightBottom, thickness, color);
            DrawLine(rightBottom, bottomRight, thickness, color);
            DrawLine(bottomRight, bottomLeft, thickness, color);
            DrawLine(bottomLeft, leftBottom, thickness, color);
            DrawLine(leftBottom, leftTop, thickness, color);
            DrawLine(leftTop, topLeft, thickness, color);
        }

        private void DrawFooterRail(Rect rect, float scale)
        {
            float thickness = Mathf.Max(1f, scale);
            float inset = 8f * scale;
            float y = rect.y + thickness;
            DrawLine(
                new Vector2(rect.x + inset, y),
                new Vector2(rect.xMax - inset, y),
                thickness,
                CyberHudPalette.AccentDim);
            DrawLine(
                new Vector2(rect.x + inset, y),
                new Vector2(rect.x + inset, y + 7f * scale),
                thickness,
                CyberHudPalette.Frame);
            DrawLine(
                new Vector2(rect.xMax - inset, y),
                new Vector2(rect.xMax - inset, y + 7f * scale),
                thickness,
                CyberHudPalette.Frame);
            DrawSolid(
                new Rect(
                    rect.center.x - 10f * scale,
                    y - thickness * 0.5f,
                    20f * scale,
                    thickness * 2f),
                CyberHudPalette.Accent);
        }

        private void DrawKeyCap(Rect rect, float scale)
        {
            DrawSolid(rect, CyberHudPalette.PanelInner);
            float border = Mathf.Max(1f, scale);
            DrawSolid(
                new Rect(rect.x, rect.y, rect.width, border),
                CyberHudPalette.Frame);
            DrawSolid(
                new Rect(rect.x, rect.yMax - border, rect.width, border),
                CyberHudPalette.Frame);
            DrawSolid(
                new Rect(rect.x, rect.y, border, rect.height),
                CyberHudPalette.Frame);
            DrawSolid(
                new Rect(rect.xMax - border, rect.y, border, rect.height),
                CyberHudPalette.Frame);
        }

        private void DrawLine(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (solidTexture == null || length <= 0.01f || thickness <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                start);
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    start.x,
                    start.y - thickness * 0.5f,
                    length,
                    thickness),
                solidTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawIcon(
            Sprite sprite,
            Rect rect,
            Color tint,
            string fallback)
        {
            Color previousColor = GUI.color;
            GUI.color = tint;
            if (sprite != null && sprite.texture != null)
            {
                GUI.DrawTexture(
                    rect,
                    sprite.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }
            else
            {
                GUI.Label(rect, fallback, fallbackStyle);
            }

            GUI.color = previousColor;
        }

        private void DrawSolid(Rect rect, Color color)
        {
            if (solidTexture == null || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, solidTexture);
            GUI.color = previousColor;
        }

        private void EnsureResources()
        {
            if (solidTexture == null)
            {
                solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Cyber HUD Solid"
                };
                solidTexture.SetPixel(0, 0, Color.white);
                solidTexture.Apply();
            }

            if (icons == null)
            {
                icons = HudIconSet.Load();
            }

            if ((icons == null || !icons.IsConfigured) &&
                !missingAssetErrorReported)
            {
                missingAssetErrorReported = true;
                Debug.LogError(
                    "Cyber HUD icons are missing or incomplete. " +
                    "Run Tools/UI/Build Cyber HUD Assets. Text fallbacks will be used.");
            }
        }

        private void EnsureStyles(float scale)
        {
            if (stageStyle != null && Mathf.Abs(styledScale - scale) < 0.001f)
            {
                return;
            }

            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Font regularFont = fontSettings == null
                ? null
                : fontSettings.RegularFont;
            Font boldFont = fontSettings == null
                ? null
                : fontSettings.BoldFont;
            styledScale = scale;

            stageStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = Mathf.Max(15, Mathf.RoundToInt(24f * scale)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = CyberHudPalette.Text }
            };
            valueStyle = new GUIStyle(stageStyle)
            {
                fontSize = Mathf.Max(18, Mathf.RoundToInt(30f * scale)),
                alignment = TextAnchor.MiddleCenter
            };
            timeStyle = new GUIStyle(stageStyle)
            {
                font = regularFont,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(14f * scale)),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = CyberHudPalette.Accent }
            };
            fallbackStyle = new GUIStyle(stageStyle)
            {
                font = regularFont,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(20f * scale)),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = CyberHudPalette.Text }
            };
            controlKeyStyle = new GUIStyle(stageStyle)
            {
                fontSize = Mathf.Max(9, Mathf.RoundToInt(11f * scale)),
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                normal = { textColor = CyberHudPalette.Text }
            };
            controlLabelStyle = new GUIStyle(stageStyle)
            {
                font = regularFont,
                fontSize = Mathf.Max(9, Mathf.RoundToInt(12f * scale)),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = new Color(
                        CyberHudPalette.Text.r,
                        CyberHudPalette.Text.g,
                        CyberHudPalette.Text.b,
                        0.76f)
                }
            };
        }
    }
}
