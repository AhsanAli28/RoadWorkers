using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    [UpdateInGroup(typeof(HashMapGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class BulletHashMapSystem : SimpleSystemBase
    {
        public struct BulletEntityData : IComponentData
        {
            public Entity Entity;
            public float3 Position;
            public float3 Forward;
            public FactionType FactionType;
            public int Damage;
            public int IsDestroyed;
        }

        private const int MAX_BULLET_HASHMAP_CAPACITY = 100;

        public NativeParallelMultiHashMap<int, BulletEntityData> BulletHashMap { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            BulletHashMap = new NativeParallelMultiHashMap<int, BulletEntityData>(MAX_BULLET_HASHMAP_CAPACITY, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BulletHashMap.Dispose();
        }

        protected override void OnUpdate()
        {
            BulletHashMap.Clear();
            var bulletHashMapParallel = BulletHashMap.AsParallelWriter();

            Entities
            .WithBurst()
            .WithNativeDisableContainerSafetyRestriction(bulletHashMapParallel)
            .ForEach((
                Entity entity,
                in LocalTransform transform,
                in BulletComponent bulletComponent,
                in BulletStatsComponent bulletStatsComponent) =>
            {
                int hashKey = HashMapHelper.GetHashMapPosition(transform.Position.Flat());

                var bulletEntityData = new BulletEntityData()
                {
                    Entity = entity,
                    Position = transform.Position,
                    Forward = transform.Forward(),
                    Damage = bulletStatsComponent.Damage,
                    FactionType = bulletComponent.FactionType
                };

                bulletHashMapParallel.Add(hashKey, bulletEntityData);
            }).ScheduleParallel();
        }
    }
}
