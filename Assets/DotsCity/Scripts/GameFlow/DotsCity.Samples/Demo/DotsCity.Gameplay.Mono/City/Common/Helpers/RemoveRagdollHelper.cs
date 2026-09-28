using Spirit604.Attributes;
using System.Linq;
using UnityEngine;

namespace Spirit604.Utils
{
    public class RemoveRagdollHelper : MonoBehaviourBase
    {
        [Button]
        public void Remove()
        {
            var joints = GetComponentsInChildren<CharacterJoint>();

            for (int i = 0; i < joints?.Length; i++)
            {
                DestroyImmediate(joints[i]);
            }

            var colliders = GetComponentsInChildren<Collider>().Where(a => a.transform != transform).ToArray();

            for (int i = 0; i < colliders?.Length; i++)
            {
                DestroyImmediate(colliders[i]);
            }

            var rbs = GetComponentsInChildren<Rigidbody>().Where(a => a.transform != transform).ToArray();

            for (int i = 0; i < rbs?.Length; i++)
            {
                DestroyImmediate(rbs[i]);
            }

            Destroy(this);
        }
    }
}
