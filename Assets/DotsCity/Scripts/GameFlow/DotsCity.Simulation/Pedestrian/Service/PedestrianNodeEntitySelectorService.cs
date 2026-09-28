using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianNodeEntitySelectorService : EntitySceneSelectorServiceBase<WorldInteractView>
    {
        protected override EntityQuery GetSceneEntityQuery() =>
            EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NodeSettingsComponent>(),
                ComponentType.ReadOnly<LocalTransform>());

        protected override void InitWorldButton(WorldInteractView button, Entity entity, Vector3 pos, Action action, string text)
        {
            button.SetWorldButton(pos, action, text);
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
