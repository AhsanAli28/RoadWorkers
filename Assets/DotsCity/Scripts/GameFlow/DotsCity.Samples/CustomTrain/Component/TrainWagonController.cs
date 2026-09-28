using Spirit604.Extensions;
using UnityEngine;
using UnityEngine.Splines;

namespace Spirit604.DotsCity.Samples.CustomTrain
{
    public class TrainWagonController : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float splinePosition;

        private SplineContainer splineContainer;
        private Spline spline;

        public float SplinePosition { get => splinePosition; set => splinePosition = value; }

        public void Move(float splineDt)
        {
            splinePosition += splineDt;
            if (splinePosition > 1f) splinePosition -= 1f;

            var sample = spline.Evaluate(splinePosition, out var targetPosition, out var forward, out var up);

            targetPosition = splineContainer.transform.TransformPoint(targetPosition);
            targetPosition.y = transform.position.y;

            Vector3 direction = ((Vector3)targetPosition - transform.position).normalized;
            rb.Move(targetPosition, Quaternion.LookRotation(forward));
        }

        public void Init(SplineContainer splineContainer)
        {
            this.splineContainer = splineContainer;
            this.spline = splineContainer.Splines[0];
        }

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
