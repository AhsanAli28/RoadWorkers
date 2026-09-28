using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Grid display on the scene.
    /// </summary>
    public abstract class GridSceneViewBase : MonoBehaviour
    {
        public abstract void SetPlacingType(PlacingType newPlacingType);
        public abstract void SetPosition(Vector3 pos);
        public abstract void SetRemoveColor(bool isOverllaped);
    }
}
