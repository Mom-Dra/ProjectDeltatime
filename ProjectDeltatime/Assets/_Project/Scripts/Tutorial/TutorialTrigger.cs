using Deltatime.Player;
using UnityEngine;

namespace Deltatime.Tutorial
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialTrigger : MonoBehaviour
    {
        public enum TriggerKind
        {
            DashExit,
            DeadlineEntry,
            TutorialExit
        }

        [SerializeField] private TutorialDirector director;
        [SerializeField] private TriggerKind kind;

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (director == null ||
                other.GetComponentInParent<PlayerHealth>() == null)
            {
                return;
            }

            director.NotifyTrigger(kind);
        }

        public void Configure(
            TutorialDirector tutorialDirector,
            TriggerKind triggerKind)
        {
            director = tutorialDirector;
            kind = triggerKind;
        }
    }
}
