using Spirit604.DotsCity.Common;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config;
using Spirit604.MainMenu.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Spirit604.DotsCity.Gameplay.Bootstrap
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private MainMenuSceneSelectionController selectionController;
        [SerializeField] private EventSystem eventSystem;

        [SerializeField] private bool reportResult;

        private List<ConfigInjectorData> injectors = new List<ConfigInjectorData>();
        private LoadSceneDataConfig loadSceneDataConfig;
        private bool isMobile;
        private bool gameSceneLoaded;
        private bool injected;
        private LoadingScreen loadingScreen;
        private AsyncOperation loadingGameSceneOperation;
        private ISceneBootstrap sceneBootstrap;

        private void Awake()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
            }

            DontDestroyOnLoad(gameObject);
            selectionController.OnSceneLoad += SelectionController_OnSceneLoad;
        }

        private void OnDestroy()
        {
            SceneBootstrap.OnInitStarted -= SceneBootstrap_OnInitStarted;
        }

        private void InitSceneLoaded()
        {
            var scene = SceneManager.GetSceneByName(loadSceneDataConfig.SceneName);

            SceneManager.SetActiveScene(scene);

            var currentBoostrap = ObjectUtils.FindObjectOfType<SceneBootstrapBase>();
            var runtimeConfigManager = ObjectUtils.FindObjectOfType<RuntimeConfigManager>();

            if (runtimeConfigManager)
            {
                runtimeConfigManager.RecreateOnStart = true;
            }

            if (currentBoostrap && currentBoostrap is CityBootstrapBase)
            {
                (currentBoostrap as CityBootstrapBase).ManualBootstrap = true;
            }

            if (!injected)
            {
                InjectConfigs();
            }

            sceneBootstrap = currentBoostrap;
            sceneBootstrap?.StartInitilization();
            gameSceneLoaded = true;
        }

        private void InjectConfigs()
        {
            foreach (var injector in injectors)
            {
                if (string.IsNullOrEmpty(injector.TargetScript))
                    continue;

                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .FirstOrDefault(t => t.Name == injector.TargetScript);

                if (type == null)
                {
                    Log($"GameBootstrapper. Config type not found. Target: {injector.TargetScript}");
                    continue;
                }

                var target = ObjectUtils.FindObjectOfType(type);

                if (!target)
                {
                    Log($"GameBootstrapper. Config not found. Target: {injector.TargetScript}");
                    continue;
                }

                var configInjector = target.GetComponent<IConfigInject>();

                if (configInjector == null)
                {
                    Log($"GameBootstrapper. IConfigInject not found. Make sure, that target implements 'IConfigInject' interface. Target: {injector.TargetScript}");
                    continue;
                }

                var config = injector.GetConfig(isMobile);

                if (!config)
                    continue;

                try
                {
                    configInjector.InjectConfig(config);
                    Log($"GameBootstrapper. Target '{target.name}'. Config '{config.name}' is injected");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError(ex.Message);
                }
            }
        }

        private void Log(string text)
        {
            if (reportResult)
            {
                UnityEngine.Debug.Log(text);
            }
        }

        private void CompleteInit()
        {
            SceneBootstrap.OnInitStarted -= SceneBootstrap_OnInitStarted;
            loadingScreen = null;
            var op = SceneManager.UnloadSceneAsync(0);
            op.completed += (result) => { SceneManager.UnloadSceneAsync(1); };

            Destroy(gameObject);
        }

        private void SelectionController_OnSceneLoad(LoadSceneDataConfig loadSceneDataConfig)
        {
            this.loadSceneDataConfig = loadSceneDataConfig;
            selectionController.OnSceneLoad -= SelectionController_OnSceneLoad;
            isMobile = selectionController.IsMobile;

            if (eventSystem != null)
            {
                eventSystem.gameObject.SetActive(false);
            }

            var data = loadSceneDataConfig.ConfigData;

            foreach (var item in data)
            {
                var configInjectorData = item.Value as ConfigInjectorData;

                if (configInjectorData != null)
                {
                    injectors.Add(configInjectorData);
                }
            }

            var op = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
            op.completed += OpenLoadingScene;
        }

        private void OpenLoadingScene(AsyncOperation obj)
        {
            StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            while (loadingScreen == null)
            {
                loadingScreen = LoadingScreen.Instance;
                yield return null;
            }

            SceneBootstrap.OnInitStarted += SceneBootstrap_OnInitStarted;

            loadingGameSceneOperation = SceneManager.LoadSceneAsync(loadSceneDataConfig.SceneName, LoadSceneMode.Additive);
            loadingGameSceneOperation.completed += SceneManager_completed;
            loadingGameSceneOperation.allowSceneActivation = false;

            while (!loadingGameSceneOperation.isDone)
            {
                var progress = loadingGameSceneOperation.progress / 0.9f / 2;
                loadingScreen.UpdateProgress(progress);

                if (loadingGameSceneOperation.progress >= 0.9f)
                {
                    loadingGameSceneOperation.allowSceneActivation = true;
                    break;
                }

                yield return null;
            }

            while (!gameSceneLoaded)
            {
                yield return null;
            }

            if (sceneBootstrap != null)
            {
                while (sceneBootstrap.Progress < 1f)
                {
                    var progress = 0.5f + sceneBootstrap.Progress / 2;
                    loadingScreen.UpdateProgress(progress);

                    yield return null;
                }
            }

            CompleteInit();
        }

        private void SceneManager_completed(AsyncOperation obj)
        {
            InitSceneLoaded();
        }

        private void SceneBootstrap_OnInitStarted()
        {
            injected = true;
            InjectConfigs();
        }
    }
}
