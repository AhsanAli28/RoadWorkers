using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    /// <summary>
    /// The class that can help you implement your custom interaction with the traffic car & turn it into a player car if the cars have the same motion controllers.
    /// </summary>
    public class PlayerCustomInteractCarServiceBase : SingletonMonoBehaviour<PlayerCustomInteractCarServiceBase>
    {
        private enum GetCarComponentType { GetComponent, GetComponentInParent }

        [SerializeField] private bool addPlayerTag;

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public virtual void ConvertCarBeforeEnter(GameObject enteredCar, GameObject enteredNPC)
        {
            ConvertCar(enteredCar, enteredNPC);
        }

        public virtual void ExitCar(GameObject exitCar, GameObject enteredNPC)
        {
            ProcessExitCar(exitCar, enteredNPC);
        }

        /// <summary>
        /// Removes traffic car components & adds player car components.
        /// </summary>
        /// <param name="enteredCar"></param>
        /// <param name="enteredNPC"></param>       
        protected virtual bool ConvertCar(GameObject enteredCar, GameObject enteredNPC)
        {
            var adapter = enteredCar.GetComponent<CarEntityAdapter>();

            if (adapter != null)
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<MonoAdapterComponent>(adapter.RelatedEntity);
                adapter.Destroyed = true;
                adapter.DestroyEntityImmediate();
                Destroy(adapter);

                TryToDestroyComponent<PhysicsSwitcher>(enteredCar);
                TryToDestroyComponent<ScriptSwitcher>(enteredCar);

                var hybridEntityRuntime = enteredCar.AddComponent<HybridEntityRuntimeAuthoring>();

                hybridEntityRuntime.AddHybridComponent<CopyTransformFromGameObjectHybridComponent>();
                hybridEntityRuntime.AddHybridComponent<PlayerCarHybridComponent>();
                hybridEntityRuntime.AddHybridComponent<CustomRaycastTargetHybridComponent>();

                var velocityRuntime = enteredCar.AddComponent<VelocityRuntimeAuthoring>();
                velocityRuntime.Reset();

                var boundsRuntimeAuthoring = enteredCar.AddComponent<BoundsRuntimeAuthoring>();
                boundsRuntimeAuthoring.Reset();

                hybridEntityRuntime.ReinitEntity();

                InitCustomComponents(enteredCar);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Custom user logic to convert from traffic car to player car.
        /// </summary>
        protected virtual void InitCustomComponents(GameObject enteredCar) { }

        protected virtual void ProcessExitCar(GameObject exitCar, GameObject exitNpc)
        {
            AddToPlayerNpcTag(exitCar, exitNpc);
        }

        protected Entity GetEntity(GameObject go)
        {
            var entityRef = go.GetComponent<IHybridEntityRef>();

            if (entityRef != null)
            {
                return entityRef.RelatedEntity;
            }

            return Entity.Null;
        }

        protected void TryToDestroyComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out var comp))
            {
                Destroy(comp);
            }
        }

        private void AddToPlayerNpcTag(GameObject exitCar, GameObject exitNpc)
        {
            if (!addPlayerTag)
                return;

            var vehicleEntity = GetEntity(exitCar);

            if (vehicleEntity != Entity.Null)
            {
                if (EntityManager.HasComponent<PlayerTag>(vehicleEntity))
                {
                    EntityManager.RemoveComponent<PlayerTag>(vehicleEntity);
                }
            }

            var npcEntity = GetEntity(exitNpc);

            if (npcEntity != Entity.Null && !EntityManager.HasComponent<PlayerTag>(npcEntity))
            {
                EntityManager.AddComponent<PlayerTag>(npcEntity);
            }
        }
    }
}