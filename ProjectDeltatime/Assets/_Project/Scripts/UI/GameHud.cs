using System;
using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Level;
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

        [Header("Debug HUD")]
        [SerializeField] private Color panelColor = new Color(0.02f, 0.025f, 0.04f, 0.86f);
        [SerializeField] private Color textColor = new Color(0.85f, 0.95f, 1f, 1f);
        [SerializeField] private Color accentColor = new Color(0.2f, 0.95f, 1f, 1f);

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

        private GUIStyle statusStyle;
        private GUIStyle messageStyle;
        private GUIStyle overlayMessageStyle;
        private GUIStyle controlsStyle;
        private GUIStyle replayTitleStyle;
        private GUIStyle replayMetaStyle;
        private GUIStyle replayTimeStyle;
        private GUIStyle replayEventStyle;
        private GUIStyle replayKeyStyle;
        private GUIStyle replayKeyLabelStyle;
        private GUIStyle replayOutcomeStyle;
        private Texture2D whiteTexture;
        private Texture2D panelTexture;
        private Texture2D circleTexture;
        private string replayStageLabel;

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
                Debug.LogError($"{nameof(GameHud)} is missing required references.", this);
                enabled = false;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (replay.IsReplaying)
            {
                DrawReplayHud();
                return;
            }

            string weaponName = weapon.Definition == null
                ? "빈손"
                : weapon.Definition.DisplayName;
            string ammunition = weapon.Definition == null
                ? "--"
                : weapon.Definition.IsMelee
                    ? "근접"
                    : $"{weapon.Ammunition}/{weapon.Definition.AmmunitionCapacity}";
            string dashState = playerDash.CooldownRemaining <= 0f
                ? "준비 완료"
                : $"{playerDash.CooldownRemaining:0.0}s";
            string deadlineCharges =
                $"{deadline.ChargesRemaining}/{deadline.MaxCharges}";
            string deadlineState =
                !deadline.IsActive &&
                deadline.HasCharges &&
                deadline.CooldownRemaining > 0f
                    ? $"{deadlineCharges} | {deadline.CooldownRemaining:0.00}w"
                    : deadlineCharges;

            bool isDeathReplay = replay.IsReplaying &&
                                 stage.CurrentState ==
                                 StageController.StageState.PlayerDead;
            string replaySpeedLabel = "리플레이 정규화 1.00x";
            if (replay.CurrentPlaybackPhase ==
                StageReplayController.ReplayPlaybackPhase.Deadline)
            {
                replaySpeedLabel = "DEADLINE · 정규화 1.00x";
            }
            else if (replay.CurrentPlaybackPhase ==
                     StageReplayController.ReplayPlaybackPhase.DeadlineAftermath)
            {
                replaySpeedLabel = "DEADLINE 이후 · 정규화 1.00x";
            }
            string timeStatus = replay.IsReplaying
                ? $"스테이지 클리어  ·  {replaySpeedLabel}\n리플레이 시간  {replay.PlaybackElapsed:0.0}/{replay.RecordedDuration:0.0}s"
                : $"실시간  {stage.RealPlayTime:0.0}s\n월드  {worldTime.CurrentTimeScale:0.00}x";
            if (isDeathReplay)
            {
                timeStatus = $"사망  {replaySpeedLabel}\n리플레이 시간  {replay.PlaybackElapsed:0.0}/{replay.RecordedDuration:0.0}s";
            }

            string status =
                $"적  {stage.RemainingEnemyCount}\n" +
                $"{timeStatus}\n" +
                $"대시  {dashState}\n" +
                $"DEADLINE  {deadlineState}";
            string vitalStatus =
                $"체력  {playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}\n" +
                $"무기  {weaponName}  {ammunition}";

            Rect statusPanel = new Rect(18f, 18f, 330f, 178f);
            GUI.DrawTexture(statusPanel, panelTexture);
            GUI.Label(new Rect(32f, 28f, 300f, 112f), status, statusStyle);

            Rect barBackground = new Rect(32f, 158f, 300f, 10f);
            GUI.DrawTexture(barBackground, whiteTexture);
            Color previousColor = GUI.color;
            GUI.color = accentColor;
            float progress = replay.IsReplaying && replay.RecordedDuration > 0f
                ? replay.PlaybackElapsed / replay.RecordedDuration
                : worldTime.CurrentTimeScale;
            GUI.DrawTexture(
                new Rect(
                    barBackground.x,
                    barBackground.y,
                    barBackground.width * Mathf.Clamp01(progress),
                    barBackground.height),
                whiteTexture);
            GUI.color = previousColor;

            Rect vitalPanel = CreateBottomLeftOverlay(330f, 76f, 78f);
            GUI.DrawTexture(vitalPanel, panelTexture);
            GUI.Label(
                new Rect(
                    vitalPanel.x + 14f,
                    vitalPanel.y + 10f,
                    vitalPanel.width - 28f,
                    vitalPanel.height - 20f),
                vitalStatus,
                statusStyle);

            string message = null;
            switch (stage.CurrentState)
            {
                case StageController.StageState.Cleared:
                    message = "구역 클리어\nN: 다음 스테이지\nR: 다시 시작";
                    break;
                case StageController.StageState.Replaying:
                    message = "스테이지 클리어\n리플레이 재생 중\nN: 다음 스테이지\nR: 다시 시작";
                    break;
                case StageController.StageState.PlayerDead:
                    message = replay.IsReplaying
                        ? "사망했습니다\n리플레이 재생 중\nR: 다시 시작"
                        : "사망했습니다\nR: 다시 시작";
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                Rect messagePanel = replay.IsReplaying
                    ? CreateTopCenterOverlay(330f, 144f)
                    : new Rect(
                        (Screen.width - 460f) * 0.5f,
                        (Screen.height - 168f) * 0.5f,
                        460f,
                        168f);
                GUI.DrawTexture(messagePanel, panelTexture);
                GUI.Label(
                    new Rect(
                        messagePanel.x + 20f,
                        messagePanel.y + 16f,
                        messagePanel.width - 40f,
                        messagePanel.height - 32f),
                    message,
                    replay.IsReplaying ? overlayMessageStyle : messageStyle);
            }

            DrawDeadlineFeedback();

            string controls;
            if (replay.IsReplaying)
            {
                controls = stage.CanAdvanceToNextStage
                    ? "N: 다음 스테이지  |  R: 다시 시작"
                    : "R: 다시 시작";
            }
            else if (stage.CanAdvanceToNextStage)
            {
                controls = "N: 다음 스테이지  |  R: 다시 시작";
            }
            else
            {
                controls = "WASD 이동  |  마우스 조준  |  LMB - 좌 클릭 공격 / 자동소총 연사  |  RMB - 우 클릭 투척\n" +
                           "Q DEADLINE  |  Space 대시  |  E 잡기 / 획득 / 교체  |  R 다시 시작";
            }
            GUI.Label(
                new Rect(18f, Screen.height - 64f, Screen.width - 36f, 52f),
                controls,
                controlsStyle);
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
                "R",
                "RESTART",
                restartWidth);
            if (stage.CanAdvanceToNextStage)
            {
                float dividerX = hintX + dividerMargin;
                DrawSolidRect(
                    new Rect(dividerX, bar.y + 95f, dividerWidth, 22f),
                    WithAlpha(textColor, 0.35f));
                DrawReplayKeyHint(
                    dividerX + dividerWidth + dividerMargin,
                    bar.y + 92f,
                    "N",
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
            DrawOutline(keyRect, WithAlpha(textColor, 0.65f), 1f);
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
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), 1f, color);
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.yMax), 1f, color);
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
        }

        private void EnsureStyles()
        {
            if (statusStyle != null)
            {
                return;
            }

            whiteTexture = Texture2D.whiteTexture;
            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Font regularFont = fontSettings == null ? null : fontSettings.RegularFont;
            Font boldFont = fontSettings == null ? null : fontSettings.BoldFont;
            if (regularFont == null || boldFont == null)
            {
                Debug.LogError(
                    "Korean UI font settings are missing. Run Tools/UI/Apply Korean Localization.",
                    this);
            }
            panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Runtime HUD Panel"
            };
            panelTexture.SetPixel(0, 0, panelColor);
            panelTexture.Apply();

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor },
                alignment = TextAnchor.UpperLeft
            };

            messageStyle = new GUIStyle(statusStyle)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };

            overlayMessageStyle = new GUIStyle(messageStyle)
            {
                fontSize = 20
            };

            controlsStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = 14,
                normal = { textColor = new Color(textColor.r, textColor.g, textColor.b, 0.85f) },
                alignment = TextAnchor.LowerCenter
            };

            replayTitleStyle = new GUIStyle(statusStyle)
            {
                fontSize = 28,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            replayMetaStyle = new GUIStyle(statusStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = WithAlpha(textColor, 0.72f) }
            };

            replayTimeStyle = new GUIStyle(statusStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            replayEventStyle = new GUIStyle(statusStyle)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = WithAlpha(Color.white, 0.9f) }
            };

            replayKeyStyle = new GUIStyle(statusStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            replayKeyLabelStyle = new GUIStyle(statusStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = WithAlpha(textColor, 0.86f) }
            };

            replayOutcomeStyle = new GUIStyle(statusStyle)
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

        private void DrawDeadlineFeedback()
        {
            if (!deadline.IsActive)
            {
                return;
            }

            string causes = deadline.RejectedActionFeedback
                ? "원인 가득 참"
                : $"원인 {deadline.StagedActionCount}/{deadline.MaxStagedActions}";
            string text =
                "DEADLINE\n" +
                $"{causes}\n" +
                "이동하여 실행";
            Rect panel = CreateTopCenterOverlay(330f, 142f);
            GUI.DrawTexture(panel, panelTexture);
            GUI.Label(
                new Rect(
                    panel.x + 20f,
                    panel.y + 8f,
                    panel.width - 40f,
                    panel.height - 16f),
                text,
                overlayMessageStyle);
        }

        private static Rect CreateTopCenterOverlay(float preferredWidth, float height)
        {
            const float screenMargin = 18f;
            float width = Mathf.Min(preferredWidth, Screen.width - screenMargin * 2f);
            return new Rect(
                (Screen.width - width) * 0.5f,
                screenMargin,
                width,
                height);
        }

        private static Rect CreateBottomLeftOverlay(
            float preferredWidth,
            float height,
            float bottomOffset)
        {
            const float screenMargin = 18f;
            float width = Mathf.Min(preferredWidth, Screen.width - screenMargin * 2f);
            return new Rect(
                screenMargin,
                Mathf.Max(screenMargin, Screen.height - bottomOffset - height),
                width,
                height);
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }

            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
        }
    }
}
