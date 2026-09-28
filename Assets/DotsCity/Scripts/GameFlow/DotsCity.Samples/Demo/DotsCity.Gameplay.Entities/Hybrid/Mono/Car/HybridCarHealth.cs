using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Car;

namespace Spirit604.DotsCity.Gameplay.Car
{
    public class HybridCarHealth : CarHealthBase
    {
        private HybridComponentRootBehaviour hybridComponentRootBehaviour;

        protected override void Awake()
        {
            base.Awake();
            hybridComponentRootBehaviour = GetComponent<HybridComponentRootBehaviour>();
        }

        protected override void Death()
        {
            base.Death();
            DisableComponents();
        }

        protected override void DeathVfxFinished()
        {
            base.DeathVfxFinished();
            gameObject.ReturnToPool();
        }

        private void DisableComponents()
        {
            hybridComponentRootBehaviour?.DisableComponents();
        }
    }
}