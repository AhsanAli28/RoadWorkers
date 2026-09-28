using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Player
{
    [CreateAssetMenu(fileName = "PlayerSpawnDataConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_OTHER_PATH + "PlayerSpawnDataConfig")]
    public class PlayerSpawnDataConfig : ScriptableObject
    {
        public enum SpawnPlayerType { Npc, Car }

        [Tooltip("" +
            "<b>Npc</b> : the player is initially spawned as an NPC\r\n\r\n" +
            "<b>Car</b> : the player is initially spawned as a car")]
        [SerializeField] private SpawnPlayerType currentSpawnPlayerType;

        [SerializeField] private int selectedCarModel;

        [SerializeField] private string selectedNpcID;

        [SerializeField] private PlayerCarSpawnData playerCarSpawnData;

        [SerializeField][Range(1, 4)] private int bandSize = 1;

        [SerializeField] private List<BandCharacterSpawnData> characterDatas = new List<BandCharacterSpawnData>();

        public SpawnPlayerType CurrentSpawnPlayerType { get => currentSpawnPlayerType; set => currentSpawnPlayerType = value; }

        public int SelectedCarModel
        {
            get => selectedCarModel;
            set
            {
                if (selectedCarModel != value)
                {
                    selectedCarModel = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public string SelectedNpcID
        {
            get => selectedNpcID;
            set
            {
                if (selectedNpcID != value)
                {
                    selectedNpcID = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public PlayerCarSpawnData PlayerCarSpawnData { get => playerCarSpawnData; set => playerCarSpawnData = value; }
        public int BandSize { get => bandSize; set => bandSize = value; }
        public List<BandCharacterSpawnData> CharacterDatas { get => characterDatas; set => characterDatas = value; }
        public BandCharacterSpawnData PlayerCharacterData => characterDatas.Where(a => a.IsPlayer).FirstOrDefault();
        public bool HasCharacterData => bandSize == characterDatas.Count;
    }
}