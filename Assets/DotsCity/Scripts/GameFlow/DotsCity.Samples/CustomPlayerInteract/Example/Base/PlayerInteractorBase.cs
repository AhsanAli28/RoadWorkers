using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public abstract class PlayerInteractorBase : MonoBehaviour
    {
        protected abstract Transform CameraOrigin { get; }

        public event Action<GameObject> EnterCarFinished = delegate { };
        public event Action<GameObject> ExitCarFinished = delegate { };

        public void EnterCar(GameObject car)
        {
            StartCoroutine(EnterCarRoutine(car, this.gameObject));
        }

        public void ExitCar(GameObject car)
        {
            StartExitCarNpcAction(car, gameObject);
            BeforeExitCarInternal(car, transform.gameObject);
            SetCameraTarget(CameraOrigin, true);
            StartCoroutine(ExitCarRoutine(car, this.gameObject));
        }

        /// <summary>
        /// Custom logic method called by the player when the car is close & available.
        /// </summary>
        /// <param name="car"></param>
        /// <param name="npc"></param>
        protected abstract GameObject ConvertCarBeforeEnter(GameObject car);

        /// <summary>
        /// Custom logic method called by the player when exiting the car.
        /// </summary>
        /// <param name="car"></param>
        /// <param name="npc"></param>
        protected abstract void BeforeExitCarInternal(GameObject car, GameObject npc);

        /// <summary>
        ///  Method for the NPC when the NPC before entering the car.
        /// </summary>
        protected virtual void StartEnteringCarNpcAction(GameObject car, GameObject npc) { }

        /// <summary>
        /// Method for the car when the NPC before entering the car.
        /// </summary>
        protected virtual void StartEnteringCarAction(GameObject car, GameObject npc) { }

        /// <summary>
        /// True when the NPC has entered the car, e.g. finished entering the animation.
        /// </summary>
        protected virtual bool EnterFinished() => true;

        /// <summary>
        /// Method for the NPC when NPC enterered the car.
        /// </summary>
        protected abstract void EnterCarNpcActionFinished(GameObject car, GameObject npc);

        /// <summary>
        /// Method for the car when NPC entered the car.
        /// </summary>
        protected virtual void EnterCarActionFinished(GameObject car, GameObject npc)
        {
            car.GetComponent<PlayerCarBehaviourBase>().EnterCar(npc);
        }

        /// <summary>
        /// Method for the NPC to use when the NPC starts to leave the car.
        /// </summary>
        protected abstract void StartExitCarNpcAction(GameObject car, GameObject npc);

        /// <summary>
        /// Method for the NPC when the NPC has left the car.
        /// </summary>
        protected abstract void ExitCarNpcActionFinished(GameObject car, GameObject npc);

        /// <summary>
        /// True if the NPC has left the car, e.g. has finished the animation.
        /// </summary>
        protected virtual bool ExitFinished() => true;

        protected abstract void SetCameraTarget(Transform target, bool playerNpcCamera);

        private IEnumerator EnterCarRoutine(GameObject car, GameObject npc)
        {
            car = ConvertCarBeforeEnter(car);
            StartEnteringCarNpcAction(car, npc);
            StartEnteringCarAction(car, npc);

            yield return new WaitWhile(() => !EnterFinished());

            EnterCarNpcActionFinished(car, npc);
            EnterCarActionFinished(car, npc);

            SetCameraTarget(car.transform, false);
            EnterCarFinished(car);
        }

        private IEnumerator ExitCarRoutine(GameObject car, GameObject npc)
        {
            yield return new WaitWhile(() => !ExitFinished());

            ExitCarNpcActionFinished(car, gameObject);
            ExitCarFinished(npc);
        }
    }
}