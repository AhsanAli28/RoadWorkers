using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Events.Authoring
{
    [TemporaryBakingType]
    public struct TriggerNodeValidationTag : IComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TriggerNodeValidationSystem : SimpleSystemBase
    {
        private EntityQuery triggerNodeValidationTagQuery;
        private EntityQuery triggerNodeConfigReferenceQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            triggerNodeValidationTagQuery = EntityManager.CreateEntityQuery(typeof(TriggerNodeValidationTag));
            triggerNodeConfigReferenceQuery = EntityManager.CreateEntityQuery(typeof(TriggerNodeConfigReference));

            RequireForUpdate(triggerNodeValidationTagQuery);
        }

        protected override void OnUpdate()
        {
            bool exist = false;
            bool activated = false;

            if (triggerNodeConfigReferenceQuery.HasSingleton<TriggerNodeConfigReference>())
            {
                exist = true;
                activated = triggerNodeConfigReferenceQuery.GetSingleton<TriggerNodeConfigReference>().Config.Value.IsActivated;
            }

            if (exist && activated)
                return;

            if (!exist)
            {
                UnityEngine.Debug.Log("TriggerNodeValidationSystem. Some of the nodes are of type <b>Trigger</b>, but <b>TriggerNodeConfigAuthoring</b> doesn't exist in the subscene");
            }
            else if (!activated)
            {
                UnityEngine.Debug.Log("TriggerNodeValidationSystem. Some of the nodes are of type <b>Trigger</b>, but <b>TriggerNodeConfig</b> not activated");
            }

            Entities
            .WithoutBurst()
            .WithAll<TriggerNodeValidationTag>()
            .ForEach((
                Entity entity,
                ref NodeSettingsComponent nodeSettingsComponent) =>
            {
                nodeSettingsComponent.NodeType = Gameplay.Road.PedestrianNodeType.Default;

            }).Run();

            Entities
            .WithoutBurst()
            .WithAll<TriggerNodeValidationTag>()
            .ForEach((
                Entity entity,
                ref TrafficNodeSettingsComponent nodeSettingsComponent) =>
            {
                nodeSettingsComponent.TrafficNodeType = Gameplay.Road.TrafficNodeType.Default;

            }).Run();
        }
    }
}
