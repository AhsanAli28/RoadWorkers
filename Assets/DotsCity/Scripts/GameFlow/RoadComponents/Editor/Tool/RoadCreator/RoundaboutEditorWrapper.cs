using UnityEditor;

namespace Spirit604.CityEditor.Road
{
    public class RoundaboutEditorWrapper : ICustomCreatorEditor
    {
        private Roundabout roundabout;
        private RoadSegmentCreator roadSegmentCreator;
        private SerializedObject serializedObject;

        public RoundaboutEditorWrapper(Roundabout roundabout, RoadSegmentCreator roadSegmentCreator)
        {
            this.roundabout = roundabout;
            this.roadSegmentCreator = roadSegmentCreator;
            this.serializedObject = new SerializedObject(roundabout);
        }

        public void DrawInspector()
        {
            RoundaboutEditor.DrawInspector(serializedObject, roundabout, roadSegmentCreator);
        }

        public void Validate()
        {
            if (roundabout != null)
                return;

            if (roadSegmentCreator.gameObject.TryGetComponent<Roundabout>(out var component))
            {
                roundabout = component;
            }
            else
            {
                roundabout = roadSegmentCreator.gameObject.AddComponent<Roundabout>();
            }
        }
    }
}