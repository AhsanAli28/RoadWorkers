using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#if !DOTS_SIMULATION
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
#endif

namespace Spirit604.DotsCity.Debug
{
    public class ScaryTriggerDebugger : MonoBehaviourBase
    {
        private enum ScaryTriggerMode
        {
            Default,
#if !DOTS_SIMULATION
            PlayerImitation
#endif
        }

        [SerializeField]
        private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField]
        private ScaryTriggerMode scaryTriggerMode;

        [ShowIf(nameof(enableDebug))]
        [SerializeField]
        private bool showScaryPedestrians;

        [ShowIf(nameof(showScaryPedestrians))]
        [SerializeField]
        private Color scaryPedestrianColor = Color.magenta;

        [ShowIf(nameof(showScaryPedestrians))]
        [SerializeField]
        [Range(0.1f, 5f)]
        private float circleRadius = 1f;

        [ShowIf(nameof(DefaultDebugMode))]
        [SerializeField]
        [Range(0, 30f)]
        private float triggerDistance = 5f;

        [ShowIf(nameof(DefaultDebugMode))]
        [SerializeField]
        [Range(0, 30f)]
        private float triggerLifeTime = 0.4f;

        [ShowIf(nameof(PlayerImititation))]
        [SerializeField]
        private bool imitateWeaponShowing;

        [ShowIf(nameof(PlayerImititation))]
        [SerializeField]
        private bool imitateShotOfWeapon;

        private EntityManager entityManager;
        private EntityQuery pedestrianQuery;
        private Entity fakePlayerEntity;
        private bool initialized;

        private bool DefaultDebugMode => enableDebug && scaryTriggerMode == ScaryTriggerMode.Default;

#if DOTS_SIMULATION
        private bool PlayerImititation => false;
#else

        private bool PlayerImititation => enableDebug && scaryTriggerMode == ScaryTriggerMode.PlayerImitation;

        private PlayerSession playerSession;

        [InjectWrapper]
        public void Construct(PlayerSession playerSession = null)
        {
            this.playerSession = playerSession;
        }
#endif

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            pedestrianQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<ScaryRunningTag>());

#if !DOTS_SIMULATION
#if !ZENJECT
            Construct(ObjectUtils.FindObjectOfType<PlayerSession>());
#endif
#endif
        }

        [Button]
        public void ResetToCenterOfTheScene()
        {
            transform.position = VectorExtensions.GetCenterOfSceneView();
        }

        [ShowIf(nameof(DefaultDebugMode))]
        [Button]
        public void Create()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var scaryTriggerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<NpcDeathEventConsumerSystem>();

            if (scaryTriggerSystem == null) return;
            var lifeTime = scaryTriggerSystem.GetLifeTime(triggerLifeTime);
            CreateScaryTriggerEntity(transform.position, triggerDistance * triggerDistance, lifeTime, TriggerAreaType.FearPointTrigger);
        }

        public Entity CreateScaryTriggerEntity(float3 position, float distanceSq, float disableTime, TriggerAreaType pedestrianAreaTriggerType)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var triggerEntity = entityManager.CreateEntity(typeof(TriggerComponent), typeof(LifeTimeComponent));

            entityManager.SetComponentData(triggerEntity, new
                TriggerComponent()
            {
                Position = position,
                TriggerDistanceSQ = distanceSq,
                TriggerAreaType = pedestrianAreaTriggerType
            });

            entityManager.SetComponentData(triggerEntity, new
               LifeTimeComponent()
            {
                DestroyTimeStamp = disableTime
            });

            return triggerEntity;
        }

        private void OnDrawGizmosSelected()
        {
            if (!enableDebug)
            {
                return;
            }

            switch (scaryTriggerMode)
            {
                case ScaryTriggerMode.Default:
                    {
                        Gizmos.DrawWireSphere(transform.position, triggerDistance);

                        break;
                    }
#if !DOTS_SIMULATION
                case ScaryTriggerMode.PlayerImitation:
                    {
                        if (imitateShotOfWeapon || imitateWeaponShowing)
                        {
                            float gizmosRadiusSQ = imitateShotOfWeapon ?
                              PlayerScaryTriggerSystem.SHOOTING_TRIGGER_SQ_DISTANCE :
                              PlayerScaryTriggerSystem.SCARY_WEAPON_TRIGGER_SQ_DISTANCE;

                            float gizmosRadius = Mathf.Sqrt(gizmosRadiusSQ);

                            Gizmos.DrawWireSphere(transform.position, gizmosRadius);
                        }

                        break;
                    }
#endif
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (showScaryPedestrians)
            {
                var oldColor = Gizmos.color;

                Gizmos.color = scaryPedestrianColor;

                var pedestrians = pedestrianQuery.ToComponentDataArray<LocalToWorld>(Unity.Collections.Allocator.TempJob);

                for (int i = 0; i < pedestrians.Length; i++)
                {
                    Gizmos.DrawWireSphere(pedestrians[i].Position, circleRadius);
                }

                pedestrians.Dispose();

                Gizmos.color = oldColor;
            }

#if !DOTS_SIMULATION
            if (scaryTriggerMode == ScaryTriggerMode.PlayerImitation)
            {
                if (playerSession)
                    playerSession.CurrentSessionData.WeaponIsHided = !imitateWeaponShowing;

                Initialize();
                entityManager.SetComponentData(fakePlayerEntity, LocalTransform.FromPosition(transform.position));

                Vector3 shootDirection = imitateShotOfWeapon ? Vector3.one : Vector3.zero;

                entityManager.SetComponentData(fakePlayerEntity, new InputComponent() { ShootDirection = shootDirection });
            }
#endif
        }

        private void Initialize()
        {
            if (!initialized)
            {
                initialized = true;

#if !DOTS_SIMULATION
                var playerQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InputComponent>(),
                    ComponentType.ReadOnly<PlayerTag>(),
                    ComponentType.ReadOnly<LocalToWorld>());

                if (playerQuery.CalculateEntityCount() == 0)
                {
                    fakePlayerEntity = entityManager.CreateEntity(
                         typeof(InputComponent),
                         typeof(LocalToWorld),
                         typeof(PlayerTag));
                }
                else
                {
                    fakePlayerEntity = playerQuery.GetSingletonEntity();
                }
#endif
            }
        }
    }
}
