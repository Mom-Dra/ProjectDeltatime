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
                ? "UNARMED"
                : weapon.Definition.DisplayName.ToUpperInvariant();
            string ammunition = weapon.Definition == null
                ? "--"
                : weapon.Definition.IsMelee
                    ? "MELEE"
                    : $"{weapon.Ammunition}/{weapon.Definition.AmmunitionCapacity}";
            string dashState = playerDash.CooldownRemaining <= 0f
                ? "READY"
                : $"{playerDash.CooldownRemaining:0.0}s";
            string deadlineState = deadline.IsActive
                ? $"{deadline.StagedActionCount}/{deadline.MaxStagedActions}"
                : deadline.CooldownRemaining > 0f
                    ? $"{deadline.CooldownRemaining:0.00}w"
                    : "READY";

            string replayView = replay.IsOmniscientViewEnabled
                ? "FULL"
                : "DARK";
            string timeStatus = replay.IsReplaying
                ? $"STAGE CLEAR  •  REPLAY 1.00x\nREPLAY TIME  {replay.PlaybackElapsed:0.0}/{replay.RecordedDuration:0.0}s\nVIEW  {replayView}"
                : $"REAL TIME  {stage.RealPlayTime:0.0}s\nWORLD  {worldTime.CurrentTimeScale:0.00}x";
            string status =
                $"ENEMIES  {stage.RemainingEnemyCount}\n" +
                $"HEALTH  {playerHealth.CurrentHealth}/{playerHealth.MaximumHealth}\n" +
                $"{timeStatus}\n" +
                $"DASH  {dashState}\n" +
                $"DEADLINE  {deadlineState}\n" +
                $"WEAPON  {weaponName}  {ammunition}";

            Rect statusPanel = new Rect(18f, 18f, 300f, 222f);
            GUI.DrawTexture(statusPanel, panelTexture);
            GUI.Label(new Rect(32f, 28f, 270f, 166f), status, statusStyle);

            Rect barBackground = new Rect(32f, 212f, 270f, 10f);
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
                    message = "ROOM CLEAR\nPress R to restart";
                    break;
                case StageController.StageState.Replaying:
                    message = null;
                    break;
                case StageController.StageState.PlayerDead:
                    message = "YOU DIED\nPress R to restart";
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                Rect messagePanel = new Rect(
                    (Screen.width - 420f) * 0.5f,
                    (Screen.height - 150f) * 0.5f,
                    420f,
                    150f);
                GUI.DrawTexture(messagePanel, panelTexture);
                GUI.Label(messagePanel, message, messageStyle);
            }

            DrawDeadlineFeedback();

            string controls = replay.IsReplaying
                ? "V Toggle Full View  |  R Restart"
                : "WASD Move  |  Mouse Aim  |  LMB Attack  |  RMB Throw\n" +
                  "Space Dash  |  E Catch / Pick up / Swap  |  R Restart";
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
            panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Runtime HUD Panel"
            };
            panelTexture.SetPixel(0, 0, panelColor);
            panelTexture.Apply();

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor },
                alignment = TextAnchor.UpperLeft
            };

            messageStyle = new GUIStyle(statusStyle)
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter
            };

            controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(textColor.r, textColor.g, textColor.b, 0.85f) },
                alignment = TextAnchor.LowerCenter
            };
        }

        private void DrawDeadlineFeedback()
        {
            if (deadline.IsActive)
            {
                string causes = deadline.RejectedActionFeedback
                    ? "CAUSES FULL"
                    : $"CAUSES {deadline.StagedActionCount}/{deadline.MaxStagedActions}";
                string text =
                    $"DEADLINE  |  IMPACT {deadline.ImpactTime:0.00}s\n" +
                    $"{causes}\n" +
                    "MOVE TO RELEASE";
                Rect panel = new Rect(
                    (Screen.width - 480f) * 0.5f,
                    Screen.height * 0.62f,
                    480f,
                    132f);
                GUI.DrawTexture(panel, panelTexture);
                GUI.Label(panel, text, messageStyle);
                return;
            }

            if (!deadline.HasThreat)
            {
                return;
            }

            string warning =
                $"RELEASE TO DEADLINE  |  {deadline.ImpactTime:0.00}s";
            Rect warningPanel = new Rect(
                (Screen.width - 460f) * 0.5f,
                28f,
                460f,
                54f);
            GUI.DrawTexture(warningPanel, panelTexture);
            GUI.Label(warningPanel, warning, statusStyle);
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
