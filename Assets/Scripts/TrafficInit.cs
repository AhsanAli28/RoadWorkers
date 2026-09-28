using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrafficInit : MonoBehaviour
{
    [SerializeField] private SceneBootstrapBase sceneBootstrapBase;
    [SerializeField] private EntityWorldService entityWorldService;
    [SerializeField] public SubScene subscene;

    private void Start()
    {
        sceneBootstrapBase.OnComplete += HandleOnInit;
        Invoke(nameof(StartInit), 1);
    }

    private void StartInit()
    {
        //SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subscene.SceneGUID);
        sceneBootstrapBase.StartInitilization();
    }

    private void HandleOnInit()
    {
        print("Traffic Init Complete");
    }
}
