namespace Spirit604.DotsCity.Gameplay.Npc
{
    public class EnemyNpcBehaviourEntity : NpcBehaviourEntity
    {
        private bool inCombat;

        public bool InCombat
        {
            get
            {
                return inCombat;
            }

            set
            {
                inCombat = value;

                WeaponHolder.SwitchVisibleState(inCombat);

                if (HasEntity)
                {
                    if (InCombat)
                    {
                        entityManager.AddComponent(RelatedEntity, typeof(EnemyNpcCombatStateTag));
                    }
                    else
                    {
                        entityManager.SetComponentData(RelatedEntity, new NpcTargetComponent() { HasShootingTarget = false });

                        if (entityManager.HasComponent<EnemyNpcCombatStateTag>(RelatedEntity))
                        {
                            entityManager.RemoveComponent(RelatedEntity, typeof(EnemyNpcCombatStateTag));
                        }
                    }
                }
            }
        }

        public bool ShouldMoving { get; set; }

        protected override void Awake()
        {
            base.Awake();
            InCombat = false;
        }

        // Update is called once per frame
        protected override void Update()
        {
            if (!inCombat && !ShouldMoving)
            {
                return;
            }

            if (!ShouldMoving && !WeaponHolder.IsHided && animator.GetFloat(weaponIdKeyId) != WeaponHolder.CurrentAnimatorWeaponID)
            {
                SetAnimatorWeaponIndex();
            }

            base.Update();
        }
    }
}