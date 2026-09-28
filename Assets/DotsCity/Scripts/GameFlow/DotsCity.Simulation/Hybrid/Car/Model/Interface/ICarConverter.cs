using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public interface ICarConverter
    {
        Entity Convert(ref EntityCommandBuffer commandBuffer, Entity oldEntity, CarType newType);
    }
}