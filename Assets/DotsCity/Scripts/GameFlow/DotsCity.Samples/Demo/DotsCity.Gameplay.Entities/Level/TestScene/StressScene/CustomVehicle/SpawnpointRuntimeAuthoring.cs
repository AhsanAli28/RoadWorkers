using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class SpawnpointRuntimeAuthoring : RuntimeEntityConfig, IConfigInject
    {
        [SerializeField]
        private VehicleCustomStressUI vehicleCustomStressUI;

        [Expandable]
        [SerializeField]
        private VehicleCustomStressSpawnSettings spawnSettings;

        protected override bool HasCustomEntityArchetype => true;

        protected override EntityArchetype GetEntityArchetype() =>
            EntityManager.CreateArchetype(
                typeof(SpawnPointTag),
                typeof(SpawnPointSettings),
                typeof(LocalTransform),
                typeof(LocalToWorld));

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            if (!spawnSettings)
            {
                return;
            }

            dstManager.SetComponentData(entity, new SpawnPointSettings()
            {
                Rows = spawnSettings.Rows,
                CountPerRow = spawnSettings.CountPerRow,
                XOffset = spawnSettings.XOffset,
                ZOffset = spawnSettings.ZOffset
            });

            dstManager.SetComponentData(entity, LocalTransform.FromPosition(transform.position));

            vehicleCustomStressUI.SetCount(spawnSettings.Count);
        }

        public void InjectConfig(object config)
        {
            spawnSettings = config as VehicleCustomStressSpawnSettings;
            vehicleCustomStressUI.SetCount(spawnSettings.Count);
        }

        private void OnDrawGizmosSelected()
        {
            if (!spawnSettings)
            {
                return;
            }

            for (int z = 0; z < spawnSettings.Rows; z++)
            {
                for (int x = 0; x < spawnSettings.CountPerRow; x++)
                {
                    var position = transform.position + new Vector3(x * spawnSettings.XOffset, 0, z * spawnSettings.ZOffset);

                    Gizmos.DrawWireCube(position, Vector3.one);
                }
            }
        }
    }
}
