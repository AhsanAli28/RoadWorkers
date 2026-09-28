using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class PhysicsSwitcher : MonoBehaviourBase
    {
        [SerializeField] private Rigidbody[] rbs;
        [SerializeField] private Collider[] colliders;

        private List<Vector3> poses;
        private List<Quaternion> rotations;

        private void Awake()
        {
            if (rbs?.Length > 1)
            {
                poses = new List<Vector3>();
                rotations = new List<Quaternion>();

                for (int i = 1; i < rbs.Length; i++)
                {
                    var pos = transform.InverseTransformPoint(rbs[i].transform.position);
                    var rot = Quaternion.Inverse(transform.rotation) * rbs[i].transform.rotation;
                    poses.Add(pos);
                    rotations.Add(rot);
                }
            }
        }

        public void SwitchPhysics(bool isActive)
        {
            for (int i = 0; i < rbs?.Length; i++)
            {
                Rigidbody rb = rbs[i];

                if (Application.isPlaying && i >= 1)
                {
                    var pos = transform.TransformPoint(poses[i - 1]);
                    var rot = transform.rotation * rotations[i - 1];

#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = default;
#else
                    rb.velocity = default;
#endif

                    rb.angularVelocity = default;
                    rb.transform.position = pos;
                    rb.transform.rotation = rot;

                    rb.position = pos;
                    rb.rotation = rot;
                }

                rb.isKinematic = !isActive;
                rb.detectCollisions = isActive;

                if (!isActive) rb.Sleep(); else rb.WakeUp();
            }

            for (int i = 0; i < colliders?.Length; i++)
            {
                colliders[i].enabled = isActive;
            }
        }

        [Button]
        public void Enable()
        {
            SwitchPhysics(true);
        }

        [Button]
        public void Disable()
        {
            SwitchPhysics(false);
        }

        private void Reset()
        {
            rbs = GetComponentsInChildren<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>().Where(a => a.enabled).ToArray();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
