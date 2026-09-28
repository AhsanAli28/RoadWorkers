using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public interface IVehicleEntityRef : IHybridEntityRef
    {
        void Initialize(Entity entity);
    }
}
