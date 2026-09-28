using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    public abstract class TrainBehaviourBase : MonoBehaviour
    {
        protected IHybridEntityRef HybridEntityRef { get; private set; }

        public TrainStation LastStation { get; private set; }

        protected virtual void Awake()
        {
            HybridEntityRef = GetComponent<IHybridEntityRef>();
        }

        /// <summary> Begin the process of pedestrians entering and exiting the station.</summary>
        public void StartStation()
        {
            LastStation.TrainCompleted += TrainStation_TrainCompleted;
            LastStation.Activate(HybridEntityRef);
        }

        /// <summary> Is called when a train has entered a station.</summary>
        protected abstract void ProcessEnteredStation();

        /// <summary> Called when the process of pedestrians entering and exiting the station is complete.</summary>
        protected abstract void ProcessStationComplete();

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<TrainStation>(out var trainStation))
            {
                LastStation = trainStation;
                ProcessEnteredStation();
            }

            if (other.gameObject.TryGetComponent<TrafficNodeLightTriggerSceneRef>(out var lightTriggerRef))
            {
                lightTriggerRef.AddEntity(HybridEntityRef);
            }
        }

        protected virtual void TrainStation_TrainCompleted(TrainStation trainStation)
        {
            trainStation.TrainCompleted -= TrainStation_TrainCompleted;
            trainStation.Deactivate();
            ProcessStationComplete();
            LastStation = null;
        }
    }
}