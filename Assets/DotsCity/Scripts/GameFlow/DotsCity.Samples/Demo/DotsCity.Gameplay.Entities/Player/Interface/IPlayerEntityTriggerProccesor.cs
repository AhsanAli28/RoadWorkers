using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public interface IPlayerEntityTriggerProccesor
    {
        bool TriggerIsBlocked { get; set; }

        void ProcessTrigger(Entity triggerEntity);
        void ProcessExitTrigger();
    }
}