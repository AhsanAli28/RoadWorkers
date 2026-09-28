using UnityEngine;

namespace Spirit604.Gameplay
{
    public abstract class HealthBase : HealthBehaviourBase
    {
        [SerializeField] private int maxHp = 4;

        private int currentHealth;

        public override int CurrentHealth { get => currentHealth; protected set => currentHealth = value; }

        public override int MaxHp => maxHp;

        protected virtual void Awake()
        {
        }

        protected virtual void OnEnable()
        {
            currentHealth = maxHp;
        }

        public override void Initialize(int value)
        {
            currentHealth = value;
        }
    }
}