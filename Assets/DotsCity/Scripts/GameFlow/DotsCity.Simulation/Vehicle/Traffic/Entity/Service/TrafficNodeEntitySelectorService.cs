using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Road;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficNodeEntitySelectorService : EntitySceneSelectorServiceBase<WorldInteractView>
    {
        public struct NextNodeData
        {
            public Entity NextNode;
            public int NextPath;
            public int PreviousPath;
        }

        protected override EntityQuery GetSceneEntityQuery() =>
            EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TrafficNodeSettingsComponent>(),
                ComponentType.ReadOnly<LocalTransform>());

        protected override void InitWorldButton(WorldInteractView button, Entity entity, Vector3 pos, Action action, string text)
        {
            button.SetWorldButton(pos, action, text);
        }

        public NativeList<NextNodeData> GetNextEntities(Entity targetEntity, Allocator allocator = Allocator.Persistent)
        {
            if (!EntityManager.HasComponent<PathConnectionElement>(targetEntity))
                return default;

            var pathBuffer = EntityManager.GetBuffer<PathConnectionElement>(targetEntity);

            var nextEntities = new NativeList<NextNodeData>(pathBuffer.Capacity, allocator);

            for (int i = 0; i < pathBuffer.Length; i++)
            {
                if (pathBuffer[i].ConnectedNodeEntity != Entity.Null)
                {
                    nextEntities.Add(new NextNodeData()
                    {
                        NextNode = pathBuffer[i].ConnectedNodeEntity,
                        NextPath = pathBuffer[i].GlobalPathIndex,
                        PreviousPath = pathBuffer[i].GlobalPathIndex,
                    });
                }
            }

            return nextEntities;
        }

        protected override void InitButton(WorldInteractView button, SceneEntityData sceneEntityData)
        {
            button.SetWorldButton(sceneEntityData.Position, sceneEntityData.OnClick, "Select");
        }

        protected override void UpdateButtonPosition(WorldInteractView button, SceneEntityData sceneEntityData)
        {
            button.SetPosition(sceneEntityData.Position);
        }
    }
}
