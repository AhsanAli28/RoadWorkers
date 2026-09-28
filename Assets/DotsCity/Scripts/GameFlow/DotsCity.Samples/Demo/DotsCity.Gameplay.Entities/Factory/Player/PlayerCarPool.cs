using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Authoring;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class PlayerCarPool : TrafficCarPoolBase
    {
        protected override int PoolSize => 3;

        [OnValueChanged(nameof(OnPresetChanged))]
        [SerializeField] private TrafficCarPoolGlobal trafficCarPoolGlobal;

        private BlobAssetReference<PlayerCarCollectionBlob> blobRef;

        private void OnDestroy()
        {
            if (blobRef.IsCreated)
            {
                blobRef.Dispose();
                blobRef = default;
            }
        }

        public void Initialize()
        {
            var tempList = new NativeList<int>(Allocator.TempJob);

            if (CarPrefabs != null && VehicleDataCollection)
            {
                foreach (var prefabData in CarPrefabs)
                {
                    if (prefabData.EntityPrefab == null)
                        continue;

                    var carProvider = prefabData.EntityPrefab.GetComponent<ICarIDProvider>();

                    if (carProvider != null)
                    {
                        var index = VehicleDataCollection.GetCarModelIndexByID(carProvider.ID);

                        if (index != -1)
                        {
                            tempList.Add(index);
                        }
                    }
                }
            }

            var entityMananager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var configEntity = entityMananager.CreateEntity(typeof(PlayerCarCollectionReference));

            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PlayerCarCollectionBlob>();
                var availableIds = builder.Allocate(ref root.AvailableIds, tempList.Length);

                for (int i = 0; i < tempList.Length; i++)
                {
                    availableIds[i] = tempList[i];
                }

                blobRef = builder.CreateBlobAssetReference<PlayerCarCollectionBlob>(Allocator.Persistent);

                entityMananager.SetComponentData(configEntity, new PlayerCarCollectionReference()
                {
                    Config = blobRef
                });
            }

            tempList.Dispose();
        }

        private void OnPresetChanged()
        {
            var authorings = ObjectUtils.FindObjectsOfType<PlayerCarPrefabAuthoring>();

            for (int i = 0; i < authorings?.Length; i++)
            {
                authorings[i].PlayerCarPoolPreset = CarPoolPreset;
            }
        }
    }
}