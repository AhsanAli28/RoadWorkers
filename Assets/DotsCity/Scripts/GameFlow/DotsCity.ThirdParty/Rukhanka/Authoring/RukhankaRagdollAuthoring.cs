using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [TemporaryBakingType]
    public struct RukhankaRagdollBakingTag : IComponentData { }

    public class RukhankaRagdollAuthoring : MonoBehaviour
    {
        public List<GameObject> ragdollBones = new();

#if UNITY_PHYSICS
        class RukhankaRagdollAuthoringBaker : Baker<RukhankaRagdollAuthoring>
        {
            public override void Bake(RukhankaRagdollAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<RukhankaRagdollBuffer>(entity);

                foreach (var ragdollBone in authoring.ragdollBones)
                {
                    var boneEntity = GetEntity(ragdollBone, TransformUsageFlags.Dynamic);

                    buffer.Add(new RukhankaRagdollBuffer()
                    {
                        Entity = boneEntity,
                        RelativeParentTransform = new Unity.Mathematics.RigidTransform(ragdollBone.transform.rotation, ragdollBone.transform.position),
                    });
                }

                AddComponent<RukhankaRagdollBakingTag>(entity);
            }
        }
#endif

        //[Attributes.Button]
        //public void TestRagdoll()
        //{
        //    var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //    var q = manager.CreateEntityQuery(ComponentType.ReadOnly<RukhankaRagdollBuffer>());

        //    var entities = q.ToEntityArray(Unity.Collections.Allocator.Temp);

        //    for (int k = 0; k < entities.Length; k++)
        //    {
        //        var entity = entities[k];

        //        var layerBuffer = manager.GetBuffer<Rukhanka.AnimatorControllerLayerComponent>(entity);

        //        for (int i = 0; i < layerBuffer.Length; i++)
        //        {
        //            var layerData = layerBuffer[i];
        //            layerData.weight = 0;
        //            layerBuffer[i] = layerData;
        //        }

        //        var buffer = manager.GetBuffer<RukhankaRagdollBuffer>(entity);

        //        for (int i = 0; i < buffer.Length; i++)
        //        {
        //            manager.SetComponentEnabled<Simulate>(buffer[i].Entity, true);
        //        }
        //    }
        //}

        private void Reset()
        {
            ragdollBones = GetComponentsInChildren<Rigidbody>().Select(a => a.gameObject).ToList();

            var joints = GetComponentsInChildren<CharacterJoint>();

            foreach (var joint in joints)
            {
                joint.autoConfigureConnectedAnchor = true;
                var anchor = joint.connectedAnchor;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = anchor;
                EditorSaver.SetObjectDirty(joint);
            }

            EditorSaver.SetObjectDirty(this);
        }
    }
}