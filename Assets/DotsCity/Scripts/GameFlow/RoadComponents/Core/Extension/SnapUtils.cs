using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public static class SnapUtils
    {
        public static void SnapToSurface(Transform sourceTransform, LayerMask snapLayerMask, float surfaceOffset = 0, bool recordUndo = true, bool includeRotation = false, GameObject customSnapObject = null)
        {
            RaycastHit raycastHit = default;

            var origin = sourceTransform.position + new Vector3(0, 100f);
            var direction = Vector3.down;
            var castDistance = Mathf.Infinity;

            var physicsScene = PhysicsSceneExtensions.GetPhysicsScene(sourceTransform.gameObject.scene);

            if (customSnapObject == null)
            {
                physicsScene.Raycast(origin, direction, out raycastHit, castDistance, snapLayerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                var hits = new RaycastHit[20];
                var count = physicsScene.Raycast(origin, direction, hits, castDistance, snapLayerMask, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    if (hits[i].collider != null && hits[i].collider.gameObject == customSnapObject)
                    {
                        raycastHit = hits[i];
                        break;
                    }
                }
            }

            if (raycastHit.point != default)
            {
                var newPosition = raycastHit.point + new Vector3(0, surfaceOffset, 0);

                if (sourceTransform.position != newPosition)
                {
                    if (recordUndo)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RecordObject(sourceTransform, "Edited Object Position");
#endif
                    }

                    if (!includeRotation)
                    {
                        sourceTransform.position = newPosition;
                    }
                    else
                    {
                        var dir = Vector3.Cross(raycastHit.normal, -sourceTransform.right);
                        var newRot = Quaternion.LookRotation(dir);

                        sourceTransform.SetPositionAndRotation(newPosition, newRot);
                    }
                }
            }
        }
    }
}
