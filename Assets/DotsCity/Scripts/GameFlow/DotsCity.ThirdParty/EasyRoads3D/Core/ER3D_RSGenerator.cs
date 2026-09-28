using EasyRoads3Dv3;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [ExecuteInEditMode]
    public class ER3D_RSGenerator : RSGeneratorBase
    {
        public override Type RoadType => typeof(ERModularRoad);

        public override Type CustomPrefabType => typeof(ERCrossingPrefabs);

        public override Type CustomPrefabIgnoreType => typeof(ERCrossings);

#if UNITY_EDITOR

        protected override void OnEnable()
        {
            base.OnEnable();
            ERRoadNetwork.onRoadUpdate -= ERRoadNetwork_onRoadUpdate;
            ERRoadNetwork.onRoadUpdate += ERRoadNetwork_onRoadUpdate;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ERRoadNetwork.onRoadUpdate -= ERRoadNetwork_onRoadUpdate;
        }

        protected override void AddSceneRoadObjects()
        {
            RegisterSceneRoadObject<ERModularRoad, ERModularRoadWrapper>();
            RegisterSceneRoadObject<ERCrossings, ERCrossingWrapper>();
            RegisterSceneRoadObject<ERCrossingPrefabs, ERCrossingPrefabWrapper>();
        }

        protected override ISplineRoad GetSplineRoad(Component roadComponent)
        {
            return new ERModularRoadWrapper(roadComponent.GetComponent<ERModularRoad>());
        }

        protected override ICrossingRoad GetCrossingRoad(Component roadComponent)
        {
            return new ERCrossingWrapper(roadComponent.GetComponent<ERCrossings>());
        }

        protected override ICustomPrefabRoad GetCustomRoadPrefab(Component customPrefabComponent)
        {
            return new ERCrossingPrefabWrapper(customPrefabComponent.GetComponent<ERCrossingPrefabs>());
        }

        protected override Component GetSplineComponentByObject(GameObject sceneObject)
        {
            return sceneObject.GetComponent<ERModularRoad>();
        }

        private void ERRoadNetwork_onRoadUpdate(ERRoad road)
        {
            UpdateSegment(road.roadScript, road.gameObject);
        }

#endif
    }
}