using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class CameraExtension
    {
        public static bool InViewOfCamera(this Camera camera, Vector3 position, float additionalOffset = 0)
        {
            Vector2 viewPort = camera.WorldToViewportPoint(position);

            return viewPort.x >= 0 - additionalOffset && viewPort.x <= 1 + additionalOffset && viewPort.y >= 0 - additionalOffset && viewPort.y <= 1 + additionalOffset;
        }

#if UNITY_EDITOR
        public static bool InViewOfSceneView(Vector3 position, float additionalOffset = 0)
        {
            var lastActiveSceneView = SceneView.lastActiveSceneView;

            if (lastActiveSceneView)
            {
                var camera = lastActiveSceneView.camera;
                return camera.InViewOfCamera(position, additionalOffset);
            }

            return false;
        }
#endif
    }
}
