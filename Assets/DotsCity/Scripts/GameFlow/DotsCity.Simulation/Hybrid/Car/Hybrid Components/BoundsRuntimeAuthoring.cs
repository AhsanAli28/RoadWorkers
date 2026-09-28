using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class BoundsRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [SerializeField] private Vector3 boundsSize;
        [SerializeField] private Vector3 boundsCenter;
        [SerializeField] private bool addObstacleComponent = true;
        [SerializeField] private bool drawBounds;

        public ComponentType[] GetComponentSet()
        {
            if (!addObstacleComponent)
            {
                return new ComponentType[] { typeof(BoundsComponent) };
            }
            else
            {
                return new ComponentType[] { typeof(BoundsComponent), typeof(ObstacleTag) };
            }
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity,
                new BoundsComponent()
                {
                    Extents = boundsSize / 2,
                    Center = boundsCenter
                });
        }

        public void Reset()
        {
            var meshes = GetComponentsInChildren<MeshRenderer>();

            if (meshes.Length == 0)
                return;

            var rot = transform.rotation;
            transform.rotation = Quaternion.identity;

            var bounds = meshes[0].bounds;
            bounds.center -= transform.position;

            for (int i = 1; i < meshes.Length; i++)
            {
                var meshBounds = meshes[i].bounds;
                meshBounds.center -= transform.position;
                bounds.Encapsulate(meshBounds);
            }

            boundsSize = bounds.size;
            boundsCenter = bounds.center;

            transform.rotation = rot;

            EditorSaver.SetObjectDirty(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawBounds)
                return;

            var origin = transform.position + boundsCenter;

            Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        }
    }
}
