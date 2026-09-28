using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Bootstrap
{
    public abstract class CityBootstrapBase : SceneBootstrapBase
    {
        [SerializeField]
        private bool logging;

        [Tooltip("For automatic startup bootstrap, useful for compiling the project with a single prototype scene")]
        [SerializeField] private bool autoBootstrap;

        [Tooltip("When SceneBootstrap is launched in the Editor from a custom user script <b>(Editor only)</b>")]
        [SerializeField] private bool manualBootstrap;

        [SerializeField]
        private List<InitializerBase> initializers = new List<InitializerBase>();

        private EntityQuery settingsQuery;
        private bool launched;

        protected List<IBootstrapCommand> commands = new List<IBootstrapCommand>();

        private List<IPreInitializer> preInitializers = new List<IPreInitializer>();
        private List<ILateInitializer> lateInitializers = new List<ILateInitializer>();

        public override float Progress { get; protected set; }
        public bool ManualBootstrap { get => manualBootstrap; set => manualBootstrap = value; }

        private IEnumerator Start()
        {
            bool launch = false;

#if UNITY_EDITOR

            bool startFromMenu = manualBootstrap;

            if (!startFromMenu || autoBootstrap)
            {
                launch = true;
            }
#else
            launch = autoBootstrap;
#endif

            if (launch)
            {
                yield return new WaitForEndOfFrame();
                StartInitilization();
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < initializers.Count; i++)
            {
                initializers[i].Dispose();
            }
        }

        public override void StartInitilization()
        {
            if (launched)
            {
                Debug.Log("SceneBootstrapBase. Warning, attempt to start bootstrap twice");
                return;
            }

            launched = true;

            StartCoroutine(InitializeScene());
        }

        protected abstract void InitCommands();

        protected void Log(string text)
        {
            if (logging)
            {
                UnityEngine.Debug.Log(text);
            }
        }

        private IEnumerator InitializeScene()
        {
            Log("Starting scene initialization");
            InitQueries();

            Log("InitCommands");

            InitCommands();
            AddInitializers();

            for (int i = 0; i < preInitializers.Count; i++)
            {
                Log($"Starting preInitializer {i}");
                preInitializers[i].PreInitialize();
            }

            Log($"Waiting for subscene load");

            // Config sub scene is loaded
            yield return new WaitWhile(() => settingsQuery.CalculateEntityCount() == 0);

            float step = 0.5f / initializers.Count;

            int index = 0;

            while (index < initializers.Count)
            {
                Log($"Starting {initializers[index].name}");
                initializers[index].Initialize();
                Log($"Finished {initializers[index].name}");

                Progress += step;
                index++;
            }

            Log($"RaiseEntityLoaded");
            RaiseEntityLoaded();

            const float startProgress = 0.6f;
            const float endProgress = 0.95f;

            Progress = startProgress;

            float commandStep = (endProgress - startProgress) / (commands.Count + 1);

            for (int i = 0; i < commands.Count; i++)
            {
                Log($"Command {i}");
                var command = commands[i];
                var task = command.Execute();

                yield return new WaitWhile(() => !task.IsCompleted);

                Progress = startProgress + commandStep * (i + 1);
            }

            Progress = endProgress;

            Log($"RaiseCompleteBootstrap");
            RaiseCompleteBootstrap();

            yield return new WaitForSeconds(0.1f);

            Log($"CompleteInit");
            CompleteInit();
        }

        private void InitQueries()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            settingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CullSystemConfigReference>());
            Log("InitQueries");
        }

        private void AddInitializers()
        {
            preInitializers.Clear();
            lateInitializers.Clear();

            for (int i = 0; i < initializers.Count; i++)
            {
                if (initializers[i] is IPreInitializer)
                {
                    preInitializers.Add(initializers[i] as IPreInitializer);
                }

                if (initializers[i] is ILateInitializer)
                {
                    lateInitializers.Add(initializers[i] as ILateInitializer);
                }
            }

            Log("AddInitializers");
        }

        private void CompleteInit()
        {
            for (int i = 0; i < lateInitializers.Count; i++)
            {
                lateInitializers[i].LateInitialize();
            }

            Progress = 1f;
            RaiseCompleteBootstrap();
        }
    }
}