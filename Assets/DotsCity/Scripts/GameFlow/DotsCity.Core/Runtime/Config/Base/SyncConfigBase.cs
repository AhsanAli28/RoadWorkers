using Spirit604.Attributes;
using Unity.Scenes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Spirit604.CityEditor;
#endif

namespace Spirit604.DotsCity.Core
{
    public abstract class SyncConfigBase : MonoBehaviourBase
    {
#if UNITY_EDITOR
        private ConfigAutoSyncer configAutoSyncer;
#endif

        private static SubScene subScene;

        public static SubScene SubScene
        {
            get
            {
                if (subScene == null)
                {
                    var go = GameObject.Find("EntitySubScene");

                    if (go != null)
                    {
                        subScene = go.GetComponent<SubScene>();
                    }
                }

                return subScene;
            }
        }

#if UNITY_EDITOR

        protected virtual bool AutoSync => CityEditorSettings.GetOrCreateSettings().SyncConfigOnChange;

        private bool Changed { get; set; }

        private bool IsSubscene => gameObject.scene.isSubScene;

        private bool SyncToSubsceneFlag => Changed && !IsSubscene && !Application.isPlaying;

        private bool SyncToMainSceneFlag => Changed && IsSubscene && !Application.isPlaying;

#else
        protected virtual bool AutoSync => false;
#endif

        public virtual void Sync()
        {
#if UNITY_EDITOR

            if (!AutoSync)
            {
                Changed = true;
                return;
            }

            Changed = false;

            if (!Application.isPlaying)
            {
                configAutoSyncer?.Sync();
            }
#endif
        }

#if UNITY_EDITOR

        [ShowIf(nameof(SyncToSubsceneFlag))]
        [Button]
        private void SyncToSubscene()
        {
            Changed = false;

            if (!Application.isPlaying)
            {
                PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;
                configAutoSyncer?.Sync(true);
            }
        }

        [ShowIf(nameof(SyncToMainSceneFlag))]
        [Button]
        private void SyncToMainScene()
        {
            Changed = false;

            if (!Application.isPlaying)
            {
                PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;
                configAutoSyncer?.Sync(true);
            }
        }

        [OnInspectorEnable]
        private void OnInspectorEnabled()
        {
            PrefabUtility.prefabInstanceReverted += PrefabUtility_prefabInstanceReverted;
            configAutoSyncer = new ConfigAutoSyncer(this, SubScene);
            configAutoSyncer.Synced += ConfigAutoSyncer_Synced;
        }

        [OnInspectorDisable]
        private void OnInspectorDisabled()
        {
            PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;

            if (configAutoSyncer != null)
            {
                configAutoSyncer.Synced -= ConfigAutoSyncer_Synced;
                configAutoSyncer.Dispose();
                configAutoSyncer = null;
            }
        }

        private void ConfigAutoSyncer_Synced(bool success)
        {
            PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;
            PrefabUtility.prefabInstanceReverted += PrefabUtility_prefabInstanceReverted;
        }

        private void PrefabUtility_prefabInstanceReverted(GameObject @object)
        {
            if (!AutoSync)
            {
                Changed = true;
            }
        }

#endif
    }
}
