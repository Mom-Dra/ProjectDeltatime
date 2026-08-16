using UnityEngine;

namespace Deltatime.UI
{
    [CreateAssetMenu(
        fileName = "HudIconSet",
        menuName = "Deltatime/HUD Icon Set")]
    public sealed class HudIconSet : ScriptableObject
    {
        private const string ResourcePath = "Hud/HudIconSet";

        [SerializeField] private Sprite healthIcon;
        [SerializeField] private Sprite deadlineIcon;
        [SerializeField] private Sprite clockDialIcon;
        [SerializeField] private Sprite unarmedIcon;

        public Sprite HealthIcon => healthIcon;
        public Sprite DeadlineIcon => deadlineIcon;
        public Sprite ClockDialIcon => clockDialIcon;
        public Sprite UnarmedIcon => unarmedIcon;
        public bool IsConfigured =>
            healthIcon != null &&
            deadlineIcon != null &&
            clockDialIcon != null &&
            unarmedIcon != null;

        public static HudIconSet Load()
        {
            return Resources.Load<HudIconSet>(ResourcePath);
        }

        public void Configure(
            Sprite health,
            Sprite deadline,
            Sprite clockDial,
            Sprite unarmed)
        {
            healthIcon = health;
            deadlineIcon = deadline;
            clockDialIcon = clockDial;
            unarmedIcon = unarmed;
        }
    }
}
