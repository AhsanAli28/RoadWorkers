using System.Collections;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public interface IPlayerSpawnerService
    {
        void Initialize();
        IEnumerator Spawn();
    }
}