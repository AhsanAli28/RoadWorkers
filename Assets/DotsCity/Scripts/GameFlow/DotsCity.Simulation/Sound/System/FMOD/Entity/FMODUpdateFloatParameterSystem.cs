#if FMOD
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class FMODUpdateFloatParameterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithChangeFilter<FMODFloatParameter>()
            .ForEach((
                Entity entity,
                DynamicBuffer<FMODFloatParameter> fmodFloatParameters,
                in FMODSound fMODSound,
                in DynamicBuffer<FloatParameter> parameters) =>
            {
                var length = math.min(fmodFloatParameters.Length, parameters.Length);

                for (int i = 0; i < length; i++)
                {
                    var floatParameter = parameters[i];
                    var fmodFloatParameter = fmodFloatParameters[i];

                    if (fmodFloatParameter.CurrentValue == floatParameter.Value)
                    {
                        continue;
                    }

                    fMODSound.Event.setParameterByID(fmodFloatParameter.ParameterId, floatParameter.Value);

                    fmodFloatParameter.CurrentValue = floatParameter.Value;
                    fmodFloatParameters[i] = fmodFloatParameter;
                }

            }).Run();
        }
    }
}
#endif