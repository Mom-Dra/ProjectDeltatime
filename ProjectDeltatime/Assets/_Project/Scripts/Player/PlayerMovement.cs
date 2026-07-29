using Deltatime.InputSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerDash dash;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (input == null || health == null || dash == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} requires input, health, and dash references.",
                    this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive || dash.IsDashing)
            {
                return;
            }

            Vector3 displacement =
                new Vector3(input.Move.x, 0f, input.Move.y) *
                moveSpeed *
                UnityEngine.Time.fixedUnscaledDeltaTime;
            body.MovePosition(body.position + displacement);
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerHealth playerHealth,
            PlayerDash playerDash)
        {
            input = inputReader;
            health = playerHealth;
            dash = playerDash;
        }
    }
}
