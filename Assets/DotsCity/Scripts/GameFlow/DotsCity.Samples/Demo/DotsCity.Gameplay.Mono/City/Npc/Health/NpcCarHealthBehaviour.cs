using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public class NpcCarHealthBehaviour : MonoBehaviour, IHealth
    {
        private int health;

        public int CurrentHealth
        {
            get
            {
                return health;
            }
        }

        public bool IsAlive => health > 0;

        public void Initialize(int newHealth)
        {
            health = newHealth;
        }

        public void TakeDamage(int damage)
        {
            health -= damage;

            if (health <= 0)
            {

            }
        }

        public void TakeDamage(int damage, Vector3 pos, Vector3 hitDirection)
        {
            TakeDamage(damage);
        }
    }
}