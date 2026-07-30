using UnityEngine;

namespace Deltatime.Core
{
    public enum CombatFaction
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    public readonly struct DamageHit
    {
        public DamageHit(int damage, Vector3 point, Vector3 direction, GameObject source)
        {
            Damage = damage;
            Point = point;
            Direction = direction;
            Source = source;
        }

        public int Damage { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public GameObject Source { get; }
    }

    public readonly struct StunHit
    {
        public StunHit(
            float duration,
            Vector3 point,
            Vector3 direction,
            GameObject source)
        {
            Duration = Mathf.Max(0f, duration);
            Point = point;
            Direction = direction;
            Source = source;
        }

        public float Duration { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public GameObject Source { get; }
    }

    public interface IDamageable
    {
        CombatFaction Faction { get; }
        bool IsAlive { get; }
        void ReceiveHit(DamageHit hit);
    }

    public interface IStunnable
    {
        bool IsStunned { get; }
        void ReceiveStun(StunHit hit);
    }
}
