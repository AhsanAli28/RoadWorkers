using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficLightDebugger : MonoBehaviour
    {
        [SerializeField] private bool showLights;

        public bool DrawGizmos { get => showLights; set => showLights = value; }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!showLights)
            {
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LightFrameHandlerStateComponent>());

            var entitiLightsComponents = query.ToEntityArray(Allocator.TempJob);
            var lightsComponents = query.ToComponentDataArray<LightFrameHandlerStateComponent>(Allocator.TempJob);

            for (int i = 0; i < lightsComponents.Length; i++)
            {
                var lightFrameData = entityManager.GetComponentData<LightFrameData>(entitiLightsComponents[i]);

                var pos = lightFrameData.IndexPosition;
                var color = TrafficLightSceneColor.StateToColor(lightsComponents[i].CurrentLightState);
                EditorExtension.DrawGizmosSimpleCube(pos, Vector3.one, color);
            }

            entitiLightsComponents.Dispose();
            lightsComponents.Dispose();
        }
#endif
    }
}