using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public interface IMonoCarConverter
    {
        bool EnterCar(GameObject oldCar, CarType newType);
        void ExitCar(GameObject exitCar, CarType newType);
    }
}