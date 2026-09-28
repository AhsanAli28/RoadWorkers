using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Core.Initialization
{
    [UpdateInGroup(typeof(CullSimulationGroup))]
    public partial class CullInitializerSystem : SystemBase
    {
        private EntityQuery cullSharedEntities1;
        private EntityQuery cullSharedEntities2;
        private EntityQuery cullSharedEntities3;
        private EntityQuery cullSharedEntities4;
        private bool initCamera;

        protected override void OnCreate()
        {
            cullSharedEntities1 = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullSharedConfig, Prefab>()
                .WithAbsent<PreInitInCameraTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            cullSharedEntities2 = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullSharedConfig, Prefab>()
                .WithPresent<PreInitInCameraTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            cullSharedEntities3 = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullCameraSharedConfig, Prefab>()
                .WithAbsent<PreInitInCameraTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            cullSharedEntities4 = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullCameraSharedConfig, Prefab>()
                .WithPresent<PreInitInCameraTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate() { }

        public void Launch()
        {
            var cullSystemConfig = SystemAPI.GetSingleton<CullSystemConfigReference>();

            var hasCull = cullSystemConfig.Config.Value.HasCull;

            if (hasCull)
            {
                var method = cullSystemConfig.Config.Value.CullMethod;

                switch (method)
                {
                    case CullMethod.CalculateDistance:
                        DefaultWorldUtils.SwitchActiveUnmanagedSystem<CalcCullingSystem>(true);
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCullingPreinitSystem, CullSimulationGroup>();
                        break;
                    case CullMethod.CameraView:
                        InitCamera();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCameraCullingSystem, CullSimulationGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCameraCullingPreinitSystem, CullSimulationGroup>();
                        break;
                }

                if (cullSharedEntities1.CalculateEntityCount() > 0)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCustomCullingSystem, CullSimulationGroup>();
                }

                if (cullSharedEntities2.CalculateEntityCount() > 0)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCustomCullingPreinitSystem, CullSimulationGroup>();
                }

                if (cullSharedEntities3.CalculateEntityCount() > 0)
                {
                    InitCamera();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCameraCustomCullingSystem, CullSimulationGroup>();
                }

                if (cullSharedEntities4.CalculateEntityCount() > 0)
                {
                    InitCamera();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalcCameraCustomCullingPreinitSystem, CullSimulationGroup>();
                }
            }
        }

        private void InitCamera()
        {
            if (initCamera)
                return;

            initCamera = true;
            DefaultWorldUtils.SwitchActiveManagedSystem<InitCameraCullingSystem>(true);
        }
    }
}
