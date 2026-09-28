using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class DebugLine
    {
#if UNITY_EDITOR
        private static Vector3[] linePoints = new Vector3[2];
        private static Camera cam;

        public static Camera Cam
        {
            get
            {
                if (cam == null)
                {
                    cam = Camera.current;
                }

                return cam;
            }
        }
#endif


#if UNITY_EDITOR

#endif
        public static void DrawThickLine(Vector3 start, Vector3 end, float thickness, Color color, bool allowOnlyVisibleEnd = false, bool zTest = false, bool checkCameraView = true)
        {
#if UNITY_EDITOR

            if (checkCameraView)
            {
                var cam = Cam;

                if (cam == null) return;

                //// Only draw on normal cameras
                if (cam.clearFlags == CameraClearFlags.Depth || cam.clearFlags == CameraClearFlags.Nothing)
                {
                    return;
                }

                if (!allowOnlyVisibleEnd)
                {
                    if (!cam.InViewOfCamera(start))
                    {
                        return;
                    }
                }
                else
                {
                    if (!cam.InViewOfCamera(start) && !cam.InViewOfCamera(end))
                    {
                        return;
                    }
                }
            }

            // Only draw the line when it is the closest thing to the camera
            // (Remove the Z-test code and other objects will not occlude the line.)
            var prevZTest = Handles.zTest;

            if (zTest)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            }

            Handles.color = color;

            linePoints[0] = start;
            linePoints[1] = end;

            Handles.DrawAAPolyLine(thickness * 10, linePoints);

            Handles.zTest = prevZTest;
#endif
        }

        public static void DrawSlimArrow(Vector3 startPoint, Quaternion rotation, Color color, float arrowLength = 3f, float arrowWidth = 0.8f, float arrowAngle = 40f)
        {
            var endPoint = startPoint + rotation * Vector3.forward * arrowLength;

            Debug.DrawLine(startPoint, endPoint, color);

            Vector3 direction = (startPoint - endPoint).normalized;

            Vector3 rightPoint = endPoint + Quaternion.Euler(0, arrowAngle, 0) * direction * arrowWidth;
            Vector3 leftPoint = endPoint + Quaternion.Euler(0, -arrowAngle, 0) * direction * arrowWidth;

            Debug.DrawLine(endPoint, rightPoint, color);
            Debug.DrawLine(endPoint, leftPoint, color);
        }

        public static void DrawSlimArrow(Vector3 start, Vector3 direction, Color color, float arrowLength = 3f, float arrowWidth = 0.8f, float arrowAngle = 40f)
        {
            DrawSlimArrow(start, Quaternion.LookRotation(direction, Vector3.up), color, arrowLength, arrowWidth, arrowAngle);
        }

        public static void DrawArrow(Vector3 startPoint, Quaternion rotation, Color color, float thickness = 0.7f, float arrowLength = 3f, float arrowWidth = 0.8f, float arrowAngle = 40f)
        {
#if UNITY_EDITOR
            var cam = Cam;
            if (!cam || !cam.InViewOfCamera(startPoint) || cam.clearFlags == CameraClearFlags.Depth || cam.clearFlags == CameraClearFlags.Nothing)
                return;
#endif

            var endPoint = startPoint + rotation * Vector3.forward * arrowLength;

            DrawThickLine(startPoint, endPoint, thickness, color, checkCameraView: false);

            Vector3 direction = (startPoint - endPoint).normalized;

            Vector3 rightPoint = endPoint + Quaternion.Euler(0, arrowAngle, 0) * direction * arrowWidth;
            Vector3 leftPoint = endPoint + Quaternion.Euler(0, -arrowAngle, 0) * direction * arrowWidth;

            DrawThickLine(endPoint, rightPoint, thickness, color, checkCameraView: false);
            DrawThickLine(endPoint, leftPoint, thickness, color, checkCameraView: false);
        }

        public static void DrawArrow(Vector3 start, Vector3 direction, Color color, float thickness = 0.7f, float arrowLength = 3f, float arrowWidth = 0.8f, float arrowAngle = 40f)
        {
            DrawArrow(start, direction != Vector3.zero ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity, color, thickness, arrowLength, arrowWidth, arrowAngle);
        }
    }
}
