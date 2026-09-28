using System;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Bootstrap
{
    [DefaultExecutionOrder(-10000)]
    public abstract class SceneBootstrapBase : MonoBehaviour, ISceneBootstrap
    {
        public abstract float Progress { get; protected set; }

        // Hacky way before zenject started to inject IConfigInject MainMenu configs
        public static event Action OnInitStarted = delegate { };
        public static event Action OnEntityLoaded = delegate { };
        public event Action OnComplete = delegate { };

        protected virtual void Awake()
        {
            RaiseInitStarted();
        }

        public abstract void StartInitilization();

        protected void RaiseInitStarted() => OnInitStarted();
        protected void RaiseEntityLoaded() => OnEntityLoaded();
        protected void RaiseCompleteBootstrap() => OnComplete?.Invoke();
    }
}