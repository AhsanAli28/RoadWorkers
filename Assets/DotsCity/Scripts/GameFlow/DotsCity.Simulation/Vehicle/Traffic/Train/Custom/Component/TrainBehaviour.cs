using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    public abstract class TrainBehaviour : TrainBehaviourBase
    {
        [SerializeField]
        private float delay = 1;

        protected override void ProcessEnteredStation()
        {
            StartCoroutine(WaitForStop());
        }

        protected abstract bool ShouldWait();

        private IEnumerator WaitForStop()
        {
            yield return new WaitWhile(() => ShouldWait());
            yield return new WaitForSeconds(delay);
            StartStation();
        }
    }
}