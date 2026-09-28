using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveChecker : MonoBehaviour
{
    public string SaveFileName;

    public bool DoesSaveFileExistPersistent()
    {
        return File.Exists(Path.Combine(Application.persistentDataPath, SaveFileName));
    }
}

