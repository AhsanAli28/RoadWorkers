using Spirit604.DotsCity.Hybrid.Core;
using System;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianRagdoll : RagdollBase
    {
        private float SwitchTime;
        private bool isActivated;

        public float RagdollEnabledDuration { get; set; } = 5f;
        public int DamageDealerID { get; set; }

        public Action<PedestrianRagdoll> OnLifeCycleExpired = delegate { };

        public bool DoUpdate()
        {
            if (!isActivated)
            {
                return true;
            }

            if (UnityEngine.Time.time >= SwitchTime)
            {
                isActivated = false;
                OnLifeCycleExpired(this);
                return false;
            }

            return true;
        }

        protected override void SwitchActiveState(bool isActive)
        {
            base.SwitchActiveState(isActive);

            SetSwitchTime(isActive);
        }

        protected void SetSwitchTime(bool isActive)
        {
            isActivated = isActive;

            if (isActive)
            {
                SwitchTime = UnityEngine.Time.time + RagdollEnabledDuration;
            }
        }
    }
}