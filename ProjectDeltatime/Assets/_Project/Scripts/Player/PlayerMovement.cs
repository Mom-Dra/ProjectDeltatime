using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-355)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.0001f)]
        private float minimumPhysicalDisplacement = 0.001f;

        private Rigidbody body;
        private NavMeshGroundMovement groundMovement;
        private Vector3 physicsStepStartPosition;
        private Vector3 physicsStepInputDirection;
        private Vector3 pendingRecoilDisplacement;
        private uint physicsStepVersion;
        private uint sampledPhysicsStepVersion;
        private bool physicsStepEligible;

        public bool IsPhysicallyMoving { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            groundMovement = GetComponent<NavMeshGroundMovement>();
            ResetPhysicalMovementSample();
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

        private void Update()
        {
            if (sampledPhysicsStepVersion == physicsStepVersion)
            {
                return;
            }

            sampledPhysicsStepVersion = physicsStepVersion;
            if (!physicsStepEligible)
            {
                IsPhysicallyMoving = false;
                return;
            }

            Vector3 displacement = body.position - physicsStepStartPosition;
            displacement.y = 0f;
            float directedDisplacement = Vector3.Dot(
                displacement,
                physicsStepInputDirection);
            IsPhysicallyMoving =
                directedDisplacement >= minimumPhysicalDisplacement;
        }

        private void FixedUpdate()
        {
            BeginPhysicalMovementSample();

            if (!health.IsAlive || worldTime.IsHardFrozen)
            {
                pendingRecoilDisplacement = Vector3.zero;
                StopPlanarMotion();
                return;
            }

            if (dash.IsDashing)
            {
                return;
            }

            Vector3 movementVelocity =
                new Vector3(input.Move.x, 0f, input.Move.y) * moveSpeed;
            if (movementVelocity.sqrMagnitude > 0.0001f)
            {
                physicsStepEligible = true;
                physicsStepInputDirection = movementVelocity.normalized;
            }

            Vector3 currentVelocity = body.linearVelocity;
            Vector3 recoilDisplacement = ConsumePendingRecoil();
            if (groundMovement != null)
            {
                groundMovement.TryMove(
                    body,
                    (movementVelocity *
                     UnityEngine.Time.fixedUnscaledDeltaTime) +
                    recoilDisplacement,
                    out _);
                body.linearVelocity = Vector3.zero;
                return;
            }

            if (recoilDisplacement.sqrMagnitude > 0.0000000001f)
            {
                body.MovePosition(body.position + recoilDisplacement);
            }

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

        public void QueueRecoil(Vector3 direction, float distance)
        {
            direction.y = 0f;
            if (distance <= 0f || direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            pendingRecoilDisplacement += direction.normalized * distance;
        }

        private void OnDisable()
        {
            ResetPhysicalMovementSample();
            StopPlanarMotion();
        }

        private void BeginPhysicalMovementSample()
        {
            physicsStepStartPosition = body.position;
            physicsStepInputDirection = Vector3.zero;
            physicsStepEligible = false;
            physicsStepVersion++;
        }

        private void ResetPhysicalMovementSample()
        {
            IsPhysicallyMoving = false;
            physicsStepStartPosition = body == null
                ? transform.position
                : body.position;
            physicsStepInputDirection = Vector3.zero;
            physicsStepEligible = false;
            physicsStepVersion = 0;
            sampledPhysicsStepVersion = 0;
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

        private Vector3 ConsumePendingRecoil()
        {
            Vector3 recoil = pendingRecoilDisplacement;
            pendingRecoilDisplacement = Vector3.zero;
            return recoil;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            minimumPhysicalDisplacement = Mathf.Max(
                0.0001f,
                minimumPhysicalDisplacement);
        }
    }
}
