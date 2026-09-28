using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class VelocityRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [SerializeField] private Rigidbody rb;

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(VelocityComponent), typeof(SpeedComponent) };

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            if (!rb)
            {
                Debug.LogError($"VelocityRuntimeAuthoring. {root.name} rigidbody not found.");
                return;
            }

            entityManager.AddComponentObject(entity, rb);
        }

        public void Reset()
        {
            if (!rb)
            {
                rb = GetComponentInChildren<Rigidbody>();
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}
