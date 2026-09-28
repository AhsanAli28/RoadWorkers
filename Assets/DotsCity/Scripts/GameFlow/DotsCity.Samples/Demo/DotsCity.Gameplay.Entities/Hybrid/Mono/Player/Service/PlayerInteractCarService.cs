using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerInteractCarService : MonoBehaviour, IPlayerInteractCarService
    {
        public bool EnterCar(GameObject car, GameObject enteredNpc, Action<GameObject> onEnter = null)
        {
            bool entered = false;
            var carslots = car.GetComponent<ICarSlots>();

            if (carslots != null)
            {
                var idProvider = enteredNpc.GetComponent<INpcIDProvider>();

                if (idProvider != null)
                {
                    var slot = carslots.EnterCar(idProvider.ID, enteredNpc, true, carSlot =>
                    {
                        if (car.TryGetComponent<IVehicleInput>(out var vehicleInput))
                        {
                            vehicleInput.SwitchEnabledState(true);
                        }

                        onEnter?.Invoke(carSlot.EnteredSourceNpc);
                    });

                    entered = slot;
                }
                else
                {
                    UnityEngine.Debug.LogError(
                        "PlayerInteractCarService. The car can't be entered. INpcIDProvider not found. Add 'NpcCustomIDComponent' component to your character & enter id according to PlayerNpcCarFactory");
                    return false;
                }
            }

            return entered;
        }

        public void ExitCar(GameObject car, Action<GameObject> onExit = null)
        {
            if (car.TryGetComponent<IVehicleInput>(out var vehicleInput))
            {
                vehicleInput.SwitchEnabledState(false);
            }

            var carslots = car.GetComponent<ICarSlots>();

            if (onExit == null)
            {
                carslots.ExitCarAll(true);
            }
            else
            {
                carslots.ExitCarAll(true, (npc, driver) =>
                {
                    onExit(npc);
                });
            }
        }
    }
}