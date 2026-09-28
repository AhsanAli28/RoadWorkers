using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Debug
{
    public class TempCamera : MonoBehaviour
    {
        private PlayerActorTracker playerTargetHandler;

        [InjectWrapper]
        public void Construct(PlayerActorTracker playerTargetHandler)
        {
            this.playerTargetHandler = playerTargetHandler;
        }

        void Start()
        {
#if !ZENJECT
            Construct(FindObjectOfType<PlayerActorTracker>());
#endif

            playerTargetHandler.Actor = gameObject;
        }
    }
}