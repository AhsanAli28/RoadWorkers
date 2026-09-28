using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay
{
    public abstract class MonoCarConverterBase : IMonoCarConverter
    {
        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        /// <summary>
        /// Removes traffic car components & adds player car components.
        /// </summary>
        /// <param name="enteredCar"></param>
        public virtual bool EnterCar(GameObject enteredCar, CarType newType)
        {
            if (!enteredCar.TryGetComponent<CarEntityAdapter>(out var adapter))
                return false;

            ConvertCar(enteredCar, adapter, newType);
            return true;
        }

        public virtual void ExitCar(GameObject exitCar, CarType newType) { }

        /// <summary>
        /// Custom user logic to convert from traffic car to player car.
        /// </summary>
        protected abstract void InitCustomComponents(GameObject enteredCar, int carModel, CarType newType);

        protected virtual void DestroyTrafficComponents(GameObject enteredCar, CarEntityAdapter adapter)
        {
            EntityManager.RemoveComponent<MonoAdapterComponent>(adapter.RelatedEntity);

            adapter.Destroyed = true;
            adapter.DestroyEntityImmediate();
            GameObject.Destroy(adapter);

            TryToDestroyComponent<PhysicsSwitcher>(enteredCar);
            TryToDestroyComponent<ScriptSwitcher>(enteredCar);
            TryToDestroyComponent<PoolableObjectInfo>(enteredCar);

            DestroyInput(enteredCar);
        }

        protected virtual void DestroyInput(GameObject enteredCar) => TryToDestroyComponent(enteredCar, typeof(IVehicleInput));

        private void ConvertCar(GameObject enteredCar, CarEntityAdapter adapter, CarType carType)
        {
            var carModelComponent = EntityManager.GetComponentData<CarModelComponent>(adapter.RelatedEntity);

            DestroyTrafficComponents(enteredCar, adapter);

            InitCustomComponents(enteredCar, carModelComponent.Value, carType);
        }

        protected void TryToDestroyComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out var comp)) GameObject.Destroy(comp);
        }

        protected void TryToDestroyComponent(GameObject go, Type type)
        {
            if (type != null && go.TryGetComponent(type, out var comp)) GameObject.Destroy(comp);
        }
    }
}