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

        private CyberHudRenderer hudRenderer;
        private GUIStyle titleStyle;
        private GUIStyle instructionStyle;
        private GUIStyle statusStyle;
        private GUIStyle completeStyle;
        private PlayerCombat playerCombat;
        private float styledScale = -1f;

        public bool HasRequiredVisualAssets =>
            GetRenderer().HasRequiredIcons &&
            (weapon == null ||
             weapon.Definition == null ||
             weapon.Definition.HudIcon != null);

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
            CyberHudLayout layout = GetRenderer().DrawPersistentHud(
                "TUTORIAL",
                director.PlayerHealth,
                deadline,
                weapon,
                worldTime.CurrentTimeScale,
                false);
            EnsureStyles(layout.Scale);

            if (director.Completed)
            {
                float width = Mathf.Min(560f * layout.Scale, layout.SafeArea.width);
                float height = Mathf.Min(190f * layout.Scale, layout.SafeArea.height);
                Rect completePanel = new Rect(
                    layout.SafeArea.center.x - width * 0.5f,
                    layout.SafeArea.center.y - height * 0.5f,
                    width,
                    height);
                GetRenderer().DrawPanel(
                    completePanel,
                    layout.Scale,
                    CyberHudPalette.Amber);
                GUI.Label(
                    completePanel,
                    "튜토리얼 완료\n잠시 후 스테이지 1로 이동합니다",
                    completeStyle);
                return;
            }

            Rect lessonPanel = layout.TutorialPanel;
            GetRenderer().DrawPanel(lessonPanel, layout.Scale);
            float padding = 22f * layout.Scale;
            GUI.Label(
                new Rect(
                    lessonPanel.x + padding,
                    lessonPanel.y + 12f * layout.Scale,
                    lessonPanel.width - padding * 2f,
                    36f * layout.Scale),
                director.StepTitle,
                titleStyle);
            GUI.Label(
                new Rect(
                    lessonPanel.x + padding,
                    lessonPanel.y + 50f * layout.Scale,
                    lessonPanel.width - padding * 2f,
                    52f * layout.Scale),
                director.Instruction,
                instructionStyle);
            GUI.Label(
                new Rect(
                    lessonPanel.x + padding,
                    lessonPanel.y + 108f * layout.Scale,
                    lessonPanel.width - padding * 2f,
                    28f * layout.Scale),
                director.ProgressText + "    |    R: 다시 시작",
                statusStyle);

            ResolvePlayerCombat();
            GetRenderer().DrawWeaponInteractionPrompt(
                layout,
                true,
                playerCombat == null
                    ? PlayerWeaponInteractionPrompt.None
                    : playerCombat.WeaponInteractionPrompt);
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

        private void ResolvePlayerCombat()
        {
            if (playerCombat == null && weapon != null)
            {
                playerCombat = weapon.GetComponent<PlayerCombat>();
            }
        }

        private CyberHudRenderer GetRenderer()
        {
            return hudRenderer ??= new CyberHudRenderer();
        }

        private void EnsureStyles(float scale)
        {
            if (titleStyle != null && Mathf.Abs(styledScale - scale) < 0.001f)
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

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = Mathf.Max(14, Mathf.RoundToInt(20f * scale)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = CyberHudPalette.Accent }
            };
            instructionStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(18f * scale)),
                wordWrap = true,
                normal = { textColor = CyberHudPalette.Text }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = Mathf.Max(11, Mathf.RoundToInt(15f * scale)),
                normal =
                {
                    textColor = new Color(
                        CyberHudPalette.Frame.r,
                        CyberHudPalette.Frame.g,
                        CyberHudPalette.Frame.b,
                        0.9f)
                }
            };
            completeStyle = new GUIStyle(titleStyle)
            {
                fontSize = Mathf.Max(20, Mathf.RoundToInt(30f * scale)),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = CyberHudPalette.Amber }
            };
        }

        private void OnDestroy()
        {
            hudRenderer?.Dispose();
        }
    }
}
