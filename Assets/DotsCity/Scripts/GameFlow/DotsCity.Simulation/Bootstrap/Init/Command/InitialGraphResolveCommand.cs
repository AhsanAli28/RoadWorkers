using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class InitialGraphResolveCommand : BootstrapCoroutineCommandBase
    {
        private EntityManager entityManager;
        private EntityQuery graphQuery;
        private EntityQuery roadStreamingConfigQuery;
        private EntityQuery segmentInitQuery;

        public InitialGraphResolveCommand(EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.entityManager = entityManager;
            InitQuery();
        }

        protected override IEnumerator InternalRoutine()
        {
            yield return new WaitWhile(() => graphQuery.CalculateEntityCount() == 0);

            DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficNodeResolverSystem>(true);

            yield return new WaitWhile(() => segmentInitQuery.CalculateEntityCount() != 0);

            var roadStreamingConfig = roadStreamingConfigQuery.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            if (roadStreamingConfig.StreamingIsEnabled)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRoadStreamingSystem, LateInitGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRoadUnloadSystem, InitGroup>();
            }
            else
            {
                DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficNodeResolverSystem>(false);
            }
        }

        private void InitQuery()
        {
            graphQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PathGraphSystem.Singleton>()
                .Build(entityManager);

            roadStreamingConfigQuery = entityManager.CreateEntityQuery(typeof(RoadStreamingConfigReference));
            segmentInitQuery = entityManager.CreateEntityQuery(typeof(SegmentInitTag));
        }
    }
}