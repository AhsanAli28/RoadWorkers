using Spirit604.DotsCity.Core.Initialization;
using Spirit604.DotsCity.Simulation.Binding.Authoring;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Binding
{
    [DefaultExecutionOrder(-10000)]
    public class EntityBindingService : SingletonMonoBehaviour<EntityBindingService>
    {
        [SerializeField]
        private EntityBindingConfigAuthoring bindingConfig;

        private Dictionary<int, List<EntityWeakRef>> listeners = new Dictionary<int, List<EntityWeakRef>>();

        // Unique ID / entity
        private Dictionary<int, Entity> entityBindingData = new Dictionary<int, Entity>();

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private bool RoadStreamingFlag { get; set; }

        private void Start()
        {
            var query = EntityManager.CreateEntityQuery(typeof(RoadStreamingConfigReference));

            var initAwaiter = new SystemInitAwaiter(this, () => query.CalculateEntityCount() == 0,
            () =>
                {
                    RoadStreamingFlag = query.GetSingleton<RoadStreamingConfigReference>().Config.Value.StreamingIsEnabled;
                });
        }

        public void Subscribe(EntityWeakRef listener)
        {
            if (!bindingConfig.BindingAvailable)
            {
                Debug.LogError("EntityBindingService. Trying to subscribe to EntityWeakRef, but binding is disabled in the EntityBindingConfigAuthoring config.");
                return;
            }

            var id = listener.Id;

            if (id == 0)
            {
                Debug.LogError("EntityBindingService. Attempting to subscribe an EntityRef with id 0. Make sure that you bind EntityRef with scene entity.");
                return;
            }

            if (!listeners.ContainsKey(listener.Id))
                listeners.Add(listener.Id, new List<EntityWeakRef>());

            if (entityBindingData.ContainsKey(id))
                RaiseEvent(listener, entityBindingData[id]);

            listeners[id].Add(listener);
        }

        public void Unsubscribe(EntityWeakRef listener)
        {
            var id = listener.Id;

            if (listeners.ContainsKey(id) && listeners[id].Contains(listener))
            {
                listeners[id].Remove(listener);

                if (listeners[id].Count == 0)
                    listeners.Remove(id);
            }
        }

        internal bool RegisterEntity(Entity entity, int id)
        {
            if (id == 0)
            {
                Debug.LogError($"EntityBindingService. Attempt to register an Entity {entity.Index} with id 0. Make sure that you have baked the scene data.");
                return false;
            }

            if (!entityBindingData.ContainsKey(id))
            {
                entityBindingData.Add(id, entity);
            }
            else
            {
                entityBindingData[id] = entity;
            }

            if (!listeners.ContainsKey(id))
                return true;

            for (int i = 0; i < listeners[id].Count; i++)
            {
                var listener = listeners[id][i];
                RaiseEvent(listener, entity);
            }

            return true;
        }

        internal void UnregisterEntity(int id)
        {
            if (entityBindingData.ContainsKey(id))
                entityBindingData.Remove(id);

            if (!listeners.ContainsKey(id))
                return;

            for (int i = 0; i < listeners[id].Count; i++)
            {
                var item = listeners[id][i];
                item.UnloadEntity();
            }
        }

        private void RaiseEvent(EntityWeakRef listener, Entity entity)
        {
            if (RoadStreamingFlag)
            {
                if (!EntityManager.HasComponent<EntityBindingCleanup>(entity))
                {
                    EntityManager.AddComponentData(entity, new EntityBindingCleanup()
                    {
                        Value = listener.Id
                    });
                }
            }

            listener.SetEntity(entity);
        }
    }
}