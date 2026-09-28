using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficRuntimeSettingsAuthoring : RuntimeEntityConfigBase
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [ShowIfNull]
        [SerializeField] private TrafficSettings trafficSettings;

        protected override bool UpdateAvailableByDefault => false;
        protected override bool AutoSync => false;

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            EntityType entityType = trafficSettings.EntityType;

            var settings = citySettingsInitializer.GetSettings<GeneralSettingDataSimulation>();

            if (settings.SimulationType == SimulationType.NoPhysics && entityType == EntityType.PureEntitySimplePhysics)
            {
                if (entityType != EntityType.PureEntityNoPhysics)
                {
                    Debug.Log($"TrafficRuntimeSettingsAuthoring. Traffic entity type '{entityType}' is reset to '{EntityType.PureEntityNoPhysics}', make sure have you physics enabled in the General Settings or set 'PureEntityNoPhysics' in the Traffic settings to hide this message.");
                    entityType = EntityType.PureEntityNoPhysics;
                }
            }

            dstManager.AddComponentData(entity, new TrafficRuntimeConfig() { EntityType = entityType });
        }
    }
}