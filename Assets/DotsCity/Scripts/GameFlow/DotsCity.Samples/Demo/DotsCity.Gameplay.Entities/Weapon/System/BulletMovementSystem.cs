using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    [UpdateInGroup(typeof(FixedStepGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class BulletMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dt = SystemAPI.Time.DeltaTime;

            Entities.ForEach((
             ref LocalTransform transform,
             in BulletComponent bulletComponent,
             in BulletStatsComponent bulletStatsComponent) =>
            {
                transform.Position += transform.Forward() * bulletStatsComponent.FlySpeed * dt;

            }).ScheduleParallel();
        }
    }
}
