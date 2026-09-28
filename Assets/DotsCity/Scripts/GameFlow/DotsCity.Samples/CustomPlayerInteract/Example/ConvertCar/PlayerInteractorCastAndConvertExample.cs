using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    /// <summary>
    /// An example of a player NPC interacting with traffic when the player car & traffic car have the same motion controllers, using a sample raycaster.
    /// </summary>
    public class PlayerInteractorCastAndConvertExample : PlayerInteractorConvertExample
    {
        [SerializeField] private PlayerInteractCastExample playerInteractCastExample;

        protected virtual void Awake()
        {
            playerInteractCastExample.OnCastStateChanged += PlayerInteractCastExample_OnCastStateChanged;
        }

        protected virtual void PlayerInteractCastExample_OnCastStateChanged(GameObject car)
        {
            ProcessNewCar(car);
        }
    }
}