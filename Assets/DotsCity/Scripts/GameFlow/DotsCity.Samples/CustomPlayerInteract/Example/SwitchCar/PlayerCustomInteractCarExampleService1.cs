using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerCustomInteractCarExampleService1 : PlayerCustomInteractSwitchCarServiceBase
    {
        [SerializeField] private PlayerCarSpawner playerCarSpawner;

        protected override GameObject GetPlayerCar(int carModel)
        {
            return playerCarSpawner.Spawn(carModel, true);
        }

        private void Reset()
        {
            if (!playerCarSpawner)
            {
                playerCarSpawner = ObjectUtils.FindObjectOfType<PlayerCarSpawner>();
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}