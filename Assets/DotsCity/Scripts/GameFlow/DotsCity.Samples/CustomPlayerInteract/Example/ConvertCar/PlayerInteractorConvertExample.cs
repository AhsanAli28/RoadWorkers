using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    /// <summary>
    /// An example of a player NPC interacting with traffic when the player car & traffic car have the same motion controllers.
    /// </summary>
    public class PlayerInteractorConvertExample : PlayerInteractorExampleBase
    {
        [SerializeField] private KeyCode enterCarKey = KeyCode.E;

        protected override bool ProcessUserInput() => InputExtension.GetKeyDown(enterCarKey);

        protected override GameObject ConvertCarBeforeEnter(GameObject car)
        {
            PlayerCustomInteractCarServiceBase.Instance.ConvertCarBeforeEnter(car, gameObject);
            return car;
        }

        protected override void BeforeExitCarInternal(GameObject car, GameObject npc)
        {
            PlayerCustomInteractCarServiceBase.Instance.ExitCar(car, transform.gameObject);
        }
    }
}