using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Bootstrap
{
    public abstract class BootstrapCoroutineCommandBase : IBootstrapCommand
    {
        private bool completed;

        private readonly MonoBehaviour source;

        protected BootstrapCoroutineCommandBase(MonoBehaviour source)
        {
            this.source = source;
        }

        public virtual async Task Execute()
        {
            source.StartCoroutine(MainRoutine());

            while (!completed)
            {
                await Task.Delay(100);
            }
        }

        protected IEnumerator MainRoutine()
        {
            yield return InternalRoutine();
            completed = true;
        }

        protected abstract IEnumerator InternalRoutine();
    }
}