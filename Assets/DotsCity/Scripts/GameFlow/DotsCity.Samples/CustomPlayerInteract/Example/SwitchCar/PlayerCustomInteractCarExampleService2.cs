using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerCustomInteractCarExampleService2 : PlayerCustomInteractSwitchCarServiceBase
    {
        [SerializeField] private PlayerCarFactoryExample playerCarFactory;

        protected override GameObject GetPlayerCar(int carModel)
        {
            return playerCarFactory.Get(carModel);
        }
    }
}