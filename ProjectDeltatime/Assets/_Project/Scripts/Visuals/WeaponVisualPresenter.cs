using Deltatime.Combat;
using Deltatime.Player;
using Deltatime.Replay;
using UnityEngine;

namespace Deltatime.Visuals
{
    /// <summary>
    /// Replaces the prototype cube with a weapon model beneath the humanoid
    /// right hand whenever the equipped definition supplies a custom held visual.
    /// Player firearms receive a late yaw correction on an intermediate pivot.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class WeaponVisualPresenter : MonoBehaviour
    {
        private const string AimPivotName = "Weapon Aim Pivot";
        private const string HeldModelName = "Held Weapon Model";
        private const string HeldMuzzleName = "Weapon Muzzle";
        private const int MaximumAimAlignmentPasses = 4;

        [SerializeField] private WeaponController weapon;
        [SerializeField] private CharacterAnimationController animationController;
        [SerializeField] private CharacterVisualController characterVisual;
        [SerializeField] private PlayerAim playerAim;
        [SerializeField] private PlayerDash playerDash;

        private GameObject displayedModel;
        private WeaponDefinition displayedDefinition;
        private Transform displayedHand;
        private Transform displayedAimPivot;
        private Transform displayedMuzzle;

        private void Awake()
        {
            CacheSources();
        }

        private void OnEnable()
        {
            if (weapon != null)
            {
                weapon.EquipmentChanged -= RefreshVisual;
                weapon.EquipmentChanged += RefreshVisual;
            }

            RefreshVisual();
        }

        private void LateUpdate()
        {
            // A character Animator may be configured after this component's
            // Awake call when scenes are rebuilt in the editor.
            if (displayedModel == null &&
                weapon != null &&
                weapon.Definition != null &&
                weapon.Definition.HasCustomHeldVisual)
            {
                RefreshVisual();
            }

            if (displayedAimPivot == null ||
                displayedModel == null ||
                displayedDefinition == null)
            {
                return;
            }

            ResetAimPivot();
            if (ShouldAlignPlayerFirearm())
            {
                AlignMuzzleWithPlayerAim();
            }
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.EquipmentChanged -= RefreshVisual;
            }
        }

        private void OnDestroy()
        {
            RemoveModel();
        }

        public void RefreshVisual()
        {
            CacheSources();
            WeaponDefinition definition = weapon == null
                ? null
                : weapon.Definition;
            if (definition == null || !definition.HasCustomHeldVisual)
            {
                RemoveModel();
                return;
            }

            Transform hand = ResolveRightHand();
            if (hand == null)
            {
                RemoveModel();
                return;
            }

            if (displayedModel != null &&
                displayedDefinition == definition &&
                displayedHand == hand)
            {
                ApplyModelTransforms(definition);
                weapon.SetCustomHeldVisualActive(true);
                return;
            }

            RemoveModel();
            displayedAimPivot = CreateAimPivot(hand);
            displayedModel = Instantiate(
                definition.HeldVisualPrefab,
                displayedAimPivot,
                false);
            displayedModel.name = HeldModelName;
            displayedDefinition = definition;
            displayedHand = hand;
            ApplyModelTransforms(definition);
            weapon.SetCustomHeldVisualActive(true);
            characterVisual?.RefreshRenderers();
            ReplayVisualRegistry.Active?.RegisterRendererHierarchy(
                displayedModel.transform);
        }

        private void ApplyModelTransforms(WeaponDefinition definition)
        {
            if (displayedModel == null || definition == null)
            {
                return;
            }

            Transform modelTransform = displayedModel.transform;
            modelTransform.localPosition = definition.HeldModelLocalPosition;
            modelTransform.localRotation = Quaternion.Euler(
                definition.HeldModelLocalEulerAngles);
            modelTransform.localScale = definition.HeldModelLocalScale;

            if (displayedMuzzle == null)
            {
                Transform existing = modelTransform.Find(HeldMuzzleName);
                displayedMuzzle = existing;
                if (displayedMuzzle == null)
                {
                    GameObject muzzleObject = new GameObject(HeldMuzzleName);
                    displayedMuzzle = muzzleObject.transform;
                    displayedMuzzle.SetParent(modelTransform, false);
                }
            }

            displayedMuzzle.localPosition = definition.HeldMuzzleLocalPosition;
            displayedMuzzle.localRotation = Quaternion.Euler(
                definition.HeldMuzzleLocalEulerAngles);
            weapon?.SetCustomHeldMuzzle(displayedMuzzle);

            ResetAimPivot();
            if (ShouldAlignPlayerFirearm())
            {
                AlignMuzzleWithPlayerAim();
            }
        }

