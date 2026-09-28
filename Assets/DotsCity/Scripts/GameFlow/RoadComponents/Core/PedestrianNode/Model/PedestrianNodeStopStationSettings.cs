using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class PedestrianNodeStopStationSettings : MonoBehaviour
    {
        public const float MIN_IDLE_TIME = 40f;
        public const float MAX_IDLE_TIME = 60f;

        [MinMaxSlider(0f, 400f)]
        [SerializeField] private Vector2 idleDuration = new Vector2(MIN_IDLE_TIME, MAX_IDLE_TIME);

        public Vector2 IdleDuration { get => idleDuration; set => idleDuration = value; }
    }
}
