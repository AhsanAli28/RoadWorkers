using Unity.Collections;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public partial class CrowdTransitionProviderSystem : SystemBase
    {
        public struct InitTag : IComponentData { }

        private NativeHashMap<int, Entity> transitions;

        public NativeHashMap<int, Entity> Transitions { get => transitions; }
        public static NativeHashMap<int, Entity> TransitionsStaticRef { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (transitions.IsCreated)
            {
                transitions.Dispose();
                TransitionsStaticRef = default;
            }
        }

        protected override void OnUpdate() { }

        public Entity TryToGetTransitionEntity(int hash)
        {
            if (transitions.IsCreated && transitions.TryGetValue(hash, out Entity entity))
            {
                return entity;
            }

            return Entity.Null;
        }

        public void Initialize()
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AnimNodeEntryEntityData>()
                .Build(this);

            var entities = query.ToEntityArray(Allocator.TempJob);

            transitions = new NativeHashMap<int, Entity>(entities.Length, Allocator.Persistent);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var hash = EntityManager.GetComponentData<AnimNodeEntryEntityData>(entity).ActivateTriggerHash;

                if (!transitions.ContainsKey(hash))
                {
                    transitions.Add(hash, entity);
                }
                else
                {
                    UnityEngine.Debug.LogError($"PedestrianBakedTransitionProviderSystem. Duplicate hash {hash} found!");
                }
            }

            TransitionsStaticRef = transitions;

            entities.Dispose();

            EntityManager.CreateEntity(typeof(CrowdTransitionProviderSystem.InitTag));
        }
    }
}
