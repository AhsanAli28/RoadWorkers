using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public abstract class AstarStaticGraphBase : AstarEntityGraphBase
    {
        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        protected override void Start()
        {
            base.Start();
            StartCoroutine(WaitRoutine());
        }

        private IEnumerator WaitRoutine()
        {
            yield return new WaitWhile(() => WaitCondition());
            CreateGraph();
        }

        protected abstract bool WaitCondition();

        protected abstract void CreateGraph();
    }
}
