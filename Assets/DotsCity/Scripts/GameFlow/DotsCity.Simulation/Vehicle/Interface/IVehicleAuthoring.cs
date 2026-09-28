using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public interface IVehicleAuthoring
    {
        public float WheelRadius { get; set; }
        public void AddWheel(GameObject newWheel);
        public void InsertWheel(GameObject newWheel, int index);
        public void AddSteeringWheel(GameObject newWheel);
        public void SetDirty();
    }
}