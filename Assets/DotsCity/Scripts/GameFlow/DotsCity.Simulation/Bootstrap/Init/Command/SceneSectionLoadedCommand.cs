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
    public class SceneSectionLoadedCommand : BootstrapCoroutineCommandBase
    {
        private EntityQuery streamingConfigQuery;
        private EntityQuery sceneSectionConfigQuery;
        private EntityQuery cullPointQuery;

        private readonly EntityManager entityManager;

        public SceneSectionLoadedCommand(EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.entityManager = entityManager;

            InitQuery();
        }

        protected override IEnumerator InternalRoutine()
        {
            var streamingConfig = streamingConfigQuery.GetSingleton<StreamingLevelConfig>();

            if (streamingConfig.StreamingIsEnabled)
            {
                DefaultWorldUtils.SwitchActiveUnmanagedSystem<StreamingSceneSystem>(true);

                var sections = sceneSectionConfigQuery.ToComponentDataArray<SceneSectionData>(Allocator.TempJob);
                var sectionEntities = sceneSectionConfigQuery.ToEntityArray(Allocator.TempJob);

                if (cullPointQuery.CalculateEntityCount() == 1)
                {
                    var pos = cullPointQuery.GetSingleton<LocalToWorld>().Position.Flat();

                    Entity sceneSectionEntity = Entity.Null;

                    for (int i = 0; i < sections.Length; i++)
                    {
                        if (math.isnan(sections[i].BoundingVolume.Min.x))
                        {
                            continue;
                        }

                        AABB aabb = sections[i].BoundingVolume;

                        if (aabb.Contains(pos))
                        {
                            sceneSectionEntity = sectionEntities[i];
                            break;
                        }
                    }

                    sections.Dispose();
                    sectionEntities.Dispose();

                    if (sceneSectionEntity != Entity.Null)
                    {
                        yield return new WaitWhile(() => !entityManager.HasComponent<IsSectionLoaded>(sceneSectionEntity));
                    }
                }
                else
                {
                    sections.Dispose();
                    sectionEntities.Dispose();
                }
            }
        }

        private void InitQuery()
        {
            streamingConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                 .WithAll<StreamingLevelConfig>()
                 .Build(entityManager);

            sceneSectionConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SceneSectionData>()
                .Build(entityManager);

            cullPointQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullPointTag, LocalToWorld>()
                .Build(entityManager);
        }
    }
}