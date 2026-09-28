using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerInteractorNpcExample : MonoBehaviour
    {
        [SerializeField] private PlayerInteractCastExample playerInteractCastExample;

        private PedestrianInteractable pedestrianInteractable;

        private void Awake()
        {
            playerInteractCastExample.OnCastStateChanged += PlayerInteractCastExample_OnCastStateChanged;
        }

        private void Update()
        {
            Process();
        }

        /// <summary>
        /// Check that the player has clicked the Interact button.
        /// </summary>
        protected virtual bool ProcessUserInput()
        {
            return InputExtension.GetKeyDown(KeyCode.E);
        }

        private void Interact(PedestrianInteractable npc)
        {
            npc.Activate();

            // Set talking state e.g.
            // var animator = npc.GetComponent<Animator>();
            // animator.SetFloat("yInput", 0);
            // animator.SetFloat("SideMovement", 0);
            // animator.SetFloat("Talking", 0);
        }

        private void Process()
        {
            if (pedestrianInteractable && ProcessUserInput())
            {
                Interact(pedestrianInteractable);
            }
        }

        private void PlayerInteractCastExample_OnCastStateChanged(GameObject interactable)
        {
            if (interactable == null)
            {
                pedestrianInteractable = null;
                return;
            }

            if (interactable.TryGetComponent<PedestrianInteractable>(out pedestrianInteractable))
            {

            }
        }
    }
}