using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    public class RukhankaEntityAdapter : RukhankaEntityAdapterBase
    {
        private IHybridEntityRef hybridEntityRef;

        protected override Entity Entity => hybridEntityRef.RelatedEntity;

        private void Awake()
        {
            hybridEntityRef = GetComponent<IHybridEntityRef>();
            hybridEntityRef.OnEntityInitialized += HybridEntityRef_OnEntityInitialized;
        }

        private void OnEnable()
        {
            if (hybridEntityRef.HasEntity)
            {
                TryToAddEventListeners();
            }
        }

        private void OnDisable()
        {
            TryToRemoveEventListeners();
        }

        private void TryToAddEventListeners()
        {
            TryToAddEventListener();
            TryToAddControllerEventListener();
        }

        private void TryToRemoveEventListeners()
        {
            TryToRemoveEventListener();
            TryToRemoveControllerEventListener();
        }

        private void HybridEntityRef_OnEntityInitialized(Entity obj)
        {
            TryToAddEventListeners();
        }
    }
}