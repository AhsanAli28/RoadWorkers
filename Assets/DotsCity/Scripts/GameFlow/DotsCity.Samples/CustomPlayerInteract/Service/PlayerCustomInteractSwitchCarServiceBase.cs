using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    /// <summary>
    /// The class that can help you implement your custom interaction with the traffic car & turn it into a player car if the cars have different motion controllers.
    /// </summary>
    public abstract class PlayerCustomInteractSwitchCarServiceBase : SingletonMonoBehaviour<PlayerCustomInteractSwitchCarServiceBase>
    {
        [Tooltip("Copy velocity from traffic car to player car after conversion when player enters car")]
        [SerializeField] private bool copyVelocity = true;

        [Tooltip("Add 'PlayerTag' to player car & remove from player NPC when player enters the car and vice versa")]
        [SerializeField] private bool addPlayerTag;

        [SerializeField] private float yOffset;

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        /// <summary>
        /// Implement your factory method to get the player car by CarModel ID.
        /// </summary>
        /// <param name="carModel">CarModel ID is taken from the entered traffic car.</param>
        /// <returns></returns>
        protected abstract GameObject GetPlayerCar(int carModel);

        public virtual GameObject ConvertCarBeforeEnter(GameObject enteredTrafficCar, GameObject enteredNPCObj)
        {
            var trafficVehicleRef = enteredTrafficCar.GetComponent<IHybridEntityRef>();
            int carModel = 0;

            if (trafficVehicleRef != null)
            {
                if (!EntityManager.HasComponent<TrafficTag>(trafficVehicleRef.RelatedEntity))
                {
                    AddPlayerTagToCar(enteredNPCObj, enteredTrafficCar, trafficVehicleRef);
                    return enteredTrafficCar;
                }

                carModel = EntityManager.GetComponentData<CarModelComponent>(trafficVehicleRef.RelatedEntity).Value;
            }
            else
            {
                UnityEngine.Debug.Log($"PlayerInteractCarBase. Traffic vehicle '{enteredTrafficCar.name}' doesn't have IHybridEntityRef");
            }

            var enteredPlayerCar = GetPlayerCar(carModel);

            if (enteredPlayerCar == null)
            {
                UnityEngine.Debug.Log($"PlayerInteractCarBase. Player car is null. Attempt to enter traffic vehicle '{enteredTrafficCar.name}', make sure that your factory contains prefab with '{carModel}' car model");
                return null;
            }

            enteredPlayerCar.gameObject.SetActive(true);

            Rigidbody rb = enteredPlayerCar.GetComponentInChildren<Rigidbody>();

            var trafficPos = enteredTrafficCar.transform.position + new Vector3(0, yOffset);

            if (rb)
                rb.Move(trafficPos, enteredTrafficCar.transform.rotation);

            enteredPlayerCar.transform.SetPositionAndRotation(trafficPos, enteredTrafficCar.transform.rotation);

            var vehicleRef = enteredPlayerCar.GetComponent<IHybridEntityRef>();

            AddPlayerTagToCar(enteredNPCObj, enteredPlayerCar, vehicleRef);

            if (trafficVehicleRef != null)
            {
                if (copyVelocity)
                {
                    if (rb)
                    {
                        var velocity = EntityManager.GetComponentData<VelocityComponent>(trafficVehicleRef.RelatedEntity);

#if UNITY_6000_0_OR_NEWER
                        rb.linearVelocity = velocity.Value;

#else
                        rb.velocity = velocity.Value;
#endif
                    }
                }

                EntityManager.DestroyEntity(trafficVehicleRef.RelatedEntity);
                enteredTrafficCar.ReturnToPool();
            }

            if (rb)
            {
                if (vehicleRef != null)
                    EntityManager.AddComponentObject(vehicleRef.RelatedEntity, rb);
            }
            else
            {
                UnityEngine.Debug.Log($"PlayerInteractCarBase. Player car '{enteredPlayerCar.name}' rigidbody not found");
            }

            return enteredPlayerCar;
        }

        public virtual void ExitCar(GameObject exitPlayerCar, GameObject npcObj)
        {
            AddPlayerTagToNpc(exitPlayerCar);
        }

        protected Entity GetEntity(GameObject car)
        {
            var vehicleRef = car.GetComponent<IHybridEntityRef>();

            if (vehicleRef != null)
            {
                return vehicleRef.RelatedEntity;
            }

            return Entity.Null;
        }

        private void AddPlayerTagToCar(GameObject enteredNPCObj, GameObject enteredPlayerCar, IHybridEntityRef vehicleRef)
        {
            if (!addPlayerTag)
                return;

            if (vehicleRef != null)
            {
                if (!EntityManager.HasComponent<PlayerTag>(vehicleRef.RelatedEntity))
                {
                    EntityManager.AddComponent<PlayerTag>(vehicleRef.RelatedEntity);
                }
            }
            else
            {
                UnityEngine.Debug.Log($"PlayerInteractCarBase. PlayerCar vehicle '{enteredPlayerCar.name}' doesn't have IHybridEntityRef. Make sure that your player car has the Hybrid 'HybridEntityRuntimeAuthoring', 'CustomRaycastTargetHybridComponent', 'CopyTransformFromGameObjectHybridComponent' components & Runtime 'BoundsRuntimeAuthoring', 'CarModelRuntimeAuthoring', 'VelocityRuntimeAuthoring' components");
            }

            var npcRef = enteredNPCObj.GetComponent<IHybridEntityRef>();

            if (npcRef != null)
            {
                if (EntityManager.HasComponent<PlayerTag>(npcRef.RelatedEntity))
                {
                    EntityManager.RemoveComponent<PlayerTag>(npcRef.RelatedEntity);
                }
            }
            else
            {
                UnityEngine.Debug.Log($"PlayerInteractCarBase. Player npc '{enteredNPCObj.name}' doesn't have IHybridEntityRef");
            }
        }

        private void AddPlayerTagToNpc(GameObject exitPlayerCar)
        {
            if (!addPlayerTag)
                return;

            var vehicleRef = exitPlayerCar.GetComponent<IHybridEntityRef>();

            if (vehicleRef != null)
            {
                if (EntityManager.HasComponent<PlayerTag>(vehicleRef.RelatedEntity))
                {
                    EntityManager.RemoveComponent<PlayerTag>(vehicleRef.RelatedEntity);
                }
            }

            var npcRef = exitPlayerCar.GetComponent<IHybridEntityRef>();

            if (npcRef != null && !EntityManager.HasComponent<PlayerTag>(vehicleRef.RelatedEntity))
            {
                EntityManager.AddComponent<PlayerTag>(npcRef.RelatedEntity);
            }
        }
    }
}