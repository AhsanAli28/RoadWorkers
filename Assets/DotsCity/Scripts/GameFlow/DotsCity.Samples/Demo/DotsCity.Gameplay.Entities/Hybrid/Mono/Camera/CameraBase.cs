using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.CameraService
{
    public abstract class CameraBase : MonoBehaviour
    {
        [InjectWrapper]
        public virtual void Construct(PlayerActorTracker playerActorTracker)
        {
            this.PlayerActorTracker = playerActorTracker;
        }

        protected PlayerActorTracker PlayerActorTracker { get; private set; }

        protected Transform CurrentActor { get; set; }

        protected virtual void OnEnable()
        {
            PlayerActorTracker.OnSwitchActor += PlayerActorTracker_OnSwitchActor;
        }

        protected virtual void OnDisable()
        {
            PlayerActorTracker.OnSwitchActor -= PlayerActorTracker_OnSwitchActor;
        }

        public virtual bool SetTarget(Transform newActor)
        {
            if (newActor == null)
            {
                return false;
            }

            CurrentActor = newActor;
            return true;
        }

        protected abstract void PlayerActorTracker_OnSwitchActor(GameObject newPlayerActor);
    }
}