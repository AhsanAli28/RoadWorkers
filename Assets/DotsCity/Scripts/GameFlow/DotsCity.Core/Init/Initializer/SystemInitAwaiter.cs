using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Initialization
{
    public class SystemInitAwaiter
    {
        private readonly MonoBehaviour sender;
        private readonly Func<bool> waitForInitCallback;
        private readonly Action initializedCallback;

        public SystemInitAwaiter(MonoBehaviour sender, Func<bool> waitForInitCallback, Action initializedCallback)
        {
            this.sender = sender;
            this.waitForInitCallback = waitForInitCallback;
            this.initializedCallback = initializedCallback;
        }

        public void StartInit()
        {
            sender.StartCoroutine(InternalInit());
        }

        private IEnumerator InternalInit()
        {
            yield return new WaitWhile(waitForInitCallback);

            initializedCallback();
        }
    }
}