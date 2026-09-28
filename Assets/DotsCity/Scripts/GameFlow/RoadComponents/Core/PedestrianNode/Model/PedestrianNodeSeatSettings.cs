using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class PedestrianNodeSeatSettings : MonoBehaviour
    {
        public PedestrianNode pedestrianNode;
        public bool showSeats;

        [Tooltip("Offset between the center of the seats and the center of the node")]
        [SerializeField] private Vector3 baseOffset;

        [Tooltip("Distance offset between seats")]
        [SerializeField][Range(0.1f, 5f)] private float seatOffset = 0.4f;

        [Tooltip("Offset between the animation start position and the seat")]
        [SerializeField][Range(-5f, 5f)] private float enterSeatOffset = 0.4f;

        [Tooltip("Seat height")]
        [SerializeField][Range(-0.2f, 5f)] private float seatHeight = 0.6f;

        public Vector3 BaseOffset { get => baseOffset; set => baseOffset = value; }
        public float SeatOffset { get => seatOffset; }
        public float EnterSeatOffset { get => enterSeatOffset; }
        public float SeatHeight { get => seatHeight; }

        public void Reset()
        {
            if (!pedestrianNode)
            {
                pedestrianNode = GetComponent<PedestrianNode>();
                EditorSaver.SetObjectDirty(this);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PedestrianNodeSeatSettings))]
    public class PedestrianNodeSeatSettingsEditor : Editor
    {
        private const float HandlesRadiusSize = 0.2f;

        private PedestrianNodeSeatSettings pedestrianNodeSeatSettings;

        private void OnEnable()
        {
            pedestrianNodeSeatSettings = target as PedestrianNodeSeatSettings;
        }

        private void OnSceneGUI()
        {
            if (!pedestrianNodeSeatSettings) return;

            if (pedestrianNodeSeatSettings.showSeats)
            {
                if (pedestrianNodeSeatSettings.pedestrianNode == null)
                {
                    pedestrianNodeSeatSettings.Reset();
                }

                var capacity = pedestrianNodeSeatSettings.pedestrianNode.Capacity;

                for (int i = 0; i < capacity; i++)
                {
                    Vector3 seatPosition = PedestrianBenchPositionHelper.GetSeatPosition(i, capacity, pedestrianNodeSeatSettings.SeatOffset, pedestrianNodeSeatSettings.transform.position, pedestrianNodeSeatSettings.BaseOffset, pedestrianNodeSeatSettings.transform.rotation);

                    var benchSeatPosition = seatPosition + new Vector3(0, pedestrianNodeSeatSettings.SeatHeight);

                    Handles.color = Color.white;
                    Handles.DrawWireDisc(benchSeatPosition, Vector3.up, HandlesRadiusSize);

                    var enterSeatPosition = seatPosition + pedestrianNodeSeatSettings.transform.rotation * new Vector3(0, 0, pedestrianNodeSeatSettings.EnterSeatOffset);

                    Handles.color = Color.green;
                    Handles.DrawWireDisc(enterSeatPosition, Vector3.up, HandlesRadiusSize);
                }
            }
        }
    }
#endif
}