        private void RemoveModel()
        {
            if (displayedAimPivot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(displayedAimPivot.gameObject);
                }
                else
                {
                    DestroyImmediate(displayedAimPivot.gameObject);
                }
            }
            else if (displayedModel != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(displayedModel);
                }
                else
                {
                    DestroyImmediate(displayedModel);
                }
            }

            displayedModel = null;
            displayedDefinition = null;
            displayedHand = null;
            displayedAimPivot = null;
            displayedMuzzle = null;
            if (weapon != null)
            {
                weapon.SetCustomHeldMuzzle(null);
                weapon.SetCustomHeldVisualActive(false);
            }

            characterVisual?.RefreshRenderers();
        }

        private void CacheSources()
        {
            if (weapon == null)
            {
                weapon = GetComponent<WeaponController>();
            }

            if (animationController == null)
            {
                animationController =
                    GetComponent<CharacterAnimationController>();
            }

            if (characterVisual == null)
            {
                characterVisual =
                    GetComponent<CharacterVisualController>();
            }

            if (playerAim == null)
            {
                playerAim = GetComponent<PlayerAim>();
            }

            if (playerDash == null)
            {
                playerDash = GetComponent<PlayerDash>();
            }
        }

        private static Transform CreateAimPivot(Transform hand)
        {
            GameObject pivotObject = new GameObject(AimPivotName);
            Transform pivot = pivotObject.transform;
            pivot.SetParent(hand, false);
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            return pivot;
        }

        private void ResetAimPivot()
        {
            if (displayedAimPivot == null)
            {
                return;
            }

            displayedAimPivot.localPosition = Vector3.zero;
            displayedAimPivot.localRotation = Quaternion.identity;
            displayedAimPivot.localScale = Vector3.one;
        }

        private bool ShouldAlignPlayerFirearm()
        {
            return playerAim != null &&
                   (playerDash == null || !playerDash.IsDashing) &&
                   displayedDefinition != null &&
                   displayedDefinition.IsFirearm &&
                   displayedMuzzle != null;
        }

        private void AlignMuzzleWithPlayerAim()
        {
            // The pivot rotates about the hand, so repeated passes compensate
            // for the small local hand-to-model position offset.
            for (int pass = 0; pass < MaximumAimAlignmentPasses; pass++)
            {
                Vector3 muzzleForward = Vector3.ProjectOnPlane(
                    displayedMuzzle.forward,
                    Vector3.up);
                Vector3 targetDirection = playerAim.GetPlanarDirectionFrom(
                    displayedMuzzle.position);
                if (muzzleForward.sqrMagnitude <= 0.000001f ||
                    targetDirection.sqrMagnitude <= 0.000001f)
                {
                    return;
                }

                float yawError = Vector3.SignedAngle(
                    muzzleForward.normalized,
                    targetDirection.normalized,
                    Vector3.up);
                if (Mathf.Abs(yawError) <= 0.001f)
                {
                    return;
                }

                displayedAimPivot.rotation = Quaternion.AngleAxis(
                    yawError,
                    Vector3.up) * displayedAimPivot.rotation;
            }
        }

        private Transform ResolveRightHand()
        {
            Animator animator = animationController == null
                ? GetComponentInChildren<Animator>(true)
                : animationController.Animator;
            return animator != null &&
                   animator.avatar != null &&
                   animator.avatar.isValid &&
                   animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
        }

        private void OnDrawGizmosSelected()
        {
            if (displayedMuzzle == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(displayedMuzzle.position, 0.025f);
            Gizmos.DrawRay(displayedMuzzle.position,
                displayedMuzzle.forward * 0.28f);
        }
    }
}
