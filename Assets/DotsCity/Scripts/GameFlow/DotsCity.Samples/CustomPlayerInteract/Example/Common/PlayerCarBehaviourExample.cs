using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerCarBehaviourExample : PlayerCarBehaviourBase
    {
        [SerializeField] private KeyCode exitKey = KeyCode.E;

        private GameObject playerNpcRef;

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            if (ClickedExitCarButton())
            {
                ExitCar();
            }
        }

        public override bool EnterCar(GameObject playerNpc)
        {
            playerNpcRef = playerNpc;

            // Enable  custom car input, some code
            EnableInput();

            enabled = true;
            return true;
        }

        public override GameObject ExitCar()
        {
            var currentRef = playerNpcRef;
            playerNpcRef.GetComponent<PlayerInteractorBase>().ExitCar(this.gameObject);

            // Disable custom car input, some code
            DisableInput();

            playerNpcRef = null;
            enabled = false;

            return currentRef;
        }

        public override void Init() { }

        protected virtual bool ClickedExitCarButton() => InputExtension.GetKeyDown(exitKey);

        protected virtual void EnableInput() { }

        protected virtual void DisableInput() { }
    }
}