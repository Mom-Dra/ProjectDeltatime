using System;
using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Level;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.UI
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private StageController stage;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerDash playerDash;
        [SerializeField] private DeadlineController deadline;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private StageReplayController replay;

        [Header("Replay HUD")]
        [SerializeField] private Color replayBlue =
            new Color(0.08f, 0.82f, 0.9f, 1f);
        [SerializeField] private Color replayFrameColor =
            new Color(0.32f, 0.68f, 0.72f, 0.78f);
        [SerializeField] private Color replayClearColor =
            new Color(1f, 0.72f, 0.16f, 1f);
        [SerializeField] private Color replayDeadColor =
            new Color(1f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color replayPanelColor =
            new Color(0.015f, 0.035f, 0.065f, 0.9f);

        private CyberHudRenderer hudRenderer;
        private PlayerCombat playerCombat;
        private GUIStyle messageStyle;
        private GUIStyle clearMessageStyle;
        private GUIStyle overlayMessageStyle;
        private GUIStyle overlayClearMessageStyle;
        private GUIStyle deadlineTitleStyle;
        private GUIStyle deadlineBodyStyle;
        private GUIStyle replayTitleStyle;
        private GUIStyle replayMetaStyle;
        private GUIStyle replayTimeStyle;
        private GUIStyle replayEventStyle;
        private GUIStyle replayKeyStyle;
        private GUIStyle replayKeyLabelStyle;
        private GUIStyle replayOutcomeStyle;
        private Texture2D whiteTexture;
        private Texture2D circleTexture;
        private string replayStageLabel;
        private float styledScale = -1f;

        public bool HasRequiredVisualAssets =>
            GetRenderer().HasRequiredIcons &&
            (weapon == null ||
             weapon.Definition == null ||
             weapon.Definition.HudIcon != null);

        private void Awake()
        {
            replayStageLabel = ResolveStageLabel(
                SceneManager.GetActiveScene().name);
            if (stage == null ||
                worldTime == null ||
                playerHealth == null ||
                playerDash == null ||
                deadline == null ||
                weapon == null ||
                replay == null)
            {
                Debug.LogError(
                    $"{nameof(GameHud)} is missing required references.",
                    this);
                enabled = false;
            }

            ResolvePlayerCombat();
        }

        private void OnGUI()
        {
            if (replay.IsReplaying)
            {
                EnsureReplayStyles();
                DrawReplayHud();
                return;
            }

            CyberHudLayout layout = GetRenderer().DrawPersistentHud(
                HudDisplayFormatter.FormatStageLabel(
                    SceneManager.GetActiveScene().name),
                playerHealth,
                deadline,
                weapon,
                worldTime.CurrentTimeScale,
                false);
            EnsureStyles(layout.Scale);

            string message = GetStageMessage();
            if (!string.IsNullOrEmpty(message))
            {
                Rect messagePanel = replay.IsReplaying
                    ? layout.TopMessagePanel
                    : layout.CenterMessagePanel;
                bool clearPresentation =
                    stage.CurrentState == StageController.StageState.Cleared ||
                    stage.CurrentState == StageController.StageState.Replaying;
                GetRenderer().DrawPanel(
                    messagePanel,
                    layout.Scale,
                    clearPresentation
                        ? CyberHudPalette.Amber
                        : CyberHudPalette.AccentDim);
                GUI.Label(
                    new Rect(
                        messagePanel.x + 20f * layout.Scale,
                        messagePanel.y + 16f * layout.Scale,
                        messagePanel.width - 40f * layout.Scale,
                        messagePanel.height - 32f * layout.Scale),
                    message,
                    clearPresentation
                        ? replay.IsReplaying
                            ? overlayClearMessageStyle
                            : clearMessageStyle
                        : replay.IsReplaying
                            ? overlayMessageStyle
                            : messageStyle);
            }

            DrawDeadlineFeedback(layout);
            if (stage.CurrentState == StageController.StageState.Active)
            {
                ResolvePlayerCombat();
                GetRenderer().DrawWeaponInteractionPrompt(
                    layout,
                    false,
                    playerCombat == null
                        ? PlayerWeaponInteractionPrompt.None
                        : playerCombat.WeaponInteractionPrompt);
            }
            GetRenderer().DrawControlHints(
                layout.ControlsPanel,
                layout.Scale,
                GetControlHints());
        }

        private void DrawReplayHud()
        {
            DrawReplayFrame();
            DrawReplayHeader();
            DrawReplayTimeline();
        }

        private void DrawReplayFrame()
        {
            float inset = GetReplayFrameInset();
            float corner = Mathf.Clamp(Screen.width * 0.055f, 54f, 92f);
            const float thickness = 2f;
            DrawCornerBracket(
                new Vector2(inset, inset),
                corner,
                thickness,
                1f,
                1f);
            DrawCornerBracket(
                new Vector2(Screen.width - inset, inset),
                corner,
                thickness,
                -1f,
                1f);
            DrawCornerBracket(
                new Vector2(inset, Screen.height - inset),
                corner,
                thickness,
                1f,
                -1f);
            DrawCornerBracket(
                new Vector2(Screen.width - inset, Screen.height - inset),
                corner,
                thickness,
                -1f,
                -1f);
        }

        private void DrawReplayHeader()
        {
            float inset = GetReplayFrameInset();
            float x = inset + 20f;
            float y = inset + 10f;
            Rect replayCard = new Rect(x, y, 216f, 80f);
            DrawReplayCardOutline(
                replayCard,
                16f,
                WithAlpha(replayFrameColor, 0.75f));
            GUI.Label(
                new Rect(x + 14f, y + 6f, 188f, 38f),
                "REPLAY",
                replayTitleStyle);
            GUI.Label(
                new Rect(x + 14f, y + 43f, 188f, 24f),
                replayStageLabel,
                replayMetaStyle);

            bool isDead = stage.CurrentState ==
                          StageController.StageState.PlayerDead;
            Color outcomeColor = isDead ? replayDeadColor : replayBlue;
            DrawSolidRect(
                new Rect(x + 3f, replayCard.yMax + 18f, 2f, 20f),
                outcomeColor);
            Color previousColor = GUI.color;
            GUI.color = outcomeColor;
            GUI.Label(
                new Rect(x + 16f, replayCard.yMax + 14f, 150f, 28f),
                isDead ? "DEAD" : "CLEAR",
                replayOutcomeStyle);
            GUI.color = previousColor;
        }

        private void DrawReplayTimeline()
        {
            float scrimTop = Screen.height -
                             Mathf.Clamp(Screen.height * 0.25f, 180f, 270f);
            DrawSolidRect(
                new Rect(0f, scrimTop, Screen.width, Screen.height - scrimTop),
                WithAlpha(replayPanelColor, 0.62f));

            float timelineMargin = Mathf.Max(58f, Screen.width * 0.058f);
            Rect bar = new Rect(
                timelineMargin,
                Screen.height - 181f,
                Mathf.Max(1f, Screen.width - timelineMargin * 2f),
                2f);
            GUI.Label(
                new Rect(Screen.width * 0.5f - 150f, bar.y - 58f, 300f, 34f),
                $"{FormatReplayTime(replay.PlaybackElapsed)}  /  " +
                FormatReplayTime(replay.RecordedDuration),
                replayTimeStyle);
            DrawSolidRect(bar, new Color(0.46f, 0.53f, 0.57f, 0.88f));

            for (int i = 1; i < 10; i++)
            {
                float tickX = bar.x + bar.width * (i / 10f);
                DrawSolidRect(
                    new Rect(tickX - 0.5f, bar.center.y - 5f, 1f, 10f),
                    new Color(0.62f, 0.7f, 0.73f, 0.5f));
            }

            float duration = Mathf.Max(0.0001f, replay.RecordedDuration);
            float progress = Mathf.Clamp01(replay.PlaybackElapsed / duration);
            DrawSolidRect(
                new Rect(
                    bar.x,
                    bar.center.y - 1.5f,
                    bar.width * progress,
                    3f),
                replayBlue);
            DrawReplayEventMarkers(bar, duration);

            float thumbX = bar.x + bar.width * progress;
            DrawSolidRect(
                new Rect(thumbX - 0.5f, bar.center.y + 6f, 1f, 42f),
                WithAlpha(replayBlue, 0.55f));
            DrawCircle(
                new Vector2(thumbX, bar.center.y),
                19f,
                WithAlpha(replayBlue, 0.18f));
            DrawCircle(
                new Vector2(thumbX, bar.center.y),
                13f,
                replayBlue);

            const float restartWidth = 124f;
            const float nextWidth = 148f;
            const float dividerMargin = 22f;
            const float dividerWidth = 1f;
            float totalHintWidth = restartWidth;
            if (stage.CanAdvanceToNextStage)
            {
                totalHintWidth += dividerMargin * 2f + dividerWidth + nextWidth;
            }

            float hintX = (Screen.width - totalHintWidth) * 0.5f;
            hintX = DrawReplayKeyHint(
                hintX,
                bar.y + 92f,
                InputBindingDisplay.Get("Restart"),
                "RESTART",
                restartWidth);
            if (stage.CanAdvanceToNextStage)
            {
                float dividerX = hintX + dividerMargin;
                DrawSolidRect(
                    new Rect(dividerX, bar.y + 95f, dividerWidth, 22f),
                    WithAlpha(CyberHudPalette.Text, 0.35f));
                DrawReplayKeyHint(
                    dividerX + dividerWidth + dividerMargin,
                    bar.y + 92f,
                    InputBindingDisplay.Get("NextStage"),
                    "NEXT STAGE",
                    nextWidth);
            }
        }

        private void DrawReplayEventMarkers(Rect bar, float duration)
        {
            IReadOnlyList<StageReplayController.ReplayTimelineEvent> events =
                replay.TimelineEvents;
            float lastRowZeroX = -1000f;
            float lastRowOneX = -1000f;
            bool hasOutcome = false;

            for (int i = 0; i < events.Count; i++)
            {
                StageReplayController.ReplayTimelineEvent timelineEvent =
                    events[i];
                bool isOutcome =
                    timelineEvent.Kind ==
                    StageReplayController.ReplayTimelineEventKind.Clear ||
                    timelineEvent.Kind ==
                    StageReplayController.ReplayTimelineEventKind.Dead;
                hasOutcome |= isOutcome;

                float x = bar.x +
                          bar.width * Mathf.Clamp01(
                              timelineEvent.PlaybackTime / duration);
                int row = x - lastRowZeroX < 72f ? 1 : 0;
                if (row == 1 && !isOutcome && x - lastRowOneX < 72f)
                {
                    row = i % 2;
                }

                DrawReplayEventMarker(
                    timelineEvent.Kind,
                    x,
                    bar.y,
                    row);
                if (row == 0)
                {
                    lastRowZeroX = x;
                }
                else
                {
                    lastRowOneX = x;
                }
            }

            if (hasOutcome)
            {
                return;
            }

            if (stage.CurrentState == StageController.StageState.PlayerDead)
            {
                DrawReplayEventMarker(
                    StageReplayController.ReplayTimelineEventKind.Dead,
                    bar.xMax,
                    bar.y,
                    0);
            }
            else if (stage.CanAdvanceToNextStage)
            {
                DrawReplayEventMarker(
                    StageReplayController.ReplayTimelineEventKind.Clear,
                    bar.xMax,
                    bar.y,
                    0);
            }
        }

        private void DrawReplayEventMarker(
            StageReplayController.ReplayTimelineEventKind kind,
            float x,
            float barY,
            int row)
        {
            string label;
            Color color;
            bool circle;
            float iconSize;
            float labelWidth;
            switch (kind)
            {
                case StageReplayController.ReplayTimelineEventKind.Kill:
                    label = "KILL";
                    color = replayBlue;
                    circle = true;
                    iconSize = 12f;
                    labelWidth = 44f;
                    break;
                case StageReplayController.ReplayTimelineEventKind.Deadline:
                    label = "DEADLINE";
                    color = new Color(0.38f, 0.72f, 1f, 1f);
                    circle = false;
                    iconSize = 13f;
                    labelWidth = 76f;
                    break;
                case StageReplayController.ReplayTimelineEventKind.Clear:
                    label = "CLEAR";
                    color = replayClearColor;
                    circle = false;
                    iconSize = 16f;
                    labelWidth = 54f;
                    break;
                case StageReplayController.ReplayTimelineEventKind.Dead:
                    label = "DEAD";
                    color = replayDeadColor;
                    circle = false;
                    iconSize = 16f;
                    labelWidth = 48f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            float centerY = barY + 1f - row * 22f;
            if (row > 0)
            {
                DrawSolidRect(
                    new Rect(x - 0.5f, centerY, 1f, barY - centerY),
                    WithAlpha(color, 0.55f));
            }
            if (circle)
            {
                DrawCircle(new Vector2(x, centerY), iconSize, color);
            }
            else
            {
                DrawDiamond(new Vector2(x, centerY), iconSize, color);
            }

            float labelX = Mathf.Clamp(
                x - labelWidth * 0.5f,
                GetReplayFrameInset() + 8f,
                Screen.width - GetReplayFrameInset() - labelWidth - 8f);
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.Label(
                new Rect(
                    labelX,
                    row == 0 ? centerY + 18f : centerY - 42f,
                    labelWidth,
                    18f),
                label,
                replayEventStyle);
            GUI.color = previousColor;
        }

        private float DrawReplayKeyHint(
            float left,
            float y,
            string key,
            string label,
            float width)
        {
            Rect keyRect = new Rect(left, y, 28f, 28f);
            DrawSolidRect(keyRect, new Color(0.02f, 0.05f, 0.08f, 0.9f));
            DrawOutline(keyRect, WithAlpha(CyberHudPalette.Text, 0.65f), 1f);
            GUI.Label(keyRect, key, replayKeyStyle);
            GUI.Label(
                new Rect(left + 40f, y, width - 40f, 28f),
                label,
                replayKeyLabelStyle);
            return left + width;
        }

        private void DrawReplayCardOutline(
            Rect rect,
            float cut,
            Color color)
        {
            DrawLine(
                new Vector2(rect.x, rect.y),
                new Vector2(rect.xMax, rect.y),
                1f,
                color);
            DrawLine(
                new Vector2(rect.x, rect.y),
                new Vector2(rect.x, rect.yMax),
                1f,
                color);
            DrawLine(
                new Vector2(rect.xMax, rect.y),
                new Vector2(rect.xMax, rect.yMax - cut),
                1f,
                color);
            DrawLine(
                new Vector2(rect.xMax, rect.yMax - cut),
                new Vector2(rect.xMax - cut, rect.yMax),
                1f,
                color);
            DrawLine(
                new Vector2(rect.xMax - cut, rect.yMax),
                new Vector2(rect.x, rect.yMax),
                1f,
                color);
        }

        private void DrawLine(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.001f)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(start.x, start.y - thickness * 0.5f, length, thickness),
                whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawCornerBracket(
            Vector2 corner,
            float length,
            float thickness,
            float horizontalDirection,
            float verticalDirection)
        {
            float horizontalX = horizontalDirection > 0f
                ? corner.x
                : corner.x - length;
            float horizontalY = verticalDirection > 0f
                ? corner.y
                : corner.y - thickness;
            float verticalX = horizontalDirection > 0f
                ? corner.x
                : corner.x - thickness;
            float verticalY = verticalDirection > 0f
                ? corner.y
                : corner.y - length;
            DrawSolidRect(
                new Rect(horizontalX, horizontalY, length, thickness),
                replayFrameColor);
            DrawSolidRect(
                new Rect(verticalX, verticalY, thickness, length),
                replayFrameColor);
        }

        private void DrawCircle(Vector2 center, float size, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    center.x - size * 0.5f,
                    center.y - size * 0.5f,
                    size,
                    size),
                circleTexture);
            GUI.color = previous;
        }

        private void DrawDiamond(Vector2 center, float size, Color color)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(45f, center);
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    center.x - size * 0.5f,
                    center.y - size * 0.5f,
                    size,
                    size),
                whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawOutline(Rect rect, Color color, float thickness)
        {
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawSolidRect(
                new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
                color);
            DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawSolidRect(
                new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
                color);
        }

        private void DrawSolidRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static float GetReplayFrameInset()
        {
            return Mathf.Clamp(Screen.height * 0.028f, 18f, 32f);
        }

        private static string FormatReplayTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds - minutes * 60f;
            return $"{minutes:00}:{remainingSeconds:00.0}";
        }

        private static string ResolveStageLabel(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return "STAGE --";
            }

            int digitStart = -1;
            for (int i = 0; i < sceneName.Length; i++)
            {
                if (char.IsDigit(sceneName[i]))
                {
                    digitStart = i;
                    break;
                }
            }

            if (digitStart >= 0 &&
                int.TryParse(sceneName.Substring(digitStart), out int number))
            {
                return $"STAGE {number:00}";
            }

            return sceneName.ToUpperInvariant();
        }

        private void EnsureReplayStyles()
        {
            if (replayTitleStyle != null)
            {
                return;
            }

            whiteTexture = Texture2D.whiteTexture;
            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Font regularFont = fontSettings == null
                ? null
                : fontSettings.RegularFont;
            Font boldFont = fontSettings == null
                ? null
                : fontSettings.BoldFont;
            GUIStyle baseStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = CyberHudPalette.Text },
                alignment = TextAnchor.UpperLeft
            };
            replayTitleStyle = new GUIStyle(baseStyle)
            {
                fontSize = 28,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            replayMetaStyle = new GUIStyle(baseStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = WithAlpha(CyberHudPalette.Text, 0.72f) }
            };
            replayTimeStyle = new GUIStyle(baseStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            replayEventStyle = new GUIStyle(baseStyle)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = WithAlpha(Color.white, 0.9f) }
            };
            replayKeyStyle = new GUIStyle(baseStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            replayKeyLabelStyle = new GUIStyle(baseStyle)
            {
                font = regularFont,
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = WithAlpha(CyberHudPalette.Text, 0.86f) }
            };
            replayOutcomeStyle = new GUIStyle(baseStyle)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            circleTexture = CreateCircleTexture(32);
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false)
            {
                name = "Runtime Replay Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            float radius = size * 0.5f;
            Vector2 center = new Vector2(radius - 0.5f, radius - 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Configure(
            StageController stageController,
            WorldTimeController timeSource,
            PlayerHealth health,
            PlayerDash dash,
            DeadlineController deadlineController,
            WeaponController weaponController,
            StageReplayController replayController)
        {
            stage = stageController;
            worldTime = timeSource;
            playerHealth = health;
            playerDash = dash;
            deadline = deadlineController;
            weapon = weaponController;
            replay = replayController;
            replayStageLabel = ResolveStageLabel(
                SceneManager.GetActiveScene().name);
            ResolvePlayerCombat();
        }

        private string GetStageMessage()
        {
            switch (stage.CurrentState)
            {
                case StageController.StageState.Cleared:
                    return $"구역 클리어\n{InputBindingDisplay.Get("NextStage")}: 다음 스테이지\n" +
                           $"{InputBindingDisplay.Get("Restart")}: 다시 시작";
                case StageController.StageState.Replaying:
                    return $"스테이지 클리어\n리플레이 재생 중\n" +
                           $"{InputBindingDisplay.Get("NextStage")}: 다음 스테이지\n" +
                           $"{InputBindingDisplay.Get("Restart")}: 다시 시작";
                case StageController.StageState.PlayerDead:
                    return replay.IsReplaying
                        ? $"사망했습니다\n리플레이 재생 중\n{InputBindingDisplay.Get("Restart")}: 다시 시작"
                        : $"사망했습니다\n{InputBindingDisplay.Get("Restart")}: 다시 시작";
                default:
                    return null;
            }
        }

        private HudControlHint[] GetControlHints()
        {
            if (replay.IsReplaying)
            {
                return stage.CanAdvanceToNextStage
                    ? CreateAdvanceControlHints()
                    : CreateRestartControlHints();
            }

            if (stage.CanAdvanceToNextStage)
            {
                return CreateAdvanceControlHints();
            }

            return new[]
            {
                new HudControlHint(InputBindingDisplay.GetMovement(), "이동"),
                new HudControlHint("MOUSE", "조준"),
                new HudControlHint(InputBindingDisplay.Get("Fire"), "공격 / 자동소총 연사"),
                new HudControlHint(InputBindingDisplay.Get("Throw"), "투척"),
                new HudControlHint(InputBindingDisplay.Get("Deadline"), "DEADLINE"),
                new HudControlHint(InputBindingDisplay.Get("Dash"), "대시"),
                new HudControlHint(InputBindingDisplay.Get("Interact"), "잡기 / 획득 / 교체"),
                new HudControlHint(InputBindingDisplay.Get("Restart"), "다시 시작")
            };
        }

        private static HudControlHint[] CreateAdvanceControlHints()
        {
            return new[]
            {
                new HudControlHint(InputBindingDisplay.Get("Restart"), "다시 시작"),
                new HudControlHint(InputBindingDisplay.Get("NextStage"), "다음 스테이지")
            };
        }

        private static HudControlHint[] CreateRestartControlHints()
        {
            return new[]
            {
                new HudControlHint(InputBindingDisplay.Get("Restart"), "다시 시작")
            };
        }

        private void ResolvePlayerCombat()
        {
            if (playerCombat == null && weapon != null)
            {
                playerCombat = weapon.GetComponent<PlayerCombat>();
            }
        }

        private void DrawDeadlineFeedback(CyberHudLayout layout)
        {
            if (!deadline.IsActive)
            {
                return;
            }

            string causes = deadline.RejectedActionFeedback
                ? "원인 가득 참"
                : $"원인 {deadline.StagedActionCount}/{deadline.MaxStagedActions}";
            string text =
                $"{causes}\n" +
                "이동하여 실행";
            Rect panel = layout.TopMessagePanel;
            GetRenderer().DrawPanel(
                panel,
                layout.Scale,
                CyberHudPalette.Accent);
            GUI.Label(
                new Rect(
                    panel.x + 20f * layout.Scale,
                    panel.y + 10f * layout.Scale,
                    panel.width - 40f * layout.Scale,
                    34f * layout.Scale),
                "DEADLINE",
                deadlineTitleStyle);
            GUI.Label(
                new Rect(
                    panel.x + 20f * layout.Scale,
                    panel.y + 46f * layout.Scale,
                    panel.width - 40f * layout.Scale,
                    panel.height - 56f * layout.Scale),
                text,
                deadlineBodyStyle);
        }

        private CyberHudRenderer GetRenderer()
        {
            return hudRenderer ??= new CyberHudRenderer();
        }

        private void EnsureStyles(float scale)
        {
            if (messageStyle != null && Mathf.Abs(styledScale - scale) < 0.001f)
            {
                return;
            }

            styledScale = scale;
            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Font regularFont = fontSettings == null
                ? null
                : fontSettings.RegularFont;
            Font boldFont = fontSettings == null
                ? null
                : fontSettings.BoldFont;

            messageStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = Mathf.Max(18, Mathf.RoundToInt(24f * scale)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = CyberHudPalette.Text }
            };
            clearMessageStyle = new GUIStyle(messageStyle)
            {
                normal = { textColor = CyberHudPalette.Amber }
            };
            overlayMessageStyle = new GUIStyle(messageStyle)
            {
                fontSize = Mathf.Max(16, Mathf.RoundToInt(20f * scale))
            };
            overlayClearMessageStyle = new GUIStyle(overlayMessageStyle)
            {
                normal = { textColor = CyberHudPalette.Amber }
            };
            deadlineTitleStyle = new GUIStyle(messageStyle)
            {
                fontSize = Mathf.Max(15, Mathf.RoundToInt(20f * scale)),
                normal = { textColor = CyberHudPalette.Accent }
            };
            deadlineBodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * scale)),
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(
                        CyberHudPalette.Text.r,
                        CyberHudPalette.Text.g,
                        CyberHudPalette.Text.b,
                        0.82f)
                }
            };
        }

        private void OnDestroy()
        {
            hudRenderer?.Dispose();
            if (circleTexture != null)
            {
                Destroy(circleTexture);
                circleTexture = null;
            }
        }
    }
}
