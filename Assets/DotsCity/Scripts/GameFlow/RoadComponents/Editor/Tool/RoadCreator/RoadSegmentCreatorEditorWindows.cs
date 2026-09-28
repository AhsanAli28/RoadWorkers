using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        public void OpenPathCreator()
        {
            pathCreator = PathCreator.ShowWindow();
            pathCreator.Initialize(Config.PathPrefab, creator);
        }

        public void OpenTrafficNodeEditor()
        {
            trafficNodeWindowEditor = TrafficNodeWindowEditor.ShowWindow();
            trafficNodeWindowEditor.Initialize(creator, creator.CreatedTrafficNodes.ToArray(), creator.CrossWalkOffset);
        }

        public void TryToAddTrafficNodeToPathCreator(Vector3 position)
        {
            if (pathCreator != null)
            {
                var colliders = Physics.OverlapSphere(position, 4f, 1 << LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME));

                for (int i = 0; i < colliders?.Length; i++)
                {
                    var trafficNode = colliders[i].transform.GetComponent<TrafficNode>();

                    if (trafficNode != null)
                    {
                        pathCreator.TryToAddOrRemoveNode(trafficNode);
                    }
                }
            }
        }

        public void OpenPathSettingsWindow()
        {
            var path = creator.GetSelectedPath();

            if (path != null)
            {
                OpenPathSettings(path, true);
            }
        }

        public void OpenPathSettings(Path path)
        {
            if (lastSelectedPath != null)
            {
                lastSelectedPath.ShowInfoWaypoints = false;
            }

            lastSelectedPath = path;

            if (path == null)
            {
                return;
            }

            pathSettingsWindow?.Initialize(path);
        }

        public void OpenPathSettings(Path path, bool openWindow = false)
        {
            if (path == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (openWindow)
            {
                pathSettingsWindow = PathSettingsWindowEditor.ShowWindow();
            }

            OpenPathSettings(path);
#endif
        }

    }
}