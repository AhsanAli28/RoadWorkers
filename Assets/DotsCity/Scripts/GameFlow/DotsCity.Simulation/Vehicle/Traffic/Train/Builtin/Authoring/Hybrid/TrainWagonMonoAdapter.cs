using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    public class TrainWagonMonoAdapter : MonoBehaviour
    {
        [SerializeField] private PhysicsHybridEntityAdapter parentAdapter;
        [SerializeField] private PhysicsHybridEntityAdapter childAdapter;

        private void OnDisable()
        {
            parentAdapter.OnPhysicsStateChanged -= ParentAdapter_OnPhysicsStateChanged;
        }

        public void Initialize()
        {
            UpdateState();
            parentAdapter.OnPhysicsStateChanged += ParentAdapter_OnPhysicsStateChanged;
        }

        private void UpdateState()
        {
            ParentAdapter_OnPhysicsStateChanged(parentAdapter);
        }

        private void Reset()
        {
            if (transform.parent != null)
                parentAdapter = transform.parent.GetComponentInParent<PhysicsHybridEntityAdapter>();

            childAdapter = GetComponent<PhysicsHybridEntityAdapter>();
            EditorSaver.SetObjectDirty(this);
        }

        private void ParentAdapter_OnPhysicsStateChanged(PhysicsHybridEntityAdapter obj)
        {
            childAdapter.CheckCullState(obj.CullState);
        }
    }
}