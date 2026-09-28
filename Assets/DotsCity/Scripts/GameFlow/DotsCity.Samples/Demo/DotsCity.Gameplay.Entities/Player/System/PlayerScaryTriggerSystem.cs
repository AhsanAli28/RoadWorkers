using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Gameplay.Weapons;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(BeginSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerScaryTriggerSystem : SystemBase
    {
        private const float SHOOTING_TRIGGER_LIFE_TIME = 2f;
        public const float SHOOTING_TRIGGER_SQ_DISTANCE = 144;
        public const float SCARY_WEAPON_TRIGGER_SQ_DISTANCE = 25f;

        private EntityQuery inputQuery;
        private EntityQuery scaryTriggerQuery;

        private PlayerSession playerSession;
        private Entity triggerEntity;
        private bool entityCreated;

        private bool triggerLocked;
        private float triggerUnlockTime;
        private bool shootingTriggerActivated;

        public event Action OnStartShooting = delegate { };

        protected override void OnCreate()
        {
            base.OnCreate();

            inputQuery = GetEntityQuery(
                ComponentType.ReadOnly<InputComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<PlayerTag>());

            scaryTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<TriggerComponent>());

            RequireForUpdate(inputQuery);

            Enabled = false;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            DestroyTriggerEntity();
        }

        protected override void OnUpdate()
        {
            if (inputQuery.CalculateEntityCount() != 1)
                return;

            if (triggerLocked && triggerUnlockTime > (float)SystemAPI.Time.ElapsedTime)
            {
                triggerLocked = false;
            }

            var playerEntity = inputQuery.GetSingletonEntity();
            InputComponent playerInputComponent = EntityManager.GetComponentData<InputComponent>(playerEntity);
            float3 playerPosition = EntityManager.GetComponentData<LocalTransform>(playerEntity).Position;

            var playerData = playerSession.CurrentSessionData;

            bool showWeaponTrigger = !playerData.WeaponIsHided;

            bool shootTrigger = !playerInputComponent.ShootDirection.Equals(float3.zero) &&
                playerData.CurrentSelectedPlayer.CurrentSelectedWeapon != WeaponType.Default;

            if (shootTrigger)
            {
                var entity = GetTriggerEntity();

                EntityManager.SetComponentData(entity, new TriggerComponent()
                {
                    Position = playerPosition,
                    TriggerDistanceSQ = SHOOTING_TRIGGER_SQ_DISTANCE,
                    TriggerAreaType = TriggerAreaType.FearPointTrigger
                });

                triggerLocked = true;
                triggerUnlockTime = (float)SystemAPI.Time.ElapsedTime + SHOOTING_TRIGGER_LIFE_TIME;

                if (!shootingTriggerActivated)
                {
                    shootingTriggerActivated = true;
                    OnStartShooting();
                }
            }
            else if (!triggerLocked)
            {
                if (showWeaponTrigger)
                {
                    var entity = GetTriggerEntity();

                    EntityManager.SetComponentData(entity, new TriggerComponent()
                    {
                        Position = playerPosition,
                        TriggerDistanceSQ = SCARY_WEAPON_TRIGGER_SQ_DISTANCE,
                        TriggerAreaType = TriggerAreaType.FearPointTrigger
                    });
                }
                else
                {
                    DestroyTriggerEntity();
                }
            }
        }

        private Entity GetTriggerEntity()
        {
            if (!entityCreated)
            {
                triggerEntity = EntityManager.CreateEntity(typeof(TriggerComponent));
                entityCreated = true;
            }

            return triggerEntity;
        }

        private void DestroyTriggerEntity()
        {
            if (entityCreated && EntityManager.Exists(triggerEntity))
            {
                EntityManager.DestroyEntity(triggerEntity);
                entityCreated = false;
            }
        }

        public void Initialize(PlayerSession playerSession)
        {
            this.playerSession = playerSession;
            Enabled = true;
        }
    }
}
