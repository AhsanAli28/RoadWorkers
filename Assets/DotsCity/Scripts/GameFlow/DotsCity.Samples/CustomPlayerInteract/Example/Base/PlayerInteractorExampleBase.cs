using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public abstract class PlayerInteractorExampleBase : PlayerInteractorBase
    {
        [Tooltip("Camera follow target if null parent is used as origin")]
        [SerializeField] private Transform cameraOrigin;

        private GameObject car;

        protected override Transform CameraOrigin => cameraOrigin ?? transform;

        protected virtual void Update()
        {
            ProcessCarInternal();
        }

        /// <summary>
        /// Add an interactive car using this method.
        /// </summary>
        public virtual void ProcessNewCar(GameObject newCar)
        {
            if (IsAvailable(newCar))
            {
                this.car = newCar;
            }
            else
            {
                if (this.car)
                {
                    this.car = null;
                }
            }
        }

        /// <summary>
        /// Check that the player has clicked the Interact button.
        /// </summary>
        protected abstract bool ProcessUserInput();

        /// <summary>
        /// Method if object is a car & available for interaction.
        /// </summary>
        protected virtual bool IsAvailable(GameObject car) => car != null;

        /// <summary>
        /// Method for the NPC when NPC enterered the car.
        /// </summary>
        protected override void EnterCarNpcActionFinished(GameObject car, GameObject npc)
        {
            npc.transform.SetParent(car.transform);
            npc.transform.gameObject.SetActive(false);
        }

        /// <summary>
        /// Method for the NPC to use when the NPC starts to leave the car.
        /// </summary>
        protected override void StartExitCarNpcAction(GameObject car, GameObject npc)
        {
            npc.transform.SetParent(null);
            npc.SetActive(true);
        }

        /// <summary>
        /// Method for the NPC when the NPC has left the car.
        /// </summary>
        protected override void ExitCarNpcActionFinished(GameObject car, GameObject npc)
        {
            transform.position = car.transform.position - car.transform.right * 1.5f;
        }

        protected override void SetCameraTarget(Transform target, bool playerNpcCamera)
        {
            PlayerCameraBehaviourExample.Instance.SetTarget(target, playerNpcCamera);
        }

        private void ProcessCarInternal()
        {
            if (car && ProcessUserInput())
            {
                EnterCar(car);
            }
        }
    }
}