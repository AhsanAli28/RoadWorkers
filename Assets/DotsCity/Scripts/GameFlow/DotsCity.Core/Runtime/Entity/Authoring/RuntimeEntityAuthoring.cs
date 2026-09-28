using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [DisallowMultipleComponent]
    public class RuntimeEntityAuthoring : RuntimeEntityAuthoringBase
    {
        protected virtual void Start()
        {
            InitEntity();
        }
    }
}
