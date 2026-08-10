using Deltatime.Combat;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using UnityEngine;

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

        private GUIStyle statusStyle;
        private GUIStyle messageStyle;
        private GUIStyle overlayMessageStyle;
        private GUIStyle controlsStyle;
        private Texture2D whiteTexture;
        private Texture2D panelTexture;

        private void Awake()
        {
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
                $"체력  {playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}\n" +
                $"{timeStatus}\n" +
                $"대시  {dashState}\n" +
                $"DEADLINE  {deadlineState}\n" +
                $"무기  {weaponName}  {ammunition}";

            Rect statusPanel = new Rect(18f, 18f, 330f, 248f);
            GUI.DrawTexture(statusPanel, panelTexture);
            GUI.Label(new Rect(32f, 28f, 300f, 188f), status, statusStyle);

            Rect barBackground = new Rect(32f, 228f, 300f, 10f);
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
                    ? CreateTopRightOverlay(330f, 144f)
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
            Rect panel = CreateTopRightOverlay(330f, 142f);
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

        private static Rect CreateTopRightOverlay(float preferredWidth, float height)
        {
            const float screenMargin = 18f;
            float width = Mathf.Min(preferredWidth, Screen.width - screenMargin * 2f);
            return new Rect(
                Screen.width - screenMargin - width,
                screenMargin,
                width,
                height);
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}
