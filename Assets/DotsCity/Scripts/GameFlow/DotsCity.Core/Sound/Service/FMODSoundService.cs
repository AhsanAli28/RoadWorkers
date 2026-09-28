using Spirit604.Attributes;
using Spirit604.CityEditor;
using System.Collections.Generic;
using UnityEngine;

#if FMOD
using FMOD;
using FMOD.Studio;
using FMODUnity;
#endif

namespace Spirit604.DotsCity.Core.Sound
{
    [CreateAssetMenu(menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Sound/Sound Service")]
    public class FMODSoundService : ScriptableObject
#if FMOD
    , IFMODSoundService
#endif
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/sound.html#fmod-sound-service")]
        [SerializeField] private string link;

        [SerializeField] private SoundDataContainer soundDataContainer;

        [ReadOnly]
        [SerializeField] private List<SoundData> soundDatas;

        public List<SoundData> Sounds => soundDataContainer.Sounds;

#if FMOD

        private struct SoundParamData
        {
            public EventDescription EventDescription;
            public PARAMETER_DESCRIPTION[] FloatParametersDatas;
        }

        private SoundParamData[] soundParamDatas;
        private FMOD.System fmodSystem;
        private bool initialized;
        private Dictionary<int, SoundData> allData;

        public SoundData GetSoundById(int id)
        {
            if (allData.TryGetValue(id, out var soundData)) return soundData;

            UnityEngine.Debug.LogError($"FMODSoundService. Sound id {id} doesn't exist.");

            return null;
        }

        public EventDescription GetEventDescription(SoundData soundData)
        {
            var soundParamData = soundParamDatas[soundData.RuntimeIndex];
            if (!soundParamData.EventDescription.isValid())
            {
                UnityEngine.Debug.LogError($"FMODSoundService. Event {Sounds[soundData.RuntimeIndex].Name} not valid");
            }
            return soundParamData.EventDescription;
        }

        public PARAMETER_DESCRIPTION GetFloatParameterDescription(SoundData soundData, EventFloatParameter parameter)
        {
            var soundParamData = soundParamDatas[soundData.RuntimeIndex];

            var parameterIndex = parameter.RuntimeIndex;

            if (soundParamData.FloatParametersDatas.Length <= parameterIndex)
            {
                UnityEngine.Debug.LogError($"FMODSoundService. Event {Sounds[soundData.RuntimeIndex].Name} don't have {parameterIndex + 1} parameters");
                return default;
            }

            var parameterDefition = soundParamData.FloatParametersDatas[parameterIndex];
            return parameterDefition;
        }

        public void Initialize()
        {
            soundParamDatas = new SoundParamData[Sounds.Count];

            for (int index = 0; index < Sounds.Count; index++)
            {
                var soundData = Sounds[index];

                soundData.RuntimeIndex = index;
                ref var data = ref soundParamDatas[index];

                var result = RuntimeManager.StudioSystem.getEvent(soundData.Name, out data.EventDescription);

                if (result != RESULT.OK)
                {
                    UnityEngine.Debug.LogError($"FMODSoundService. Event {soundData.Name} not found. Make sure you have installed the FMOD project settings & the event name matches.");
                    continue;
                }

                data.FloatParametersDatas = new PARAMETER_DESCRIPTION[soundData.Parameters.Length];

                for (int i = 0; i < soundData.Parameters.Length; i++)
                {
                    ref var parameterDef = ref data.FloatParametersDatas[i];
                    result = data.EventDescription.getParameterDescriptionByName(
                        soundData.Parameters[i].Name, out parameterDef
                    );

                    if (result != RESULT.OK)
                    {
                        UnityEngine.Debug.LogError($"FMODSoundService. Event {soundData.Name} parameter {soundData.Parameters[i].Name} not found");
                    }

                    soundData.Parameters[i].RuntimeIndex = i;
                }
            }

            allData = new Dictionary<int, SoundData>();

            foreach (var soundData in Sounds)
            {
                if (!allData.ContainsKey(soundData.Id))
                {
                    allData.Add(soundData.Id, soundData);
                }
                else
                {
                    UnityEngine.Debug.LogError($"FMODSoundService. Event '{soundData.Name}' id {soundData.Id} id duplication found");
                }
            }
        }

#endif

        public void Mute()
        {
            CheckInitilization();
#if FMOD
            fmodSystem.setOutput(FMOD.OUTPUTTYPE.NOSOUND);
#endif
        }

        public void Unmute()
        {
            CheckInitilization();
#if FMOD
            fmodSystem.setOutput(FMOD.OUTPUTTYPE.AUTODETECT);
#endif
        }

        private void CheckInitilization()
        {
#if FMOD
            if (initialized)
                return;

            initialized = true;
            FMODUnity.RuntimeManager.StudioSystem.getCoreSystem(out fmodSystem);
#endif

        }
    }
}