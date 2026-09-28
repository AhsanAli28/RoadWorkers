using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    /// <summary>
    /// An example of a player NPC interacting with traffic when the player car & traffic car have different motion controllers.
    /// </summary>
    public class PlayerInteractorSwitchExample : PlayerInteractorExampleBase
    {
        [SerializeField] private KeyCode enterCarKey = KeyCode.E;

        protected override bool ProcessUserInput() => InputExtension.GetKeyDown(enterCarKey);

        protected override GameObject ConvertCarBeforeEnter(GameObject car)
        {
            return PlayerCustomInteractSwitchCarServiceBase.Instance.ConvertCarBeforeEnter(car, gameObject);
        }

        protected override void BeforeExitCarInternal(GameObject car, GameObject npc)
        {
            PlayerCustomInteractSwitchCarServiceBase.Instance.ExitCar(car, transform.gameObject);
        }
    }
}