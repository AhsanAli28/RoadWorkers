using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Bootstrap
{
    public class PlayerSpawnCommand : BootstrapCoroutineCommandBase
    {
        private readonly EntityManager entityManager;
        private readonly WorldUnmanaged worldUnmanaged;
        private readonly IPlayerSpawnerService playerSpawner;

        public PlayerSpawnCommand(EntityManager entityManager, WorldUnmanaged worldUnmanaged, IPlayerSpawnerService playerSpawner, MonoBehaviour source) : base(source)
        {
            this.entityManager = entityManager;
            this.worldUnmanaged = worldUnmanaged;
            this.playerSpawner = playerSpawner;
        }

        protected override IEnumerator InternalRoutine()
        {
            playerSpawner.Initialize();
            yield return playerSpawner.Spawn();
        }
    }
}