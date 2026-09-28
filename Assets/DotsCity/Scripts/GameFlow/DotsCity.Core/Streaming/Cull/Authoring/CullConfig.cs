using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [CreateAssetMenu(fileName = "CullConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_COMMON_PATH + "CullConfig")]
    public class CullConfig : ScriptableObjectBase
    {
        [HideInView]
        [SerializeField] private bool hasCull = true;

        [SerializeField] private bool ignoreY;

        [ShowIf(nameof(hasCull))]
        [Tooltip("" +
            "<b>Calculate Distance</b> : culling based on distance\r\n\r\n" +
            "<b>Camera View</b> : culling based on distance to camera & view port\r\n\r\n" +
            "<b>Custom</b> : user's custom culling solution")]
        [SerializeField] private CullMethod cullMethod;

        [ShowIf(nameof(hasCull))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float maxDistance = 65f;

        [ShowIf(nameof(hasCull))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float preinitDistance = 0;

        [ShowIf(nameof(hasCull))]
        [OnValueChanged(nameof(ValidateVisibleDistance))]
        [Tooltip("Distance to activate visual features of entities")]
        [SerializeField][Range(0, 9999)] private float visibleDistance = 26f;

        [ShowIf(nameof(CameraMethod))]
        [SerializeField][Range(1, 2)] private float viewPortSquareSize = 1.2f;

        [ShowIf(nameof(CameraMethod))]
        [SerializeField] private bool overrideBehindCamera;

        [ShowIf(nameof(CameraBehindDistances))]
        [OnValueChanged(nameof(ValidateCameraMaxDistance))]
        [Tooltip("Behind camera, maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float behindMaxDistance = 65f;

        [ShowIf(nameof(CameraBehindDistances))]
        [OnValueChanged(nameof(ValidateMaxDistance))]
        [Tooltip("Behind camera, maximum distance to activate entities")]
        [SerializeField][Range(0, 9999)] private float behindPreinitDistance = 0;

        [ShowIf(nameof(CameraBehindDistances))]
        [OnValueChanged(nameof(ValidateCameraVisibleDistance))]
        [Tooltip("Behind camera, distance to activate visual features of entities")]
        [SerializeField][Range(0, 9999)] private float behindVisibleDistance = 26f;

        public bool HasCull { get => hasCull; }
        public bool IgnoreY { get => ignoreY; }
        public CullMethod CurrentCullMethod { get => cullMethod; set => cullMethod = value; }
        public float MaxDistance { get => maxDistance; }
        public float VisibleDistance { get => visibleDistance; }
        public float PreinitDistance { get => preinitDistance; }
        public float ViewPortSquareSize { get => viewPortSquareSize; set => viewPortSquareSize = value; }
        public bool OverrideBehindCameraDistances { get => overrideBehindCamera; set => overrideBehindCamera = value; }
        public float BehindMaxDistance { get => behindMaxDistance; set => behindMaxDistance = value; }
        public float BehindPreinitDistance { get => behindPreinitDistance; set => behindPreinitDistance = value; }
        public float BehindVisibleDistance { get => behindVisibleDistance; set => behindVisibleDistance = value; }

        private bool CameraMethod => hasCull && cullMethod == CullMethod.CameraView;
        private bool CameraBehindDistances => overrideBehindCamera && CameraMethod;

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

        private void ValidateCameraMaxDistance()
        {
            if (behindVisibleDistance > behindMaxDistance)
            {
                behindVisibleDistance = behindMaxDistance;
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void ValidateCameraVisibleDistance()
        {
            if (behindVisibleDistance > behindMaxDistance)
            {
                behindMaxDistance = behindVisibleDistance;
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}