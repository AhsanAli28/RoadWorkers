using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class InitializerBase : MonoBehaviour
    {
        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }
}
