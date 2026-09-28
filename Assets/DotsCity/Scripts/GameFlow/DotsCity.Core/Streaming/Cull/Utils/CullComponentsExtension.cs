using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public static class CullComponentsExtension
    {
        public static ComponentTypeSet GetComponentSet() => GetComponentSet(CullStateList.Default);

        public static ComponentTypeSet GetComponentSet(CullStateList cullStateList)
        {
            if (cullStateList == CullStateList.PreInit)
                return PreinitCullComponentSet;

            return CullComponentSet;
        }

        public static void InitStateQuery(ref EntityQueryBuilder builder, CullState cullState, bool addViewOfCameraComponent = false)
        {
            builder = builder.WithNone<CulledEventTag>();

            switch (cullState)
            {
                case CullState.InViewOfCamera:
                    {
                        if (addViewOfCameraComponent)
                        {
                            builder = builder.WithAny<InViewOfCameraTag>();
                        }

                        break;
                    }
                case CullState.PreInitInCamera:
                    builder =
                        builder
                        .WithAny<PreInitInCameraTag, InPermittedRangeTag>();
                    break;
                case CullState.CloseToCamera:
                    builder =
                       builder
                       .WithAny<InPermittedRangeTag>();
                    break;
            }
        }

        public static bool IsAvailable(CullState cullState, CullStateList cullStateList)
        {
            if (cullState == CullState.Culled || cullState == CullState.Uninitialized || cullState == CullState.PreInitInCamera && cullStateList != CullStateList.PreInit)
                return false;

            return true;
        }

        private static ComponentTypeSet CullComponentSet =>
            new ComponentTypeSet(
                new ComponentType[]
                {
                    typeof(CullStateBakingTag),
                    typeof(CullStateComponent),
                    typeof(InPermittedRangeTag),
                    typeof(InViewOfCameraTag),
                    typeof(CulledEventTag)
                });

        private static ComponentTypeSet PreinitCullComponentSet =>
            new ComponentTypeSet(
                new ComponentType[]
                {
                    typeof(CullStateBakingTag),
                    typeof(CullStateComponent),
                    typeof(InPermittedRangeTag),
                    typeof(InViewOfCameraTag),
                    typeof(PreInitInCameraTag),
                    typeof(CulledEventTag)
                });
    }
}