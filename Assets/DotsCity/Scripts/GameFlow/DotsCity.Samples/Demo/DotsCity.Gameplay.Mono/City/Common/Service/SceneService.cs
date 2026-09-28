using Spirit604.Attributes;
using Spirit604.DotsCity.Common;
using Spirit604.DotsCity.Core;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spirit604.Gameplay.Services
{
    public class SceneService : MonoBehaviour, ISceneService
    {
        private const int LoadingSceneIndex = 1;

        private int sceneId;
        private string sceneName;
        private LoadingScreen loadingScreen;
        private AsyncOperation op;

        private IEntityWorldService entityWorldService;

        [InjectWrapper]
        public void Construct(IEntityWorldService entityWorldService)
        {
            this.entityWorldService = entityWorldService;
        }

        public event Action OnSceneUnloaded = delegate { };

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            if (loadingScreen != null && op != null)
            {                
                var progress = op.progress / 0.9f;
                loadingScreen.UpdateProgress(progress);
                CheckProgress();
            }
        }

        public void LoadScene(int sceneId)
        {
            bool hasScene = sceneId != -1;
            this.sceneId = -1;

            if (hasScene)
            {
                this.sceneId = sceneId;
                LoadLoadingScreen();
            }
            else
            {
                UnityEngine.Debug.LogError($"Request scene index {sceneId} not found");
            }
        }

        public void LoadScene(string sceneName)
        {
            this.sceneName = sceneName;
            LoadLoadingScreen();
        }

        private void LoadLoadingScreen()
        {
            transform.parent = null;
            DontDestroyOnLoad(gameObject);

            enabled = true;
            SceneManager.LoadSceneAsync(LoadingSceneIndex, LoadSceneMode.Additive);
            SceneManager.sceneLoaded += SceneManager_sceneLoadingLoaded;
        }

        private IEnumerator DestroyWorldAndLoadScene(int sceneName)
        {
            yield return entityWorldService.DisposeWorldRoutine();
            op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private IEnumerator DestroyWorldAndLoadScene(string sceneName)
        {
            yield return entityWorldService.DisposeWorldRoutine();
            op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private void CheckProgress()
        {
            if (op.progress >= 0.9f)
            {
                SceneManager.UnloadSceneAsync(LoadingSceneIndex);
                Destroy(gameObject);
            }
        }

        private void SceneManager_sceneLoadingLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoadingLoaded;

            OnSceneUnloaded();

            loadingScreen = LoadingScreen.Instance;

            if (sceneId != -1)
            {
                StartCoroutine(DestroyWorldAndLoadScene(sceneId));
            }
            else
            {
                StartCoroutine(DestroyWorldAndLoadScene(sceneName));
            }
        }
    }
}