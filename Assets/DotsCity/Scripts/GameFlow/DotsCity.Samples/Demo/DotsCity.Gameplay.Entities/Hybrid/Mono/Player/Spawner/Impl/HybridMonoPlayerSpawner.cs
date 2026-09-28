using Spirit604.DotsCity.Gameplay.Npc.Factory.Player;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.Factory.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class HybridMonoPlayerSpawner : PlayerSpawnerBase
    {
        private readonly PlayerHybridMonoNpcFactory playerHybridMonoNpcFactory;
        private readonly PlayerCarSpawner playerCarSpawner;

        public HybridMonoPlayerSpawner(
            IPlayerNpcFactory playerHybridMonoNpcFactory,
            PlayerCarSpawner playerCarSpawner)
        {
            this.playerHybridMonoNpcFactory = playerHybridMonoNpcFactory as PlayerHybridMonoNpcFactory;
            this.playerCarSpawner = playerCarSpawner;
        }

        public override void Initialize()
        {
            playerHybridMonoNpcFactory.GeneratePool();
        }

        public override GameObject Spawn(PlayerSpawnDataConfig playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            GameObject playerObject = null;

            if (playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Npc)
            {
                playerObject = playerHybridMonoNpcFactory.Get(playerSpawnDataConfig.SelectedNpcID, spawnPosition, spawnRotation);

                if (!playerObject)
                    return null;

                playerObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }
            else
            {
                playerObject = playerCarSpawner.Spawn(playerSpawnDataConfig.SelectedCarModel, spawnPosition, spawnRotation, true);

                var carSlots = playerObject.GetComponent<ICarSlots>();

                if (carSlots != null)
                {
                    var npc = playerHybridMonoNpcFactory.Get(playerSpawnDataConfig.SelectedNpcID, spawnPosition, spawnRotation);

                    if (!npc)
                        return null;

                    carSlots.EnterCar(playerSpawnDataConfig.SelectedNpcID, npc, driver: true);
                }
            }

            return playerObject;
        }
    }
}