using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming.Authoring
{
    public class RoadSceneAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        class RoadSceneBaker : Baker<RoadSceneAuthoring>
        {
            public override void Bake(RoadSceneAuthoring authoring)
            {
                var scene = authoring.gameObject.scene;
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);

                var reference = new EntitySceneReference(sceneAsset);
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new RoadSceneData
                {
                    SceneReference = reference,
                    Hash128 = GetSceneGUID()
                });
            }
        }
#endif
    }
}