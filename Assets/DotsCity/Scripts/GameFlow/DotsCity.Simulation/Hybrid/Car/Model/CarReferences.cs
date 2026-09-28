using Spirit604.DotsCity.Simulation.Car;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public class CarReferences : MonoBehaviour, IVehicleEntityRef
    {
        public Entity RelatedEntity { get; private set; }

        public bool HasEntity => RelatedEntity != Entity.Null;

        public event Action<Entity> OnEntityInitialized = delegate { };

        public void Initialize(Entity entity)
        {
            RelatedEntity = entity;
            OnEntityInitialized.Invoke(entity);
        }

        public void DestroyEntity()
        {
        }
    }
}
