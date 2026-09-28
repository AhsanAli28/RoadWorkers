using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;

#if !LEGACY_GIZMOS && UNITY_COLLECTIONS
using UGizmo;
#endif

namespace Spirit604.Extensions
{
    public static class GizmosDrawer
    {
        private const string UNITY_EDITOR = "UNITY_EDITOR";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(UNITY_EDITOR)]
        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
#if LEGACY_GIZMOS || !UNITY_COLLECTIONS
            var prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(from, to);
            Gizmos.color = prevColor;
#else
            UGizmos.DrawLine(from, to, color);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(UNITY_EDITOR)]
        public static void DrawSphere(Vector3 pos, float radius, Color color)
        {
#if LEGACY_GIZMOS || !UNITY_COLLECTIONS
            var prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(pos, radius);
            Gizmos.color = prevColor;
#else
            UGizmos.DrawSphere(pos, radius, color);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(UNITY_EDITOR)]
        public static void DrawArrow(Vector3 start, Vector3 direction, Color color, float thickness = 0.7f, float arrowLength = 3f, float arrowWidth = 0.8f, float arrowAngle = 40f)
        {
#if LEGACY_GIZMOS || !UNITY_COLLECTIONS
            DebugLine.DrawArrow(start, direction, color, thickness, arrowLength, arrowWidth, arrowAngle);
#else
            UGizmos.DrawArrow2d(start, start + direction * arrowLength, Vector3.up, color, arrowLength, arrowWidth);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(UNITY_EDITOR)]
        public static void DrawWireSphere(Vector3 position, float radius, Color color)
        {
#if LEGACY_GIZMOS || !UNITY_COLLECTIONS
            var prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(position, radius);
            Gizmos.color = prevColor;
#else
            UGizmos.DrawWireSphere(position, radius, color);
#endif          
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional(UNITY_EDITOR)]
        public static void DrawLineList(ReadOnlySpan<Vector3> points, Color color)
        {
            var prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLineList(points);
            Gizmos.color = prevColor;
        }
    }
}
