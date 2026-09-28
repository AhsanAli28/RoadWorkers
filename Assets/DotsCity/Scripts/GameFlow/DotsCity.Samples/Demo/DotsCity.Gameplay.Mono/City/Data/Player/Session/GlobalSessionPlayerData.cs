using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Player.Session
{
    [Serializable]
    public class GlobalSessionPlayerData
    {
        public enum Location { Default, Home, Building }

        [SerializeField] private List<CharacterSessionData> totalCharaterData = new List<CharacterSessionData>();

        public List<CharacterSessionData> TotalCharaterData
        {
            get
            {
                return totalCharaterData;
            }
            set
            {
                totalCharaterData = value;
            }
        }

        public CharacterSessionData CurrentSelectedPlayer;

        public SerializableVector3 SpawnPosition;
        public CarData CarData;
        public bool WeaponIsHided = true;
        public SessionState CurrentState;
        public Location LastLocation;
    }
}