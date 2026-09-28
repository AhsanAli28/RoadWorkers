using UnityEditor;
using UnityEngine;

#if ZENJECT
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
#endif

namespace Spirit604.DotsCity.Installer
{
    /// <summary>
    /// Helper component for switching from Zenject to manual resolve and back, if you don't need it, feel free to remove it.
    /// </summary>
    public class ProjectContextSwitcher : MonoBehaviour
    {
#if UNITY_EDITOR

        public bool SwitchContext(bool zenject)
        {
#if ZENJECT
            if (!zenject)
                return false;

            var manualContext = this.GetComponent<ManualSceneContext>();

            if (manualContext != null)
            {
                GameObject.DestroyImmediate(manualContext);
                UnityEngine.Debug.Log("ProjectContextSwitcher. ManualSceneContext is removed.");
            }

            var sceneContext = this.GetComponent<Zenject.SceneContext>();

            if (sceneContext == null)
            {
                var root = this.GetComponentInParent<EntityRootSubsceneGenerator>();

                sceneContext = this.gameObject.AddComponent<Zenject.SceneContext>();
                sceneContext.Installers = this.GetComponentsInChildren<Zenject.MonoInstaller>();
                EditorSaver.SetObjectDirty(sceneContext);

                foreach (var installer in sceneContext.Installers)
                {
                    InspectorExtension.RebindInspector(installer, root.gameObject);
                }

                UnityEngine.Debug.Log("ProjectContextSwitcher. Zenject.SceneContext is enabled.");
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(this.gameObject);

            return true;

#else
            if (zenject)
                return false;

            var manualContext = GetComponent<ManualSceneContext>();

            if (manualContext == null)
            {
                manualContext = this.gameObject.AddComponent<ManualSceneContext>();
                UnityEngine.Debug.Log("ProjectContextSwitcher. ManualSceneContext is enabled.");
            }

            manualContext.RebindInspector();

            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(this.gameObject) > 0)
            {
                UnityEngine.Debug.Log("ProjectContextSwitcher. Zenject.SceneContext is removed.");
            }

            return true;
#endif
        }
#endif
    }
}