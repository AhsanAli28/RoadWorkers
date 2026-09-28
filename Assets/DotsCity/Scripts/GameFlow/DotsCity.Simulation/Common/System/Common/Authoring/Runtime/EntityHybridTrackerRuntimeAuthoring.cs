using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common.Authoring
{
    [RequireComponent(typeof(RuntimeEntityAuthoring))]
    public class EntityHybridTrackerRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider
    {
        [SerializeField] private Transform objectToTrack;

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(CopyTransformToGameObject), typeof(Transform) };

        private void Start()
        {
            var runtimeEntityAuthoring = GetComponent<RuntimeEntityAuthoring>();
            var entity = runtimeEntityAuthoring.Entity;

            if (objectToTrack == null)
            {
                runtimeEntityAuthoring.EntityManager.AddComponentObject(entity, transform);
            }
            else
            {
                runtimeEntityAuthoring.EntityManager.AddComponentObject(entity, objectToTrack);
            }
        }
    }
}
