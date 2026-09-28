using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [DisallowMultipleComponent]
    public partial class HybridComponentRootBehaviour : MonoBehaviour
    {
        private IHybridComponent[] hybridComponents;

        private void Awake()
        {
            hybridComponents = GetComponents<IHybridComponent>();
        }

        public void DisableComponents()
        {
            SwitchComponentState(false);
        }

        public void SwitchComponentState(bool isEnabled)
        {
            for (int i = 0; i < hybridComponents?.Length; i++)
            {
                hybridComponents[i].Enabled = isEnabled;
            }
        }
    }
}