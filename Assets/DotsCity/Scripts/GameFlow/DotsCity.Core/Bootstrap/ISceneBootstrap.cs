using System;

namespace Spirit604.DotsCity.Core.Bootstrap
{
    public interface ISceneBootstrap
    {
        public float Progress { get; }

        public event Action OnComplete;

        public void StartInitilization();
    }
}