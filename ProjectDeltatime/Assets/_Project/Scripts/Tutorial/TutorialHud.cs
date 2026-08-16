using Deltatime.Combat;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.UI;
using UnityEngine;

namespace Deltatime.Tutorial
{
    public sealed class TutorialHud : MonoBehaviour
    {
        [SerializeField] private TutorialDirector director;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private DeadlineController deadline;

        private GUIStyle titleStyle;
        private GUIStyle instructionStyle;
        private GUIStyle statusStyle;
        private GUIStyle completeStyle;
        private GUIStyle interactionPromptStyle;
        private GUIStyle interactionKeyStyle;
        private Texture2D panelTexture;
        private Texture2D whiteTexture;
        private PlayerCombat playerCombat;

        private void Awake()
        {
            if (director == null || worldTime == null ||
                weapon == null || deadline == null)
            {
                Debug.LogError(
                    $"{nameof(TutorialHud)} is missing required references.",
                    this);
                enabled = false;
            }

            ResolvePlayerCombat();
        }

        private void OnGUI()
        {
            EnsureStyles();

            string weaponName = weapon.Definition == null
                ? "빈손"
                : weapon.Definition.DisplayName;
            string ammunition = weapon.Definition != null &&
                                weapon.Definition.IsFirearm
                ? $"  {weapon.Ammunition}/{weapon.Definition.AmmunitionCapacity}"
                : string.Empty;

            Rect statusPanel = new Rect(18f, 18f, 285f, 116f);
            GUI.DrawTexture(statusPanel, panelTexture);
            GUI.Label(
                new Rect(32f, 28f, 255f, 98f),
                $"월드  {worldTime.CurrentTimeScale:0.00}x\n" +
                $"무기  {weaponName}{ammunition}\n" +
                $"DEADLINE  {deadline.ChargesRemaining}/{deadline.MaxCharges}",
                statusStyle);

            if (director.Completed)
            {
                Rect completePanel = new Rect(
                    (Screen.width - 560f) * 0.5f,
                    (Screen.height - 190f) * 0.5f,
                    560f,
                    190f);
                GUI.DrawTexture(completePanel, panelTexture);
                GUI.Label(completePanel, "튜토리얼 완료\n잠시 후 스테이지 1로 이동합니다", completeStyle);
                return;
            }

            Rect lessonPanel = new Rect(
                (Screen.width - 720f) * 0.5f,
                Screen.height - 174f,
                720f,
                148f);
            GUI.DrawTexture(lessonPanel, panelTexture);
            GUI.Label(
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 12f, 676f, 36f),
                director.StepTitle,
                titleStyle);
            GUI.Label(
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 52f, 676f, 48f),
                director.Instruction,
                instructionStyle);
            GUI.Label(
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 104f, 676f, 28f),
                director.ProgressText + "    |    R: 다시 시작",
                statusStyle);
            DrawWeaponInteractionPrompt(lessonPanel);
        }

        public void Configure(
            TutorialDirector tutorialDirector,
            WorldTimeController timeSource,
            WeaponController playerWeapon,
            DeadlineController deadlineController)
        {
            director = tutorialDirector;
            worldTime = timeSource;
            weapon = playerWeapon;
            deadline = deadlineController;
            ResolvePlayerCombat();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Tutorial HUD Panel"
            };
            panelTexture.SetPixel(0, 0, new Color(0.015f, 0.025f, 0.045f, 0.9f));
            panelTexture.Apply();
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

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 1f, 1f, 1f) }
            };
            instructionStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = 15,
                normal = { textColor = new Color(0.75f, 0.88f, 0.96f, 1f) }
            };
            completeStyle = new GUIStyle(titleStyle)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };
            interactionPromptStyle = new GUIStyle(titleStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            interactionKeyStyle = new GUIStyle(interactionPromptStyle)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void DrawWeaponInteractionPrompt(Rect lessonPanel)
        {
            ResolvePlayerCombat();
            string action = GetWeaponInteractionAction();
            if (string.IsNullOrEmpty(action))
            {
                return;
            }

            const float screenMargin = 18f;
            const float height = 46f;
            float width = Mathf.Min(246f, Screen.width - screenMargin * 2f);
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                Mathf.Max(screenMargin, lessonPanel.y - height - 20f),
                width,
                height);
            Color accent = new Color(0.2f, 1f, 1f, 0.86f);
            GUI.DrawTexture(panel, panelTexture);
            DrawOutline(panel, accent, 1f);

            Rect key = new Rect(panel.x + 10f, panel.y + 9f, 30f, 28f);
            DrawSolidRect(key, new Color(0.005f, 0.015f, 0.03f, 0.96f));
            DrawOutline(key, accent, 1f);
            GUI.Label(key, "E", interactionKeyStyle);
            GUI.Label(
                new Rect(
                    key.xMax + 12f,
                    panel.y,
                    panel.xMax - key.xMax - 22f,
                    panel.height),
                action,
                interactionPromptStyle);
        }

        private string GetWeaponInteractionAction()
        {
            if (playerCombat == null)
            {
                return null;
            }

            switch (playerCombat.WeaponInteractionPrompt)
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

        private void ResolvePlayerCombat()
        {
            if (playerCombat == null && weapon != null)
            {
                playerCombat = weapon.GetComponent<PlayerCombat>();
            }
        }

        private void DrawSolidRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
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

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}
