#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Road.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class TrafficPathDrawerSystem : SystemBase
    {
        private const float TempVisualOffsetY = 1.5f;

        private PathIndexDebuggerSystem pathIndexDebuggerSystem;
        private EntityQuery trafficNodeQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeSettingsComponent>());

            RequireForUpdate<PathGraphReference>();
        }

        protected override void OnUpdate()
        {
            if (!PathDebugger.ShouldDrawEntityPath && !PathDebugger.ShoulDrawEntityNodeConnection)
                return;

            if (PathDebugger.ShouldDrawEntityPath && !PathDebugger.ShoulDrawEntityNodeConnection)
            {
                if (!Application.isPlaying)
                {
                    DrawEditor();
                }
                else
                {
                    DrawPlaymode();
                }
            }

            var nodeEntities = trafficNodeQuery.ToEntityArray(Allocator.TempJob);

            for (int j = 0; j < nodeEntities.Length; j++)
            {
                var localToWorld = EntityManager.GetComponentData<LocalToWorld>(nodeEntities[j]);
                var nodeEnabled = EntityManager.IsComponentEnabled<TrafficNodeComponent>(nodeEntities[j]);
                var pathConnections = EntityManager.GetBuffer<PathConnectionElement>(nodeEntities[j]);

                Vector3 position = localToWorld.Position + new float3(0, 1f, 0);
                DebugLine.DrawSlimArrow(position, localToWorld.Rotation, nodeEnabled ? Color.yellow : Color.red);

                if (PathDebugger.ShoulDrawEntityNodeConnection)
                {
                    for (int i = 0; i < pathConnections.Length; i++)
                    {
                        var pathData = pathConnections[i];

                        if (EntityManager.HasComponent<LocalToWorld>(pathData.ConnectedNodeEntity))
                        {
                            var targetTranslation = EntityManager.GetComponentData<LocalToWorld>(pathData.ConnectedNodeEntity);

                            float3 startPoint = localToWorld.Position + new float3(0, TempVisualOffsetY, 0);
                            float3 endPoint = targetTranslation.Position + new float3(0, TempVisualOffsetY, 0);

                            UnityEngine.Debug.DrawLine(startPoint, endPoint, Color.green);
                        }
                    }
                }
            }

            nodeEntities.Dispose();
        }

        private void DrawEditor()
        {
            var graph = SystemAPI.GetSingleton<PathGraphReference>();

            if (graph.Graph.Value.Paths.Length == 0) return;

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                DynamicBuffer<PathConnectionElement> pathConnections) =>
            {
                for (int i = 0; i < pathConnections.Length; i++)
                {
                    var pathData = pathConnections[i];
                    var pathIndex = pathData.GlobalPathIndex;

                    if (pathIndex >= graph.Graph.Value.Paths.Length)
                        continue;

                    ref var nodes = ref graph.Graph.Value.Paths[pathIndex].RouteNodes;

                    int startIndex = 0;
                    int endIndex = nodes.Length;

                    bool hasSubPath = false;

                    if (pathData.HasSubNode)
                    {
                        startIndex = pathData.StartLocalNodeIndex;

                        if (EntityManager.HasComponent<PathConnectionElement>(pathData.ConnectedSubNodeEntity))
                        {
                            var subpaths = EntityManager.GetBuffer<PathConnectionElement>(pathData.ConnectedSubNodeEntity);

                            if (subpaths.Length > 0)
                            {
                                if (subpaths[0].StartLocalNodeIndex != 0)
                                {
                                    endIndex = subpaths[0].StartLocalNodeIndex + 1;
                                }
                            }

                            hasSubPath = true;
                        }
                    }

                    for (int index = startIndex; index < endIndex - 1; index++)
                    {
                        DrawLine(nodes[index].Position, nodes[index + 1].Position, ref pathData, pathIndex, hasSubPath);
                    }
                }
            }).Run();
        }

        private void DrawPlaymode()
        {
            var graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                DynamicBuffer<PathConnectionElement> pathConnections) =>
            {
                for (int i = 0; i < pathConnections.Length; i++)
                {
                    var pathData = pathConnections[i];
                    var pathIndex = pathData.GlobalPathIndex;

                    if (pathIndex == -1) continue;

                    var nodes = graph.GetRouteNodes(pathIndex);

                    int startIndex = 0;
                    int endIndex = nodes.Length;

                    bool hasSubPath = false;

                    if (pathData.HasSubNode)
                    {
                        startIndex = pathData.StartLocalNodeIndex;

                        if (EntityManager.HasComponent<PathConnectionElement>(pathData.ConnectedSubNodeEntity))
                        {
                            var subpaths = EntityManager.GetBuffer<PathConnectionElement>(pathData.ConnectedSubNodeEntity);

                            if (subpaths.Length > 0)
                            {
                                if (subpaths[0].StartLocalNodeIndex != 0)
                                {
                                    endIndex = subpaths[0].StartLocalNodeIndex + 1;
                                }
                            }

                            hasSubPath = true;
                        }
                    }

                    for (int index = startIndex; index < endIndex - 1; index++)
                    {
                        DrawLine(nodes[index].Position, nodes[index + 1].Position, ref pathData, pathIndex, hasSubPath);
                    }
                }
            }).Run();
        }

        public void Initialize(PathIndexDebuggerSystem pathIndexDebuggerSystem)
        {
            this.pathIndexDebuggerSystem = pathIndexDebuggerSystem;
        }

        private void DrawLine(float3 a1, float3 a2, ref PathConnectionElement pathData, int pathIndex, bool hasSubPath)
        {
            var startPoint = a1 + new float3(0, TempVisualOffsetY, 0);
            var endPoint = a2 + new float3(0, TempVisualOffsetY, 0);

            PathIndexDebuggerSystem.SelectedPathData selectedPathData = null;

            if (pathIndexDebuggerSystem != null)
                selectedPathData = pathIndexDebuggerSystem.GetSelectedPathData(pathIndex);

            if (selectedPathData == null || !selectedPathData.Highliten)
            {
                Color color = default;

                if (pathData.ConnectedNodeEntity != Entity.Null && !pathData.HasSubNode || pathData.HasSubNode && pathData.ConnectedSubNodeEntity != Entity.Null)
                {
                    color = Color.magenta;
                }
                else
                {
                    if (!hasSubPath)
                    {
                        color = Color.red;
                    }
                    else
                    {
                        color = Color.yellow;
                    }
                }

                UnityEngine.Debug.DrawLine(startPoint, endPoint, color);
            }
            else
            {
                UnityEngine.Debug.DrawLine(startPoint, endPoint, selectedPathData.Color);
            }
        }
    }
}
#endif