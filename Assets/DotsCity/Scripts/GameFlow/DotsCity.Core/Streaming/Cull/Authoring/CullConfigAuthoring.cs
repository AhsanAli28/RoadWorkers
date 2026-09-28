using Spirit604.Attributes;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    public class CullConfigAuthoring : RuntimeConfigUpdater<CullSystemConfigReference, CullSystemConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#cull-config")]
        [SerializeField] private string link;

        [Expandable]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private CullConfig cullConfig;

#if UNITY_EDITOR
        [ShowIf(nameof(HasCull))]
        [SerializeField] private bool showDebug = true;
#endif

        public CullConfig CullConfig { get => cullConfig; set => cullConfig = value; }

        private bool HasCull => cullConfig != null ? cullConfig.HasCull : false;

        public override CullSystemConfigReference CreateConfig(BlobAssetReference<CullSystemConfig> blobRef)
        {
            return new CullSystemConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<CullSystemConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CullSystemConfig>();

                root.HasCull = cullConfig.HasCull;
                root.IgnoreY = cullConfig.IgnoreY;

                float preinitDistance = cullConfig.PreinitDistance;

                if (preinitDistance == 0)
                {
                    preinitDistance = Mathf.Clamp(root.VisibleDistance * 1.3f, 0, root.MaxDistance);
                }

                root.CullMethod = cullConfig.CurrentCullMethod;
                root.MaxDistance = cullConfig.MaxDistance;
                root.VisibleDistance = cullConfig.VisibleDistance;
                root.PreinitDistance = preinitDistance;
                root.MaxDistanceSQ = root.MaxDistance * root.MaxDistance;
                root.VisibleDistanceSQ = root.VisibleDistance * root.VisibleDistance;
                root.PreinitDistanceSQ = preinitDistance * preinitDistance;

                if (cullConfig.CurrentCullMethod == CullMethod.CameraView)
                {
                    root.ViewPortOffset = cullConfig.ViewPortSquareSize - 1;

                    if (!cullConfig.OverrideBehindCameraDistances)
                    {
                        root.BehindMaxDistanceSQ = root.MaxDistanceSQ;
                        root.BehindVisibleDistanceSQ = 0;
                        root.BehindPreinitDistanceSQ = root.PreinitDistanceSQ;
                    }
                    else
                    {
                        root.BehindMaxDistanceSQ = cullConfig.BehindMaxDistance * cullConfig.BehindMaxDistance;
                        root.BehindVisibleDistanceSQ = cullConfig.BehindVisibleDistance * cullConfig.BehindVisibleDistance;
                        root.BehindPreinitDistanceSQ = cullConfig.BehindPreinitDistance * cullConfig.BehindPreinitDistance;
                    }

                    if (root.BehindPreinitDistanceSQ == 0)
                    {
                        root.BehindPreinitDistanceSQ = Mathf.Clamp(root.BehindVisibleDistanceSQ * 1.3f * 1.3f, 0, root.BehindMaxDistanceSQ);
                    }
                }

                var blobRef = builder.CreateBlobAssetReference<CullSystemConfig>(Unity.Collections.Allocator.Persistent);

                return blobRef;
            }
        }

        class CullConfigAuthoringBaker : Baker<CullConfigAuthoring>
        {
            public override void Bake(CullConfigAuthoring authoring)
            {
                if (authoring.cullConfig == null)
                {
                    Debug.LogError("Cull config not assigned!");
                    return;
                }

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(entity, authoring.CreateConfig(this));
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!showDebug || cullConfig == null)
            {
                return;
            }

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(transform.position, cullConfig.VisibleDistance);

            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(transform.position, cullConfig.MaxDistance);
        }

#endif
    }
}