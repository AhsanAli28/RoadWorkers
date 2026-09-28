using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public static class ShaderUtils
    {
        public static void AddShaderComponents(ref EntityCommandBuffer commandBuffer, Entity entity, bool includeTransition = false)
        {
            commandBuffer.AddComponent(entity, new ShaderPlaybackTime()
            {
                Value = -1
            });

            if (includeTransition)
            {
                commandBuffer.AddComponent<ShaderTargetPlaybackTime>(entity);
                commandBuffer.AddComponent<ShaderTransitionTime>(entity);
                commandBuffer.AddComponent<ShaderTargetFrameStepInvData>(entity);
                commandBuffer.AddComponent<ShaderTargetFrameOffsetData>(entity);
            }
        }
    }
}
