using Spirit604.DotsCity.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Helper class to create pedestrian area triggers from custom code (system, monobehaviour, etc...)
    /// </summary>
    [UpdateInGroup(typeof(BeginSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class CustomAreaTriggerCreatorSystem : SystemBase
    {
        public struct EntityData
        {
            public Entity Entity;
            public float3 Position;
            public float DisableTime;
        }

        private const float SHOOTING_TRIGGER_LIFE_TIME = 2f;
        public const float SHOOTING_TRIGGER_SQ_DISTANCE = 144;

        // 0.2 * 0.2
        private const float UpdateTriggerDistanceSQ = 0.04f;
        private const float CellRadius = 1f;

        private Dictionary<int, EntityData> data = new Dictionary<int, EntityData>();
        private List<int> listToRemove = new List<int> { };

        private static bool registered;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            registered = false;
        }

        /// <summary>
        /// Сreating aria triggers for pedestrians.
        /// </summary>
        /// <param name="position">Position of the trigger.</param>
        /// <param name="distance">If distance is 0, the default is used.</param>
        /// <param name="duration">If duration is 0, the default is used.</param>
        public static Entity CreateScaryTriggerStatic(Vector3 position, float distance = 0, float duration = 0, bool autoLifetimeRegister = true, TriggerAreaType triggerAreaType = TriggerAreaType.FearPointTrigger)
        {
            var system = GetOrCreateSystem();

            return system.CreateAreaTrigger(position, distance, duration, autoLifetimeRegister, triggerAreaType);
        }

        public static CustomAreaTriggerCreatorSystem GetOrCreateSystem()
        {
            if (!registered)
            {
                registered = true;
                return DefaultWorldUtils.CreateAndAddSystemManaged<CustomAreaTriggerCreatorSystem, BeginSimulationGroup>();
            }

            return World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CustomAreaTriggerCreatorSystem>();
        }

        /// <summary>
        /// Сreating aria triggers for pedestrians.
        /// </summary>
        /// <param name="position">Position of the trigger.</param>
        /// <param name="distance">If distance is 0, the default is used.</param>
        /// <param name="duration">If duration is 0, the default is used.</param>
        public Entity CreateAreaTrigger(Vector3 position, float distance = 0, float duration = 0, bool autoLifetimeRegister = true, TriggerAreaType triggerAreaType = TriggerAreaType.FearPointTrigger)
        {
            if (distance == 0)
            {
                distance = SHOOTING_TRIGGER_SQ_DISTANCE;
            }
            else
            {
                distance *= distance;
            }

            if (duration == 0)
            {
                duration = SHOOTING_TRIGGER_LIFE_TIME;
            }

            var hash = HashMapHelper.GetHashMapPosition(position, CellRadius);

            Entity entity = Entity.Null;

            bool updateEntity = false;

            if (!data.ContainsKey(hash))
            {
                entity = GetTriggerEntity();

                if (autoLifetimeRegister)
                {
                    var entityData = new EntityData()
                    {
                        Entity = entity,
                        DisableTime = duration + (float)SystemAPI.Time.ElapsedTime
                    };

                    data.Add(hash, entityData);
                }

                updateEntity = true;
            }
            else
            {
                var entityData = data[hash];

                entity = entityData.Entity;
                entityData.DisableTime = duration + (float)SystemAPI.Time.ElapsedTime;

                // To avoid spam in the same position
                if (math.distancesq(position, entityData.Position) >= UpdateTriggerDistanceSQ)
                {
                    updateEntity = true;
                    entityData.Position = position;
                }

                data[hash] = entityData;
            }

            if (updateEntity)
            {
                EntityManager.SetComponentData(entity, new TriggerComponent()
                {
                    Position = position,
                    TriggerDistanceSQ = distance,
                    TriggerAreaType = triggerAreaType
                });
            }

            Enabled = true;
            return entity;
        }

        protected override void OnUpdate()
        {
            if (data.Count == 0)
            {
                Enabled = false;
                return;
            }

            var keys = data.Keys;

            foreach (var key in keys)
            {
                var entityData = data[key];

                if (SystemAPI.Time.ElapsedTime >= entityData.DisableTime)
                {
                    listToRemove.Add(key);
                }
            }

            if (listToRemove.Count > 0)
            {
                for (int i = 0; i < listToRemove.Count; i++)
                {
                    data.Remove(listToRemove[i]);
                }

                listToRemove.Clear();
            }
        }

        private Entity GetTriggerEntity()
        {
            return EntityManager.CreateEntity(typeof(TriggerComponent));
        }
    }
}
