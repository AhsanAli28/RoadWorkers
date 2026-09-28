using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class ScriptSwitcher : MonoBehaviourBase
    {
        [SerializeField] private MonoBehaviour[] scripts;
        [SerializeField] private AudioSource[] audioSources;

        public void SwitchScripts(bool isActive)
        {
            for (int i = 0; i < scripts?.Length; i++)
            {
                scripts[i].enabled = isActive;
            }

            for (int i = 0; i < audioSources?.Length; i++)
            {
                audioSources[i].enabled = isActive;
            }
        }

        [Button]
        public void Enable()
        {
            SwitchScripts(true);
        }

        [Button]
        public void Disable()
        {
            SwitchScripts(false);
        }

        private void Reset()
        {
            scripts = GetComponentsInChildren<MonoBehaviour>().Where(a => a.enabled && a != this).ToArray();
            audioSources = GetComponentsInChildren<AudioSource>().Where(a => a.enabled).ToArray();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
