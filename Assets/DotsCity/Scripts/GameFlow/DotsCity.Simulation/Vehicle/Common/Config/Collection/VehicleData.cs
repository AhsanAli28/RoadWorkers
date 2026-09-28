using Spirit604.DotsCity.Simulation.Car.Sound;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [Serializable]
    public class VehicleData
    {
        public string Name;

        public VehicleDataCollection.SettingsType SettingsType;

        [Tooltip("ID of source parent settings vehicles (if empty, no clone settings)")]
        public string SourceVehicleID;

        public VehicleDataCollection.SettingsType GetSettingsFlag()
        {
            if (SettingsType.HasFlag(VehicleDataCollection.SettingsType.Overwrite))
            {
                return VehicleDataCollection.SettingsType.Overwrite;
            }

            return SettingsType;
        }

        [Tooltip("Minimum pitch of the car engine")]
        [Range(0f, 5f)] public float MinPitch = 0.6f;

        [Tooltip("Maximum pitch of the car engine")]
        [Range(0f, 5f)] public float MaxPitch = 3f;

        [Tooltip("Speed at which the engine has the maximum pitch")]
        [Range(0f, 100f)] public float MaxLoadSpeed = 60f;

        [Tooltip("Speed at which the engine has the maximum volume")]
        [Range(0f, 100f)] public float MaxVolumeSpeed = 20f;

        [Tooltip("Minimum engine volume")]
        [Range(0f, 1f)] public float MinVolume = 0.4f;

        public CarSoundDictionary CarSoundData = new CarSoundDictionary();
    }
}
