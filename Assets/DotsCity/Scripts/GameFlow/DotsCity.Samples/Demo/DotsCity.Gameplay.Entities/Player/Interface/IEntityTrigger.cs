using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Level
{
    public interface IEntityTrigger
    {
        void Process(Entity triggerEntity);
    }
}