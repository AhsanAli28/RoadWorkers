using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    [CreateAssetMenu(menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Sound/Sound Data Container")]
    public class SoundDataContainer : ScriptableObject
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/sound.html#fmod-sound-service")]
        [SerializeField] private string link;

        [SerializeField] private List<SoundData> soundDatas = new List<SoundData>();

        public List<SoundData> Sounds => soundDatas;

        public SoundData GetSoundById(int id)
        {
            for (int i = 0; i < soundDatas?.Count; i++)
            {
                if (soundDatas[i].Id == id)
                    return soundDatas[i];
            }

            return null;
        }

        public bool HasSoundData(SoundData soundData)
        {
            if (soundDatas != null)
                return soundDatas.Contains(soundData);

            return false;
        }

        public bool AddSoundData(SoundData soundData)
        {
            if (soundDatas == null)
                soundDatas = new List<SoundData>();

            var added = soundDatas.TryToAdd(soundData);

            if (added)
                EditorSaver.SetObjectDirty(this);

            return added;
        }

        public bool RemoveSoundData(SoundData soundData)
        {
            if (soundDatas == null)
                return false;

            var removed = soundDatas.TryToRemove(soundData);

            if (removed)
                EditorSaver.SetObjectDirty(this);

            return removed;
        }
    }
}