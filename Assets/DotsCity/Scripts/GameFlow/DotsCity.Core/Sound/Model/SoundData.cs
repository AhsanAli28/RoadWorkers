using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    [ExecuteInEditMode]
    [CreateAssetMenu(menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Sound/Sound Data")]
    public class SoundData : ScriptableObject
    {
        public int Id;
        public string Name;
        public bool Loop;
        public float ClipVolume = 1f;
        public bool randomSound;
        public AudioClip AudioClip;
        public List<AudioClip> Multiclips = new List<AudioClip>();
        public EventFloatParameter[] Parameters;

        public string CurrentName
        {
            get
            {
#if FMOD
                return Name;
#else
                return name;
#endif
            }
        }

        [NonSerialized]
        public int RuntimeIndex;

        private static SoundDataContainer soundDataContainer;

#if UNITY_EDITOR
        private void OnDestroy()
        {
            RemoveFromContainer();
        }
#endif

        public AudioClip GetClip()
        {
            if (randomSound)
            {
                return Multiclips[UnityEngine.Random.Range(0, Multiclips.Count)];
            }

            return AudioClip;
        }

        public SoundDataContainer GetContainer()
        {
            if (soundDataContainer != null)
                return soundDataContainer;

#if UNITY_EDITOR
            const string containerMatchText = "t:SoundDataContainer";

            string[] containerGuids = UnityEditor.AssetDatabase.FindAssets(containerMatchText);

            if (containerGuids?.Length > 0)
            {
                soundDataContainer = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundDataContainer>(UnityEditor.AssetDatabase.GUIDToAssetPath(containerGuids[0]));
                return soundDataContainer;
            }
#endif

            return null;
        }

        public bool AddToContainer()
        {
#if UNITY_EDITOR
            var soundContainer = GetContainer();

            if (soundContainer != null)
            {
                if (soundContainer.AddSoundData(this))
                {
                    Debug.Log($"Sound data '{CurrentName}' added.");
                    return true;
                }
                else
                {
                    Debug.LogError($"Sound data '{CurrentName}' already added.");
                }
            }
            else
            {
                Debug.Log("Sound container not found.");
            }
#endif

            return false;
        }

        public bool RemoveFromContainer(bool autoDelete = true)
        {
#if UNITY_EDITOR
            var soundContainer = GetContainer();

            if (soundContainer != null)
            {
                if (soundContainer.RemoveSoundData(this))
                {
                    Debug.Log($"Sound data '{CurrentName}' removed.");
                    return true;
                }
                else if (!autoDelete)
                {
                    Debug.LogError($"Sound data '{CurrentName}' is not assigned to the container.");
                }
            }
            else
            {
                Debug.Log("Can't remove from container. Sound container not found.");
            }
#endif

            return false;
        }

        public bool ContainerContainsSound()
        {
#if UNITY_EDITOR
            var soundContainer = GetContainer();

            if (soundContainer != null)
            {
                return soundContainer.HasSoundData(this);
            }
            else
            {
                Debug.Log("Sound container not found.");
            }
#endif

            return false;
        }

#if UNITY_EDITOR

        public void Reset()
        {
            var sounds = UnityEditor.AssetDatabase.FindAssets("t:SoundData");
            var firstAvailableId = 1;

            foreach (var soundGuid in sounds)
            {
                var sound = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundData>(UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid));
                if (sound.Id >= firstAvailableId)
                {
                    firstAvailableId = sound.Id + 1;
                }
            }

            Id = firstAvailableId;
            EditorSaver.SetObjectDirty(this);
        }

        // The only reliable way to get the OnDestroy method to work is to
        class OnDestroyProcessor : UnityEditor.AssetModificationProcessor
        {
            // Cache the type for reuse.
            private static Type _type = typeof(SoundData);

            // Limit to certain file endings only.
            private const string _fileEnding = ".asset";

            public static UnityEditor.AssetDeleteResult OnWillDeleteAsset(string path, UnityEditor.RemoveAssetOptions _)
            {
                if (!path.EndsWith(_fileEnding))
                    return UnityEditor.AssetDeleteResult.DidNotDelete;

                var assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);

                if (assetType != null && (assetType == _type || assetType.IsSubclassOf(_type)))
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundData>(path);
                    asset.OnDestroy();
                }

                return UnityEditor.AssetDeleteResult.DidNotDelete;
            }
        }
#endif
    }
}
