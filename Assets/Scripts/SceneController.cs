using Spirit604.DotsCity.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] EntityWorldService entityWorldService;

    private const int LoadingSceneIndex = 1;

    private int sceneId;
    private string sceneName;
    private LoadingScreen loadingScreen;
    private AsyncOperation op;

    private static SceneController _instance;

    public static SceneController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneController>();

                if (_instance == null)
                {
                    Debug.LogWarning("No SceneController instance found in the scene.");
                }
                else if (FindObjectsOfType<SceneController>().Length > 1)
                {
                    Debug.LogWarning("Multiple SceneController instances found. Keeping the first and destroying the rest.");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }

    }
    private void Update()
    {
        if (loadingScreen != null && op != null)
        {
            var progress = op.progress / 0.9f;
            loadingScreen.UpdateProgress(progress);
        }
    }

    //public void UnloadMainScene()
    //{
    //    if (entityWorldService == null)
    //    {
    //        entityWorldService = FindObjectOfType<EntityWorldService>();
    //    }

    //    if (entityWorldService == null)
    //    {
    //        Debug.LogError("No Entity world service");
    //        return;
    //    }

    //    entityWorldService.DisposeWorld();
    //    Invoke(nameof(LoadMainMenu), 1);
    //}

    public void LoadMainScene()
    {
        LoadScene(2);
    }

    public void LoadMainMenu()
    {
        LoadScene(0);
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

    //public void LoadScene(string sceneName)
    //{
    //    this.sceneName = sceneName;
    //    LoadLoadingScreen();
    //}

    private void LoadLoadingScreen()
    {
        SceneManager.LoadSceneAsync(LoadingSceneIndex, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += SceneManager_sceneLoadingLoaded;
    }

    private IEnumerator DestroyWorldAndLoadScene(int sceneName)
    {
        if (entityWorldService == null)
        {
            entityWorldService = FindObjectOfType<EntityWorldService>();
        }

        if (entityWorldService == null)
        {
            Debug.LogError("No entity World Service");
            yield return null;
        }


        entityWorldService.DisposeWorld();
        yield return new WaitForSeconds(1);
        op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        //yield return entityWorldService.DisposeWorldRoutine();
        //op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    private IEnumerator DestroyWorldAndLoadScene(string sceneName)
    {
        if (entityWorldService == null)
        {
            entityWorldService = FindObjectOfType<EntityWorldService>();
        }

        if (entityWorldService == null)
        {
            Debug.LogError("No entity World Service");
            yield return null;
        }
        print("We are doing DestroyWorldAndLoadScene with string");

        entityWorldService.DisposeWorld();
        yield return new WaitForSeconds(1);
        op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        //yield return entityWorldService.DisposeWorldRoutine();
        //op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }


    private void SceneManager_sceneLoadingLoaded(Scene arg0, LoadSceneMode arg1)
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoadingLoaded;

        loadingScreen = FindObjectOfType<LoadingScreen>();

        if(loadingScreen != null)
        {
            loadingScreen.UpdateProgress(0);
        }

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
