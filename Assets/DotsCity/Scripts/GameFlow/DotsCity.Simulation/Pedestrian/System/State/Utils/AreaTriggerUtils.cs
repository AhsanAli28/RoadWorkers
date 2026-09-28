using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class AreaTriggerUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTrigger(
            ref EntityCommandBuffer commandBuffer,
            in TriggerConfigReference Config,
            Entity entity,
            in AreaTriggerPlaybackSystem.AreaTriggerInfo triggerInfo)
        {
            switch (triggerInfo.TriggerAreaType)
            {
                case TriggerAreaType.FearPointTrigger:
                    {
                        var processScaryRunningComponent = new ProcessScaryRunningTag()
                        {
                            TriggerPosition = triggerInfo.TriggerPosition
                        };

                        commandBuffer.AddComponent(entity, processScaryRunningComponent);
                        commandBuffer.AddComponent<ScaryRunningTag>(entity);
                        commandBuffer.SetComponentEnabled<ScaryRunningTag>(entity, false);
                        break;
                    }
            }

            var id = (int)triggerInfo.TriggerAreaType;

            ref var triggerDataConfigs = ref Config.Config.Value.TriggerDataConfigs;
            var duration = 0f;

            if (triggerDataConfigs.Length > id)
            {
                duration = triggerDataConfigs[id].ImpactTriggerDuration;
            }

            commandBuffer.AddComponent<HasImpactTriggerTag>(entity);

            if (duration > 0)
            {
                commandBuffer.AddComponent(entity, new ImpactTriggerData()
                {
                    Duration = duration
                });
            }
        }
    }
}