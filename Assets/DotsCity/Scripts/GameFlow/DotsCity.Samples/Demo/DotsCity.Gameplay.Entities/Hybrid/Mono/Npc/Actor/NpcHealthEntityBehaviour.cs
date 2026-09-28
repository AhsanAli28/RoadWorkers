using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Gameplay;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public class NpcEntityHealthBehaviour : HealthBehaviourBase
    {
        protected IHybridEntityRef npcEntity;
        protected EntityManager entityManager;

        private INpcHitReaction npcHitReaction;
        private bool initializeOnSetup;
        private int tempHealth;

        public override int CurrentHealth
        {
            get
            {
                return HasEntity ? HealthComponent.Value : 0;
            }
            protected set
            {
                if (HasEntity)
                {
                    var healthComponent = HealthComponent;
                    healthComponent.Value = value;
                    UpdateHealthComponent(healthComponent);
                }
            }
        }

        public override int MaxHp => HasEntity ? HealthComponent.MaxValue : 0;

        private bool HasEntity => npcEntity.HasEntity;

        private HealthComponent HealthComponent => entityManager.GetComponentData<HealthComponent>(npcEntity.RelatedEntity);

        private void Awake()
        {
            npcEntity = GetComponent<IHybridEntityRef>();
            npcHitReaction = GetComponent<INpcHitReaction>();
            npcEntity.OnEntityInitialized += NpcBehaviourEntity_OnEntityInitialized;
        }

        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public override void Initialize(int initialHP)
        {
            if (HasEntity)
            {
                CurrentHealth = initialHP;
            }
            else
            {
                initializeOnSetup = true;
                tempHealth = initialHP;
            }
        }

        protected override void ActivateDeathVFX()
        {
            npcHitReaction?.ActivateDeathEffect(ForceDirection);

            entityManager.SetComponentEnabled<CopyTransformToGameObject>(npcEntity.RelatedEntity, false);
            entityManager.SetComponentEnabled<AliveTag>(npcEntity.RelatedEntity, false);

            entityManager.SetComponentData(npcEntity.RelatedEntity, new HealthComponent()
            {
                HitDirection = ForceDirection,
                HitPosition = transform.position,
                ForceMultiplier = 1f
            });

            entityManager.SetComponentData(npcEntity.RelatedEntity, new RagdollComponent()
            {
                Activated = true,
                ForceDirection = ForceDirection,
                ForceMultiplier = 1f,
                Position = transform.position,
                Rotation = transform.rotation
            });
        }

        protected override void HandleHitReaction(Vector3 point, Vector3 forceDirection)
        {
            npcHitReaction?.HandleHitReaction(point, forceDirection);
        }

        private void UpdateHealthComponent(HealthComponent healthComponent)
        {
            if (!HasEntity)
            {
                return;
            }

            healthComponent.HitDirection = ForceDirection;
            entityManager.SetComponentData(npcEntity.RelatedEntity, healthComponent);
        }

        private void NpcBehaviourEntity_OnEntityInitialized(Entity entity)
        {
            if (initializeOnSetup)
            {
                initializeOnSetup = false;
                Initialize(tempHealth);
            }
        }
    }
}
