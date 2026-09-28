using System;

namespace Spirit604.Gameplay.Services
{
    public interface ISceneService
    {
        public event Action OnSceneUnloaded;

        public void LoadScene(int sceneId);
        public void LoadScene(string sceneName);
    }
}