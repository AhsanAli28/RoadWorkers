using UnityEngine;

#if PROJECTDAWN_NAV
using Unity.Entities;
#endif

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class AgentsNavigationConfigAuthoring : MonoBehaviour
    {
        public AgentsNavigationSettingsConfig config;

#if PROJECTDAWN_NAV
        class AgentsNavigationConfigAuthoringBaker : Baker<AgentsNavigationConfigAuthoring>
        {
            public override void Bake(AgentsNavigationConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                AgentsNavigationSettingsConfig config = authoring.config;

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<AgentsNavigationConversionSettings>();

                    if (config)
                    {
                        root = new AgentsNavigationConversionSettings(config);
                    }
                    else
                    {
                        root = AgentsNavigationConversionSettings.GetDefault();
                    }

                    var blobRef = builder.CreateBlobAssetReference<AgentsNavigationConversionSettings>(Unity.Collections.Allocator.Persistent);

                    this.AddBlobAsset(ref blobRef, out var hash);

                    this.AddComponent(entity, new AgentsNavigationConversionSettingsReference() { Config = blobRef });
                }
            }
        }
#endif
    }
}
