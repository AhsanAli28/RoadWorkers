using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class NodeOwnerComponent : MonoBehaviour
    {
        [SerializeField] private RuntimeSegment runtimeSegment;

        public RuntimeSegment OwnerSegment => runtimeSegment;

        public void BindOwner()
        {
            if (runtimeSegment == null)
            {
                runtimeSegment = GetComponentInParent<RuntimeSegment>();
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void Reset()
        {
            BindOwner();
        }
    }
}
