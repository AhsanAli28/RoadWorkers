using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.Extensions;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class RoadSectionLoadedCommand : BootstrapCoroutineCommandBase
    {
        private EntityQuery roadStreamingConfigQuery;
        private EntityQuery roadSectionConfigQuery;
        private EntityQuery cullPointQuery;

        private readonly EntityManager entityManager;

        public RoadSectionLoadedCommand(EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.entityManager = entityManager;

            InitQuery();
        }

        private void InitQuery()
        {
            roadStreamingConfigQuery = entityManager.CreateEntityQuery(typeof(RoadStreamingConfigReference));

            roadSectionConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoadSectionData, SceneSectionData>()
                .Build(entityManager);

            cullPointQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullPointTag, LocalToWorld>()
                .Build(entityManager);
        }

        protected override IEnumerator InternalRoutine()
        {
            var roadStreamingConfig = roadStreamingConfigQuery.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            if (roadStreamingConfig.StreamingIsEnabled)
            {
                var roadSections = roadSectionConfigQuery.ToComponentDataArray<SceneSectionData>(Allocator.TempJob);
                var roadSectionEntities = roadSectionConfigQuery.ToEntityArray(Allocator.TempJob);

                var pos = cullPointQuery.GetSingleton<LocalToWorld>().Position.Flat();
                var roadSectionEntity = Entity.Null;

                for (int i = 0; i < roadSections.Length; i++)
                {
                    if (math.isnan(roadSections[i].BoundingVolume.Min.x))
                        continue;

                    AABB aabb = roadSections[i].BoundingVolume;

                    if (aabb.Contains(pos))
                    {
                        roadSectionEntity = roadSectionEntities[i];
                        break;
                    }
                }

                roadSections.Dispose();
                roadSectionEntities.Dispose();

                if (roadSectionEntity != Entity.Null)
                {
                    yield return new WaitWhile(() => !entityManager.HasComponent<IsSectionLoaded>(roadSectionEntity));
                }
            }
        }
    }
}