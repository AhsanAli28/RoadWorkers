using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    /// <summary>
    /// Class when the user wants to temporarily or permanently take full control of the pedestrian for customised movement, state, etc.
    /// </summary>
    public class PedestrianInteractable : MonoBehaviour
    {
        private IHybridEntityRef hybridEntityRef;
        private bool activated;

        public bool Activated => activated;

        private void Awake()
        {
            hybridEntityRef = GetComponent<IHybridEntityRef>();
        }

        /// <summary>
        /// Remove the pedestrian entity from the DOTS simulation. All custom states, locomotion & animation should be handled by custom user code using monobehavior scripts.
        /// </summary>
        public bool Activate()
        {
            if (activated) return false;

            if (PedestrianInteractUtils.RemoveFromSimulation(hybridEntityRef.RelatedEntity))
            {
                activated = true;
            }

            return activated;
        }

        /// <summary>
        /// Return the entity to the simulation.
        /// </summary>
        public bool Deactivate()
        {
            if (!activated) return false;

            if (PedestrianInteractUtils.RestoreToSimulation(hybridEntityRef.RelatedEntity))
            {
                activated = false;
            }

            return !activated;
        }
    }
}