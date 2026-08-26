using Deltatime.InputSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 13.5f, -12.5f);
        [SerializeField] private Vector3 cameraFocusOffset;
        [SerializeField, Min(0f)] private float aimLeadDistance = 2.25f;
        [SerializeField, Min(0.01f)] private float followSharpness = 8f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 12f;
        [SerializeField, Min(0f)] private float lookHeight = 0.55f;
        [SerializeField, Min(0f)] private float impulseMultiplier = 1f;
        [SerializeField, Min(0f)] private float maximumPositionImpulse = 0.16f;
        [SerializeField, Min(0f)] private float maximumRotationImpulse = 0.5f;
        [SerializeField] private bool constrainToBounds;
        [SerializeField] private Bounds cameraBounds =
            new Bounds(Vector3.zero, new Vector3(20f, 0f, 20f));

        // The NavMesh edge is a movement boundary rather than a camera target.
        // Correct only when the target would leave the projected viewport;
        // ordinary edge framing keeps the full NavMesh footprint constrained.
        private const float TargetViewportMargin = 0f;
        private const float ElevationAwareBoundsHeight = 1f;

        private Camera gameplayCamera;
        private bool initialized;
        private bool basePoseInitialized;
        private Vector3 smoothedBasePosition;
        private Quaternion smoothedBaseRotation;
        private float impulseTimeRemaining;
        private float impulseDuration;
        private float impulsePositionAmplitude;
        private float impulseRotationAmplitude;
        private int impulseSequence;

        public bool IsImpulseActive => impulseTimeRemaining > 0f;
        public int ImpulsePlayCount { get; private set; }

        private void Awake()
        {
            gameplayCamera = GetComponent<Camera>();
            ValidateConfiguration();
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!initialized || target == null)
            {
                return;
            }

            Vector3 focus = CalculateDesiredFocus();
            Vector3 desiredPosition = focus + cameraOffset;
            Vector3 lookPoint = focus + (Vector3.up * lookHeight);
            Quaternion desiredRotation = Quaternion.LookRotation(
                lookPoint - desiredPosition,
                Vector3.up);
            if (!basePoseInitialized)
            {
                smoothedBasePosition = desiredPosition;
                smoothedBaseRotation = desiredRotation;
                basePoseInitialized = true;
            }

            float positionBlend =
                1f - Mathf.Exp(-followSharpness * UnityEngine.Time.unscaledDeltaTime);
            smoothedBasePosition = Vector3.Lerp(
                smoothedBasePosition,
                desiredPosition,
                positionBlend);

            desiredRotation = Quaternion.LookRotation(
                lookPoint - smoothedBasePosition,
                Vector3.up);
            float rotationBlend =
                1f - Mathf.Exp(-rotationSharpness * UnityEngine.Time.unscaledDeltaTime);
            smoothedBaseRotation = Quaternion.Slerp(
                smoothedBaseRotation,
                desiredRotation,
                rotationBlend);
            ApplyImpulse();
        }

        public void Configure(
            Transform followTarget,
            PlayerAim playerAim,
            PlayerInputReader inputReader)
        {
            target = followTarget;
            aim = playerAim;
            input = inputReader;
            initialized = target != null;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 focus = CalculateDesiredFocus();
            smoothedBasePosition = focus + cameraOffset;
            smoothedBaseRotation = Quaternion.LookRotation(
                focus + (Vector3.up * lookHeight) - smoothedBasePosition,
                Vector3.up);
            transform.SetPositionAndRotation(
                smoothedBasePosition,
                smoothedBaseRotation);
            basePoseInitialized = true;
            initialized = true;
        }

        public void AddImpulse(
            float positionAmplitude,
            float rotationAmplitude,
            float duration)
        {
            float scaledPosition = Mathf.Max(0f, positionAmplitude) *
                impulseMultiplier;
            float scaledRotation = Mathf.Max(0f, rotationAmplitude) *
                impulseMultiplier;
            float safeDuration = Mathf.Max(0f, duration);
            if (safeDuration <= 0f ||
                (scaledPosition <= 0f && scaledRotation <= 0f))
            {
                return;
            }

            if (impulseTimeRemaining <= 0f)
            {
                impulsePositionAmplitude = 0f;
                impulseRotationAmplitude = 0f;
                impulseDuration = 0f;
            }

            impulsePositionAmplitude = Mathf.Clamp(
                Mathf.Max(impulsePositionAmplitude, scaledPosition),
                0f,
                maximumPositionImpulse);
            impulseRotationAmplitude = Mathf.Clamp(
                Mathf.Max(impulseRotationAmplitude, scaledRotation),
                0f,
                maximumRotationImpulse);
            impulseDuration = Mathf.Max(impulseDuration, safeDuration);
            impulseTimeRemaining = Mathf.Max(
                impulseTimeRemaining,
                safeDuration);
            impulseSequence++;
            ImpulsePlayCount++;
        }

        private void ApplyImpulse()
        {
            if (impulseTimeRemaining <= 0f || impulseDuration <= 0f)
            {
                transform.SetPositionAndRotation(
                    smoothedBasePosition,
                    smoothedBaseRotation);
                impulsePositionAmplitude = 0f;
                impulseRotationAmplitude = 0f;
                impulseDuration = 0f;
                impulseTimeRemaining = 0f;
                return;
            }

            float envelope = Mathf.Clamp01(
                impulseTimeRemaining / impulseDuration);
            envelope *= envelope;
            float phase = UnityEngine.Time.unscaledTime * 37f +
                impulseSequence * 1.618f;
            float horizontal = Mathf.Sin(phase * 1.17f);
            float vertical = Mathf.Sin(phase * 1.73f + 1.2f);
            float roll = Mathf.Sin(phase * 1.31f + 0.7f);
            Vector3 localOffset = new Vector3(
                horizontal,
                vertical * 0.55f,
                0f) * (impulsePositionAmplitude * envelope);
            Quaternion localRotation = Quaternion.Euler(
                vertical * impulseRotationAmplitude * envelope * 0.25f,
                horizontal * impulseRotationAmplitude * envelope * 0.2f,
                roll * impulseRotationAmplitude * envelope);
            transform.SetPositionAndRotation(
                smoothedBasePosition + smoothedBaseRotation * localOffset,
                smoothedBaseRotation * localRotation);
            impulseTimeRemaining = Mathf.Max(
                0f,
                impulseTimeRemaining - UnityEngine.Time.unscaledDeltaTime);
        }

        private Vector3 CalculateDesiredFocus()
        {
            Vector3 aimLead = aim == null
                ? Vector3.zero
                : aim.AimDirection * aimLeadDistance;
            return ConstrainFocus(
                target.position + cameraFocusOffset + aimLead);
        }

        private Vector3 ConstrainFocus(Vector3 focus)
        {
            if (!constrainToBounds ||
                cameraBounds.size.x <= Mathf.Epsilon ||
                cameraBounds.size.z <= Mathf.Epsilon)
            {
                return focus;
            }

            Camera camera = gameplayCamera != null
                ? gameplayCamera
                : GetComponent<Camera>();
            if (camera == null ||
                !TryCalculateGroundFootprint(
                    camera,
                    focus,
                    out Vector2 minimumOffset,
                    out Vector2 maximumOffset))
            {
                focus.x = Mathf.Clamp(
                    focus.x,
                    cameraBounds.min.x,
                    cameraBounds.max.x);
                focus.z = Mathf.Clamp(
                    focus.z,
                    cameraBounds.min.z,
                    cameraBounds.max.z);
                return focus;
            }

            focus.x = ClampFocusAxis(
                focus.x,
                cameraBounds.min.x - minimumOffset.x,
                cameraBounds.max.x - maximumOffset.x);
            focus.z = ClampFocusAxis(
                focus.z,
                cameraBounds.min.z - minimumOffset.y,
                cameraBounds.max.z - maximumOffset.y);

            // When the full viewport is wider than the map (Stage5's compact
            // hall), the map is deliberately centered and has no horizontal
            // spare range for a target correction. Larger combat regions such
            // as Stage6 retain enough width to trim edge aim lead safely.
            if (cameraBounds.size.x >
                (maximumOffset.x - minimumOffset.x) + Mathf.Epsilon)
            {
                KeepTargetHorizontallyVisible(ref focus);
            }

            return focus;
        }

        private void KeepTargetHorizontallyVisible(ref Vector3 focus)
        {
            if (target == null)
            {
                return;
            }

            Camera camera = gameplayCamera != null
                ? gameplayCamera
                : GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }

            Vector3 cameraPosition = focus + cameraOffset;
            Vector3 lookPoint = focus + (Vector3.up * lookHeight);
            Vector3 lookDirection = lookPoint - cameraPosition;
            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            Vector3 targetInCameraSpace = Quaternion.Inverse(rotation) *
                (target.position - cameraPosition);
            if (targetInCameraSpace.z <= Mathf.Epsilon)
            {
                return;
            }

            float aspect = camera.aspect > Mathf.Epsilon
                ? camera.aspect
                : 16f / 9f;
            float horizontalTangent = Mathf.Tan(
                camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect;
            float safeHorizontalScale = 1f - (TargetViewportMargin * 2f);
            if (horizontalTangent <= Mathf.Epsilon ||
                safeHorizontalScale <= Mathf.Epsilon)
            {
                return;
            }

            float requiredDepth = Mathf.Abs(targetInCameraSpace.x) /
                (horizontalTangent * safeHorizontalScale);
            if (requiredDepth <= targetInCameraSpace.z)
            {
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(
                rotation * Vector3.forward,
                Vector3.up);
            float flatForwardMagnitude = flatForward.magnitude;
            if (flatForwardMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            // Moving the focus backward along the viewing direction increases
            // the target's screen-space depth without shifting the visible map
            // sideways. This trims edge aim lead instead of letting it push the
            // player out of the horizontal viewport.
            focus -= flatForward / flatForwardMagnitude *
                ((requiredDepth - targetInCameraSpace.z) / flatForwardMagnitude);
        }

        private bool TryCalculateGroundFootprint(
            Camera camera,
            Vector3 focus,
            out Vector2 minimumOffset,
            out Vector2 maximumOffset)
        {
            minimumOffset = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            maximumOffset = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            Vector3 cameraPosition = focus + cameraOffset;
            Vector3 lookPoint = focus + (Vector3.up * lookHeight);
            Vector3 lookDirection = lookPoint - cameraPosition;
            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Quaternion rotation = Quaternion.LookRotation(
                lookDirection,
                Vector3.up);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            float verticalTangent = Mathf.Tan(
                camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspect = camera.aspect > Mathf.Epsilon
                ? camera.aspect
                : 16f / 9f;
            float horizontalTangent = verticalTangent * aspect;

            for (int y = -1; y <= 1; y += 2)
            {
                for (int x = -1; x <= 1; x += 2)
                {
                    Vector3 direction =
                        forward +
                        right * (x * horizontalTangent) +
                        up * (y * verticalTangent);
                    if (direction.y >= -Mathf.Epsilon)
                    {
                        return false;
                    }

                    // The camera follows actors over large Stage6 elevation
                    // changes. Project its footprint onto the active focus
                    // height there; a high platform otherwise clamps XZ as if
                    // the player were still on the lower floor. Compact Stage5
                    // keeps its shared NavMesh plane for stable edge framing.
                    float footprintHeight = cameraBounds.size.y >=
                        ElevationAwareBoundsHeight
                        ? focus.y
                        : cameraBounds.center.y;
                    float distance =
                        (footprintHeight - cameraPosition.y) / direction.y;
                    if (distance <= 0f)
                    {
                        return false;
                    }

                    Vector3 groundPoint = cameraPosition + direction * distance;
                    Vector3 offset = groundPoint - focus;
                    minimumOffset = Vector2.Min(
                        minimumOffset,
                        new Vector2(offset.x, offset.z));
                    maximumOffset = Vector2.Max(
                        maximumOffset,
                        new Vector2(offset.x, offset.z));
                }
            }

            return true;
        }

        private static float ClampFocusAxis(
            float value,
            float minimum,
            float maximum)
        {
            return minimum <= maximum
                ? Mathf.Clamp(value, minimum, maximum)
                : (minimum + maximum) * 0.5f;
        }

        private void ValidateConfiguration()
        {
            if (target == null && !initialized)
            {
                return;
            }

            if (target == null || aim == null || input == null)
            {
                Debug.LogError(
                    $"{nameof(TopDownCameraController)} requires a target, aim, and input.",
                    this);
                enabled = false;
            }
        }

        private void OnValidate()
        {
            aimLeadDistance = Mathf.Max(0f, aimLeadDistance);
            followSharpness = Mathf.Max(0.01f, followSharpness);
            rotationSharpness = Mathf.Max(0.01f, rotationSharpness);
            impulseMultiplier = Mathf.Max(0f, impulseMultiplier);
            maximumPositionImpulse = Mathf.Max(0f, maximumPositionImpulse);
            maximumRotationImpulse = Mathf.Max(0f, maximumRotationImpulse);
        }
    }
}
