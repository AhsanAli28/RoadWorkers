using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

#if UNITY_EDITOR
using System.Text;
#endif

namespace Spirit604.DotsCity.Debug
{
    public class TrafficRaycastDebugger : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficDebug.html#traffic-raycast-debugger")]
        [SerializeField] private string link;

        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private bool showCastInfo;

        [Tooltip("Show all info of all physics layers of physics shapes in the scene")]
        [ShowIf(nameof(showCastInfo))]
        [SerializeField] private bool showAllCollidersInfo;

        [ShowIf(nameof(showCastInfo))]
        [SerializeField] private Color fontColor = Color.white;

#if UNITY_EDITOR

        private StringBuilder sb = new StringBuilder();
        private string cachedRaycastText;
        private uint cachedMask;
        private HashSet<Entity> addedEntities = new HashSet<Entity>();
        private EntityQuery trafficRaycastObstacleQuery;
        private EntityQuery raycastHoldSystemQuery;
        private EntityQuery physicsColliderQuery;
        private EntityQuery raycastConfigQuery;

        private void Start()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            trafficRaycastObstacleQuery = entityManager.CreateEntityQuery(typeof(TrafficRaycastObstacleComponent));
            raycastHoldSystemQuery = entityManager.CreateEntityQuery(typeof(TrafficRaycastGizmosSystem.Singleton));
            physicsColliderQuery = entityManager.CreateEntityQuery(typeof(PhysicsCollider));
            raycastConfigQuery = entityManager.CreateEntityQuery(typeof(RaycastConfigReference));
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
                return;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (raycastHoldSystemQuery.CalculateEntityCount() == 0)
                return;

            var singleton = raycastHoldSystemQuery.GetSingleton<TrafficRaycastGizmosSystem.Singleton>();

            var trafficsRaycastDebugInfo = singleton.TrafficRaycastDebugInfo;

            if (!trafficsRaycastDebugInfo.IsCreated)
                return;

            var raycastInfoList = trafficsRaycastDebugInfo.GetValueArray(Allocator.TempJob);

            foreach (var item in raycastInfoList)
            {
                var color = item.HasHit ? Color.red : Color.green;
                BoxCastExtension.DrawBoxCastBox(item.origin, item.halfExtents, item.orientation, item.direction, item.MaxDistance, color);
            }

            raycastInfoList.Dispose();

            if (!showCastInfo)
                return;

            CheckRaycastLayers();

            addedEntities.Clear();

            var entities = trafficRaycastObstacleQuery.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                addedEntities.Add(entity);
                DrawLayerInfo(entityManager, entity, true);
            }

            entities.Dispose();

            if (showAllCollidersInfo)
            {
                var physicsEntities = physicsColliderQuery.ToEntityArray(Allocator.TempJob);

                for (int i = 0; i < physicsEntities.Length; i++)
                {
                    var entity = physicsEntities[i];

                    if (addedEntities.Contains(entity))
                    {
                        continue;
                    }

                    DrawLayerInfo(entityManager, entity);
                }

                physicsEntities.Dispose();
            }
        }

        private void DrawLayerInfo(EntityManager entityManager, Entity entity, bool includeRaycast = false)
        {
            sb.Clear();

            if (!entityManager.HasComponent<LocalTransform>(entity))
                return;

            var pos = entityManager.GetComponentData<LocalTransform>(entity).Position;

            GetLayer(entityManager, entity, ref sb);

            if (includeRaycast)
            {
                sb.Append(cachedRaycastText);
            }

            EditorExtension.DrawWorldString(sb.ToString(), pos, fontColor);
        }

        private string CheckRaycastLayers()
        {
            var raycastConfigReference = raycastConfigQuery.GetSingleton<RaycastConfigReference>();

            if (cachedMask != raycastConfigReference.Config.Value.RaycastFilter)
            {
                cachedMask = raycastConfigReference.Config.Value.RaycastFilter;

                var sb = new StringBuilder();

                sb.Append("Raycast: ");

                AppendLayers(sb, cachedMask);

                sb.Append(Environment.NewLine);
                cachedRaycastText = sb.ToString();
            }

            return cachedRaycastText;
        }

        private void GetLayer(EntityManager entityManager, Entity entity, ref StringBuilder sb)
        {
            if (entityManager.HasComponent<PhysicsCollider>(entity))
            {
                var physicsCollider = entityManager.GetComponentData<PhysicsCollider>(entity);
                var filter = physicsCollider.Value.Value.GetCollisionFilter();

                sb.Append($"Belongs To: ");
                AppendLayers(sb, filter.BelongsTo);
                sb.Append(Environment.NewLine);
            }
        }

        private void AppendLayers(StringBuilder sb, uint mask)
        {
            var layers = DotsEnumExtension.GetIndexLayers(mask);

            if (layers.Length == 32)
            {
                sb.Append("All layers");
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                sb.Append(layers[i]).Append("|");
            }
        }
#endif
    }
}
