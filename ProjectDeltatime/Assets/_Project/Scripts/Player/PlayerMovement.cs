using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (input == null ||
                health == null ||
                dash == null ||
                worldTime == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} requires input, health, dash, and world-time references.",
                    this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive || worldTime.IsHardFrozen)
            {
                StopPlanarMotion();
                return;
            }

            if (dash.IsDashing)
            {
                return;
            }

            Vector3 movementVelocity =
                new Vector3(input.Move.x, 0f, input.Move.y) * moveSpeed;
            Vector3 currentVelocity = body.linearVelocity;
            body.linearVelocity = new Vector3(
                movementVelocity.x,
                currentVelocity.y,
                movementVelocity.z);
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerHealth playerHealth,
            PlayerDash playerDash,
            WorldTimeController timeSource)
        {
            input = inputReader;
            health = playerHealth;
            dash = playerDash;
            worldTime = timeSource;
        }

        private void OnDisable()
        {
            StopPlanarMotion();
        }

        private void StopPlanarMotion()
        {
            if (body == null)
            {
                return;
            }

            Vector3 currentVelocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }
    }
}
