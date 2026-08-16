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
        private static readonly HudControlHint[] LiveControlHints =
        {
            new HudControlHint("WASD", "이동"),
            new HudControlHint("MOUSE", "조준"),
            new HudControlHint("LMB", "공격 / 자동소총 연사"),
            new HudControlHint("RMB", "투척"),
            new HudControlHint("Q", "DEADLINE"),
            new HudControlHint("SPACE", "대시"),
            new HudControlHint("E", "잡기 / 획득 / 교체"),
            new HudControlHint("R", "다시 시작")
        };

        private static readonly HudControlHint[] AdvanceControlHints =
        {
            new HudControlHint("R", "다시 시작"),
            new HudControlHint("N", "다음 스테이지")
        };

        private static readonly HudControlHint[] RestartControlHints =
        {
            new HudControlHint("R", "다시 시작")
        };

        [SerializeField] private StageController stage;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerDash playerDash;
        [SerializeField] private DeadlineController deadline;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private StageReplayController replay;

        private CyberHudRenderer hudRenderer;
        private GUIStyle messageStyle;
        private GUIStyle clearMessageStyle;
        private GUIStyle overlayMessageStyle;
        private GUIStyle overlayClearMessageStyle;
        private GUIStyle deadlineTitleStyle;
        private GUIStyle deadlineBodyStyle;
        private float styledScale = -1f;

        public bool HasRequiredVisualAssets =>
            GetRenderer().HasRequiredIcons &&
            (weapon == null ||
             weapon.Definition == null ||
             weapon.Definition.HudIcon != null);

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
                Debug.LogError(
                    $"{nameof(GameHud)} is missing required references.",
                    this);
                enabled = false;
            }
        }

        private void OnGUI()
        {
            CyberHudLayout layout = GetRenderer().DrawPersistentHud(
                HudDisplayFormatter.FormatStageLabel(
                    SceneManager.GetActiveScene().name),
                playerHealth,
                deadline,
                weapon,
                worldTime.CurrentTimeScale,
                replay.IsReplaying);
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
            GetRenderer().DrawControlHints(
                layout.ControlsPanel,
                layout.Scale,
                GetControlHints());
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

        private string GetStageMessage()
        {
            switch (stage.CurrentState)
            {
                case StageController.StageState.Cleared:
                    return "구역 클리어\nN: 다음 스테이지\nR: 다시 시작";
                case StageController.StageState.Replaying:
                    return "스테이지 클리어\n리플레이 재생 중\nN: 다음 스테이지\nR: 다시 시작";
                case StageController.StageState.PlayerDead:
                    return replay.IsReplaying
                        ? "사망했습니다\n리플레이 재생 중\nR: 다시 시작"
                        : "사망했습니다\nR: 다시 시작";
                default:
                    return null;
            }
        }

        private HudControlHint[] GetControlHints()
        {
            if (replay.IsReplaying)
            {
                return stage.CanAdvanceToNextStage
                    ? AdvanceControlHints
                    : RestartControlHints;
            }

            if (stage.CanAdvanceToNextStage)
            {
                return AdvanceControlHints;
            }

            return LiveControlHints;
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
        }
    }
}
