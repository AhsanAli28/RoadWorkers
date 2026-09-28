using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Train;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Spirit604.DotsCity.Samples.CustomTrain
{
    public class CustomTrainController : TrainBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private TrainRuntimeAuthoring trainRuntimeAuthoring;
        [SerializeField] private LocomotiveEngine locomotiveEngine;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float splinePosition;

        private Spline currentSpline;
        private float splineLength;
        private List<TrainWagonController> wagonControllers = new List<TrainWagonController>();

        protected override void Awake()
        {
            base.Awake();

            var wagons = trainRuntimeAuthoring.Wagons;

            for (int i = 0; i < wagons.Count; i++)
            {
                var wagon = wagons[i].GetComponent<TrainWagonController>();

                if (wagon != null)
                {
                    wagon.Init(splineContainer);
                    wagonControllers.Add(wagon);
                }
                else
                {
                    Debug.LogError($"{wagons[i].name} doesn't have 'TrainWagonController' component");
                }
            }
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            currentSpline = splineContainer.Splines[0];
            splineLength = splineContainer.CalculateLength();
        }

        private void FixedUpdate()
        {
            if (splineContainer == null) return;
            if (locomotiveEngine.Speed == 0) return;

            var splineDt = locomotiveEngine.Speed * Time.fixedDeltaTime / splineLength;
            splinePosition += splineDt;

            if (splinePosition > 1f) splinePosition -= 1f;

            currentSpline.Evaluate(splinePosition, out var targetPosition, out var forward, out var up);

            targetPosition = splineContainer.transform.TransformPoint(targetPosition);
            targetPosition.y = transform.position.y;

            Vector3 direction = ((Vector3)targetPosition - transform.position).normalized;

            rb.Move(targetPosition, Quaternion.LookRotation(forward));

            for (int i = 0; i < wagonControllers.Count; i++)
            {
                wagonControllers[i].Move(splineDt);
            }
        }

        protected override void ProcessEnteredStation()
        {
            locomotiveEngine.Throttle = 0;
            base.ProcessEnteredStation();
        }

        protected override void ProcessStationComplete()
        {
            locomotiveEngine.Throttle = 1;
        }

        protected override bool ShouldWait() => locomotiveEngine.Speed > 0.01f;

        private void Reset()
        {

        }

        [Button]
        public void PlaceTrain()
        {
            var spline = this.splineContainer.Splines[0];

            SplineUtility.GetNearestPoint(spline, transform.position, out var nearest, out var t);
            var dir = SplineUtility.EvaluateTangent(spline, t);

            splinePosition = t;

            transform.position = this.splineContainer.transform.TransformPoint(nearest);
            transform.rotation = Quaternion.LookRotation(dir);

            float size = transform.GetComponent<BoxCollider>().size.z;
            float distance = size / 2;

            foreach (var wagon in trainRuntimeAuthoring.Wagons)
            {
                distance += trainRuntimeAuthoring.WagonOffset;
                distance += size / 2;

                nearest = this.splineContainer.transform.TransformPoint(SplineUtility.GetPointAtLinearDistance(spline, t, -distance, out var resultT));
                dir = SplineUtility.EvaluateTangent(spline, resultT);

                wagon.transform.position = nearest;
                wagon.transform.rotation = Quaternion.LookRotation(dir);

                var wagonController = wagon.GetComponent<TrainWagonController>();
                wagonController.SplinePosition = resultT;
                EditorSaver.SetObjectDirty(wagonController);

                distance += size / 2;
            }

            EditorSaver.SetObjectDirty(this);
        }
    }
}
