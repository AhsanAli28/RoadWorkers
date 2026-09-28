using Spirit604.Extensions;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [DefaultExecutionOrder(-10000)]
    public class EntityWorldService : SingletonMonoBehaviour<EntityWorldService>, IEntityWorldService
    {
        public event Action OnEntitySceneUnload = delegate { };

        public IEnumerator DisposeWorldRoutine(bool autoRecreateWorld = true)
        {
            yield return new WaitForEndOfFrame();
            DisposeWorld(autoRecreateWorld);
        }

        public void DisposeWorld(bool autoRecreateWorld = true)
        {
            OnEntitySceneUnload();
            World.DisposeAllWorlds();

            if (autoRecreateWorld)
                CreateWorld();
        }

        public void CreateWorld()
        {
            DefaultWorldInitialization.Initialize("Default World", false);
        }
    }
}