using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianSettingsConfigAuthoring : RuntimeConfigUpdater<PedestrianSettingsReference, PedestrianSettings>
    {
        [ShowIf(nameof(SettingsIsNull))]
        [SerializeField] private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;

        [ShowIf(nameof(GeneralSettingsIsNull))]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        private IPedestrianSkinInfoProvider pedestrianSkinInfoProvider;

        [InjectWrapper]
        public void Construct(IPedestrianSkinInfoProvider pedestrianSkinInfoProvider)
        {
            this.pedestrianSkinInfoProvider = pedestrianSkinInfoProvider;
        }

        private bool SettingsIsNull => pedestrianSpawnerConfigHolder == null;

        private bool GeneralSettingsIsNull => citySettingsInitializer == null;

        protected override bool AutoSync => false;

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(true);
        }

        public override PedestrianSettingsReference CreateConfig(BlobAssetReference<PedestrianSettings> blobRef)
        {
            return new PedestrianSettingsReference()
            {
                Config = blobRef
            };
        }

        protected override BlobAssetReference<PedestrianSettings> CreateConfigBlob()
        {
            return CreateConfigBlob(pedestrianSpawnerConfigHolder.PedestrianSettingsConfig, citySettingsInitializer.GetSettings<GeneralSettingDataSimulation>(), pedestrianSkinInfoProvider);
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().UpdateSettingsConfig(true);
        }

        public BlobAssetReference<PedestrianSettings> CreateConfigBlob(PedestrianSettingsConfig config, GeneralSettingDataSimulation generalSettingData, IPedestrianSkinInfoProvider pedestrianSkinInfoProvider)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PedestrianSettings>();

                root.MinWalkingSpeed = config.WalkingSpeed.x;
                root.MaxWalkingSpeed = config.WalkingSpeed.y;
                root.MinRunningSpeed = config.RunningSpeed.x;
                root.MaxRunningSpeed = config.RunningSpeed.y;
                root.RotationSpeed = config.RotationSpeed;
                root.ColliderRadius = config.ColliderRadius;
                root.Health = generalSettingData.HealthSystemSupport ? config.Health : 0;
                root.NavigationType = config.PedestrianNavigationType;
                root.SkinType = config.PedestrianSkinType;
                root.EntityType = config.PedestrianEntityType;
                root.RigType = config.PedestrianRigType;
                root.HasRig = config.HasRig;
                root.LerpRotation = config.LerpRotation;
                root.LerpRotationInView = config.LerpRotationInView;
                root.MaxSkinIndex = pedestrianSkinInfoProvider != null ? pedestrianSkinInfoProvider.SkinCount : 0;
                root.AlignYAxis = config.AlignYAxis;
                root.YAxisLerpSpeed = config.YAxisLerpSpeed;

                return builder.CreateBlobAssetReference<PedestrianSettings>(Unity.Collections.Allocator.Persistent);
            }
        }

        public class PedestrianSettingsConfigAuthoringBaker : Baker<PedestrianSettingsConfigAuthoring>
        {
            public override void Bake(PedestrianSettingsConfigAuthoring authoring)
            {
                if (authoring.SettingsIsNull || authoring.GeneralSettingsIsNull)
                {
                    return;
                }

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                var blobRef = authoring.CreateConfigBlob(authoring.pedestrianSpawnerConfigHolder.PedestrianSettingsConfig, authoring.citySettingsInitializer.GetSettings<GeneralSettingDataSimulation>(), null);

                AddBlobAsset(ref blobRef, out var hash);

                AddComponent(entity, new PedestrianSettingsReference()
                {
                    Config = blobRef
                });
            }
        }
    }
}
