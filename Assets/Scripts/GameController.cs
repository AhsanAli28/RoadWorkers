using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController _instance;

    public static GameController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameController>();

                if (_instance == null)
                {
                    Debug.LogWarning("No GameController instance found in the scene.");
                }
                else if (FindObjectsOfType<GameController>().Length > 1)
                {
                    Debug.LogWarning("Multiple GameController instances found. Keeping the first and destroying the rest.");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Play BG music
        AudioManager.Instance?.Play_IngameMusic();
        //AudioManager.Instance?.Play_CitySounds();
    }
}
