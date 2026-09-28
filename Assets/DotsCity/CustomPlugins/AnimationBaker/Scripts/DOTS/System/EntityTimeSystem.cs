using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class EntityTimeSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            EntityManager.CreateEntity(typeof(EntityTime));
        }

        protected override void OnUpdate()
        {
            var time = UnityEngine.Time.time;

            Entities
            .WithBurst()
            .ForEach((
                Entity trafficEntity,
                ref EntityTime entityTime) =>
            {
                entityTime.UnityEngineTime = time;
            }).Run();
        }
    }
}