using Unity.Entities;

namespace Spirit604.DotsCity.Events
{
    public interface ITriggerNodeListener
    {
        void RaiseEvent(Entity entity, Entity nodeEntity, TriggerNodeType triggerNodeType);
    }
}
