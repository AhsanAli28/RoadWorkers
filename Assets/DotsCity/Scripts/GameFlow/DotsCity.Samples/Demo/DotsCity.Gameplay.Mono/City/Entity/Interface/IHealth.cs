using UnityEngine;

namespace Spirit604.Gameplay
{
    public interface IHealth
    {
        bool IsAlive { get; }
        int CurrentHealth { get; }
        void TakeDamage(int damage);
        void TakeDamage(int damage, Vector3 position, Vector3 hitDirection);
        void Initialize(int initialHP);
    }
}