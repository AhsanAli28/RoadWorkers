using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory.Npc;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class DefaultMonoPlayerCitySpawner : DefaultPlayerCitySpawner
    {
        public DefaultMonoPlayerCitySpawner(PlayerSession playerSession, PlayerCarSpawner playerCarSpawner, IPlayerEntityTriggerProccesor playerEntityTriggerProccesor, IPlayerNpcFactory playerNpcFactory, IPlayerMobNpcFactory playerMobNpcFactory) : base(playerSession, playerCarSpawner, playerEntityTriggerProccesor, playerNpcFactory, playerMobNpcFactory)
        {
        }

        protected override CarSlot AddToSlots(ICarSlots carSlots, string id, bool isAi)
        {
            var npc = GetNpc(id, isAi);

            return carSlots.EnterCar(id, npc, driver: !isAi);
        }
    }
}