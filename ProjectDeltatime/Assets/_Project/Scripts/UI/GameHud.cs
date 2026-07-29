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
                : $"{weapon.Ammunition}/{weapon.Definition.AmmunitionCapacity}";
            string dashState = playerDash.CooldownRemaining <= 0f
                ? "READY"
                : $"{playerDash.CooldownRemaining:0.0}s";

            string timeStatus = replay.IsReplaying
                ? $"STAGE CLEAR  •  REPLAY 1.00x\nREPLAY TIME  {replay.PlaybackElapsed:0.0}/{replay.RecordedDuration:0.0}s"
                : $"REAL TIME  {stage.RealPlayTime:0.0}s\nWORLD  {worldTime.CurrentTimeScale:0.00}x";
            string status =
                $"ENEMIES  {stage.RemainingEnemyCount}\n" +
                $"{timeStatus}\n" +
                $"DASH  {dashState}\n" +
                $"WEAPON  {weaponName}  {ammunition}";

            Rect statusPanel = new Rect(18f, 18f, 300f, 172f);
            GUI.DrawTexture(statusPanel, panelTexture);
            GUI.Label(new Rect(32f, 28f, 270f, 136f), status, statusStyle);

            Rect barBackground = new Rect(32f, 162f, 270f, 10f);
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

            const string controls =
                "WASD Move  |  Mouse Aim  |  LMB Fire  |  RMB Throw\n" +
                "Space Dash  |  E Pick up / Swap  |  R Restart";
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
            WeaponController weaponController,
            StageReplayController replayController)
        {
            stage = stageController;
            worldTime = timeSource;
            playerHealth = health;
            playerDash = dash;
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

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}
