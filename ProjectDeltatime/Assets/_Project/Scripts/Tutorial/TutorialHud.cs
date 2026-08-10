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
        private Texture2D panelTexture;

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
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 12f, 676f, 32f),
                director.StepTitle,
                titleStyle);
            GUI.Label(
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 48f, 676f, 50f),
                director.Instruction,
                instructionStyle);
            GUI.Label(
                new Rect(lessonPanel.x + 22f, lessonPanel.y + 102f, 676f, 30f),
                director.ProgressText + "    |    R: 다시 시작",
                statusStyle);
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
                fontSize = 23,
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
