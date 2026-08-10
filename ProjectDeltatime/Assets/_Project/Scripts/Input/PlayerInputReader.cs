using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.InputSystem
{
    [DefaultExecutionOrder(-400)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private WorldTimeActivity worldTimeActivity;

        private PlayerControls controls;

        public Vector2 Move { get; private set; }
        public Vector2 PointerScreenPosition { get; private set; }
        public bool FirePressed { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool DeadlinePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool RestartPressed { get; private set; }
        public bool NextStagePressed { get; private set; }
        public bool ReplayVisionTogglePressed { get; private set; }

        private void Awake()
        {
            controls = new PlayerControls();

            if (worldTimeActivity == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} requires a {nameof(WorldTimeActivity)} reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            controls?.Gameplay.Enable();
        }

        private void Update()
        {
            PlayerControls.GameplayActions gameplay = controls.Gameplay;

            Move = Vector2.ClampMagnitude(gameplay.Move.ReadValue<Vector2>(), 1f);
            PointerScreenPosition = gameplay.Point.ReadValue<Vector2>();
            FirePressed = gameplay.Fire.WasPressedThisFrame();
            FireHeld = gameplay.Fire.IsPressed();
            ThrowPressed = gameplay.Throw.WasPressedThisFrame();
            DashPressed = gameplay.Dash.WasPressedThisFrame();
            DeadlinePressed = gameplay.Deadline.WasPressedThisFrame();
            InteractPressed = gameplay.Interact.WasPressedThisFrame();
            RestartPressed = gameplay.Restart.WasPressedThisFrame();
            NextStagePressed = gameplay.NextStage.WasPressedThisFrame();
            ReplayVisionTogglePressed =
                gameplay.ReplayVisionToggle.WasPressedThisFrame();

            worldTimeActivity.SetMovement(Move.magnitude);
        }

        private void OnDisable()
        {
            controls?.Gameplay.Disable();
            ResetState();
        }

        private void OnDestroy()
        {
            controls?.Dispose();
            controls = null;
        }

        public void Configure(WorldTimeActivity activity)
        {
            worldTimeActivity = activity;
        }

#if UNITY_EDITOR
        public void SetValidationInputState(
            Vector2 move,
            bool deadlinePressed)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            DeadlinePressed = deadlinePressed;
            worldTimeActivity.SetMovement(Move.magnitude);
        }
#endif

        private void ResetState()
        {
            Move = Vector2.zero;
            PointerScreenPosition = Vector2.zero;
            FirePressed = false;
            FireHeld = false;
            ThrowPressed = false;
            DashPressed = false;
            DeadlinePressed = false;
            InteractPressed = false;
            RestartPressed = false;
            NextStagePressed = false;
            ReplayVisionTogglePressed = false;

            if (worldTimeActivity != null)
            {
                worldTimeActivity.SetMovement(0f);
            }
        }
    }
}
