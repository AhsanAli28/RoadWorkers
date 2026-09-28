using System;
using System.Collections;

namespace Spirit604.DotsCity.Core
{
    public interface IEntityWorldService
    {
        event Action OnEntitySceneUnload;

        IEnumerator DisposeWorldRoutine(bool autoRecreateWorld = true);
        void DisposeWorld(bool autoRecreateWorld = true);
        void CreateWorld();
    }
}
