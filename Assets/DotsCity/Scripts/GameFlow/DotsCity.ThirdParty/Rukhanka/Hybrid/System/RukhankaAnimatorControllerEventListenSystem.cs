using Rukhanka;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial class RukhankaAnimatorControllerEventListenSystem : SystemBase
    {
        private Dictionary<Entity, RukhankaEntityAdapterBase> listeners = new Dictionary<Entity, RukhankaEntityAdapterBase>();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            listeners.Clear();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            Entities
            .WithChangeFilter<AnimatorControllerEventComponent>()
            .WithoutBurst()
            .ForEach((
                Entity entity,
                in DynamicBuffer<AnimatorControllerEventComponent> eventBuffer) =>
            {
                if (eventBuffer.Length > 0)
                {
                    if (listeners.TryGetValue(entity, out var adapter))
                    {
                        for (int i = 0; i < eventBuffer.Length; i++)
                        {
                            adapter.RaiseEvent(eventBuffer[i]);
                        }
                    }
                }

            }).Run();
        }

        public static void AddListener(Entity entity, RukhankaEntityAdapterBase adapter)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RukhankaAnimatorControllerEventListenSystem>();
            system.Enabled = true;
            system.listeners.Add(entity, adapter);
        }

        public static void RemoveListener(Entity entity)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RukhankaAnimatorControllerEventListenSystem>();
            system.listeners.Remove(entity);

            if (!system.listeners.Any())
            {
                system.Enabled = false;
            }
        }
    }
}
