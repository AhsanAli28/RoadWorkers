using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Spirit604.Gameplay.Common
{
    public class DataSaver : MonoBehaviour
    {
        [Header("Meta")]
        public string persisterName;

        [Header("Scriptable Objects")]
        public List<ScriptableObject> objectsToPersist;

        protected void OnEnable()
        {
            Load();
        }

        protected void OnDisable()
        {
            Save();
        }

        private void Load()
        {
            for (int i = 0; i < objectsToPersist.Count; i++)
            {
                if (File.Exists(Application.persistentDataPath + string.Format("/{0}_{1}.pso", persisterName, i)))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(Application.persistentDataPath + string.Format("/{0}_{1}.pso", persisterName, i), FileMode.Open);
                    JsonUtility.FromJsonOverwrite((string)bf.Deserialize(file), objectsToPersist[i]);
                    file.Close();

                }
                else
                {
                    // Do Nothing
                }
            }
        }

        private void Save()
        {
            for (int i = 0; i < objectsToPersist?.Count; i++)
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Create(Application.persistentDataPath + string.Format("/{0}_{1}.pso", persisterName, i));
                var json = JsonUtility.ToJson(objectsToPersist[i]);
                bf.Serialize(file, json);
                file.Close();
            }
        }
    }
}