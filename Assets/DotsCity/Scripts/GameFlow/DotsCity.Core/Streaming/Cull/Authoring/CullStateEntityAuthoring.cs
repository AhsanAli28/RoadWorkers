using Spirit604.Attributes;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    public class CullStateEntityAuthoring : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#cull-config")]
        [SerializeField] private string link;

        [SerializeField] private bool addCullComponents = true;

        [ShowIf(nameof(addCullComponents))]
        [SerializeField] private CullStateList cullStateList;

        [Tooltip("Override cull config for this entity")]
        [SerializeField] private bool overrideCullConfig;

        [ShowIf(nameof(overrideCullConfig))]
        [SerializeField] private CullMethod cullMethod;

        [ShowIf(nameof(overrideCullConfig))]
        [SerializeField] private bool ignoreY;

        [ShowIf(nameof(overrideCullConfig))]
        [SerializeField] private bool showDebug;

        [ShowIf(nameof(overrideCullConfig))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float maxDistance = 65f;

        [ShowIf(nameof(PreInit))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float preinitDistance = 0;

        [ShowIf(nameof(overrideCullConfig))]
        [OnValueChanged(nameof(ValidateVisibleDistance))]
        [Tooltip("Distance to activate visual features of entities")]
        [SerializeField][Range(0, 9999)] private float visibleDistance = 26f;

        [ShowIf(nameof(CameraMethod))]
        [SerializeField][Range(1, 2)] private float viewPortSquareSize = 1.1f;

        [ShowIf(nameof(CameraMethod))]
        [SerializeField] private bool overrideBehindCamera;

        [ShowIf(nameof(CameraBehindDistances))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Behind camera, maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float behindMaxDistance = 65f;

        [ShowIf(nameof(CameraPreInit))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Behind camera, maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float behindPreinitDistance = 0;

        [ShowIf(nameof(CameraBehindDistances))]
        [OnValueChanged(nameof(ValidateVisibleDistance))]
        [Tooltip("Behind camera, distance to activate visual features of entities")]
        [SerializeField][Range(0, 9999)] private float behindVisibleDistance = 26f;

        private bool PreInit => cullStateList == CullStateList.PreInit && overrideCullConfig;
        private bool ShowDebug => showDebug && overrideCullConfig;

        private bool CameraMethod => overrideCullConfig && cullMethod == CullMethod.CameraView;
        private bool CameraBehindDistances => overrideBehindCamera && CameraMethod;
        private bool CameraPreInit => PreInit && CameraBehindDistances;

        class CullSharedConfigEntityAuthoringBaker : Baker<CullStateEntityAuthoring>
        {
            public override void Bake(CullStateEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.addCullComponents)
                    AddComponent(entity, CullComponentsExtension.GetComponentSet(authoring.cullStateList));

                if (authoring.overrideCullConfig)
                {
                    if (!authoring.CameraMethod)
                    {
                        AddCullSharedConfig(authoring, entity);
                    }
                    else
                    {
                        AddCameraCullSharedConfig(authoring, entity);
                    }
                }
            }

            private void AddCullSharedConfig(CullStateEntityAuthoring authoring, Entity entity)
            {
                CullSharedConfig sharedConfig;

                sharedConfig.IgnoreY = authoring.ignoreY;

                float preinitDistance = authoring.preinitDistance;

                if (preinitDistance == 0)
                {
                    preinitDistance = Mathf.Clamp(authoring.visibleDistance * 1.3f, 0, authoring.maxDistance);
                }

                sharedConfig.MaxDistanceSQ = authoring.maxDistance * authoring.maxDistance;
                sharedConfig.VisibleDistanceSQ = authoring.visibleDistance * authoring.visibleDistance;
                sharedConfig.PreinitDistanceSQ = preinitDistance * preinitDistance;

                AddSharedComponent(entity, sharedConfig);
            }

            private void AddCameraCullSharedConfig(CullStateEntityAuthoring authoring, Entity entity)
            {
                CullCameraSharedConfig sharedConfig;

                sharedConfig.IgnoreY = authoring.ignoreY;

                float preinitDistance = authoring.preinitDistance;

                if (preinitDistance == 0)
                {
                    preinitDistance = Mathf.Clamp(authoring.visibleDistance * 1.3f, 0, authoring.maxDistance);
                }

                sharedConfig.MaxDistanceSQ = authoring.maxDistance * authoring.maxDistance;
                sharedConfig.VisibleDistanceSQ = authoring.visibleDistance * authoring.visibleDistance;
                sharedConfig.PreinitDistanceSQ = preinitDistance * preinitDistance;

                sharedConfig.ViewPortOffset = authoring.viewPortSquareSize - 1;

                if (!authoring.overrideBehindCamera)
                {
                    sharedConfig.BehindMaxDistanceSQ = sharedConfig.MaxDistanceSQ;
                    sharedConfig.BehindVisibleDistanceSQ = 0;
                    sharedConfig.BehindPreinitDistanceSQ = sharedConfig.PreinitDistanceSQ;
                }
                else
                {
                    sharedConfig.BehindMaxDistanceSQ = authoring.behindMaxDistance * authoring.behindMaxDistance;
                    sharedConfig.BehindVisibleDistanceSQ = authoring.behindVisibleDistance * authoring.behindVisibleDistance;
                    sharedConfig.BehindPreinitDistanceSQ = authoring.behindPreinitDistance * authoring.behindPreinitDistance;
                }

                if (sharedConfig.BehindPreinitDistanceSQ == 0)
                {
                    sharedConfig.BehindPreinitDistanceSQ = Mathf.Clamp(sharedConfig.BehindVisibleDistanceSQ * 1.3f * 1.3f, 0, sharedConfig.BehindMaxDistanceSQ);
                }

                AddSharedComponent(entity, sharedConfig);
            }
        }

        private void ValidateMaxDistance()
        {
            if (maxDistance < visibleDistance)
            {
                visibleDistance = maxDistance;
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void ValidateVisibleDistance()
        {
            if (maxDistance < visibleDistance)
            {
                maxDistance = visibleDistance;
                EditorSaver.SetObjectDirty(this);
            }
        }


#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!ShowDebug)
                return;

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(transform.position, visibleDistance);

            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }

#endif
    }
}