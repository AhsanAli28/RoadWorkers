using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public class NpcBehaviourEntity : NpcBehaviourBase, INpcEntity
    {
        [SerializeField] private NpcEntityShapeType npcType;

        private NpcWeaponHolder weaponHolder;
        private INpcHitReaction npcHitReaction;

        private Entity relatedEntity;
        private bool isInitialized;

        protected NavMeshAgent navMeshAgent;
        protected EntityManager entityManager;

        public Entity RelatedEntity { get => relatedEntity; set => relatedEntity = value; }
        public NpcEntityShapeType NpcShapeType { get => npcType; set => npcType = value; }
        public bool HasEntity => isInitialized && entityManager.Exists(RelatedEntity);
        protected override bool ShouldAimingAlways => false;

        public event Action<Entity> OnEntityInitialized = delegate { };
        public event Action<INpcEntity> OnDisableCallback = delegate { };

        protected override void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();

            if (navMeshAgent)
            {
                navMeshAgent.updatePosition = false;
                navMeshAgent.updateRotation = false;
                navMeshAgent.updateUpAxis = false;
            }

            weaponHolder = GetComponent<NpcWeaponHolder>();
            npcHitReaction = GetComponent<INpcHitReaction>();

            if (weaponHolder)
            {
                weaponHolder.OnSelectWeapon += WeaponHolder_OnSelectWeapon;
            }

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            npcHitReaction.OnDeathEffectFinished += NpcHitReactionBehaviour_OnDeathEffectFinished;
            CanControl = true;
        }

        private void OnDisable()
        {
            isInitialized = false;
            OnDisableCallback(this);
        }

        private void OnDestroy()
        {
            isInitialized = false;
            OnDisableCallback(this);
        }

        protected virtual void Update()
        {
            if (!IsAlive)
                return;

            var shootDirection = entityManager.GetComponentData<InputComponent>(RelatedEntity).ShootDirection;

            Shoot(shootDirection);
        }

        public override void Initialize(Vector3 spawnPosition, int localSpawnIndex = -1)
        {
            isInitialized = true;

            if (navMeshAgent)
            {
                navMeshAgent.Warp(spawnPosition);
            }

            if (localSpawnIndex != -1)
            {
                //entityManager.SetComponentData(RelatedEntity, new PlayerMobNpcComponent() { SideIndex = localSpawnIndex });
            }

            OnEntityInitialized(RelatedEntity);
        }

        // Animator event
        public override void Landed()
        {
            var animatorStateComponent = entityManager.GetComponentData<AnimatorStateComponent>(RelatedEntity);
            animatorStateComponent.IsLanded = true;
            entityManager.SetComponentData(RelatedEntity, animatorStateComponent);
        }

        public void Initialize()
        {
            if (isInitialized)
                entityManager.SetComponentData(RelatedEntity, new NpcCombatStateComponent() { ReducationFactor = this.ReducationFactor });
        }

        public void SetCustomTarget(Vector3 targetPosition, Quaternion targetRotation)
        {
            entityManager.AddComponent(RelatedEntity, typeof(NpcInitializeCustomDestinationTag));

            entityManager.AddComponentData(RelatedEntity,
                new NpcCustomDestinationComponent()
                {
                    Destination = targetPosition,
                    DstRotation = targetRotation
                });
        }

        public void ResetCustomTarget()
        {
            entityManager.AddComponent(RelatedEntity, typeof(ResetNpcCustomDestinationTag));
        }

        public override void DestroyEntity()
        {
            isInitialized = false;

            if (entityManager.Exists(RelatedEntity))
            {
                PoolEntityUtils.DestroyEntity(ref entityManager, RelatedEntity);
            }
        }

        public void WeaponHolder_OnSelectWeapon(NpcWeaponHolder npcWeaponHolder, WeaponType weaponType)
        {
            Initialize();
        }

        private void NpcHitReactionBehaviour_OnDeathEffectFinished()
        {
            DestroyEntity();
        }
    }
}