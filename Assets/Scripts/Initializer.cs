using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
    void Start()
    {
        int loadGame = PlayerPrefs.GetInt("LoadGame", 1); // Default to new game

        if (loadGame == 1)
        {
            // Add a delay so all the singletons and subscribers have time to link events and such.
            StartCoroutine(Load());
        }
        else if (loadGame == 0)
        {
            PlayerPrefs.SetInt("LoadGame", 1);
            SaveController.Instance.ResetSave();
            // Add a delay so all the singletons and subscribers have time to link events and such.
            StartCoroutine(Load());
        }

    }

    /// <summary>
    /// Step by step init with loading and handling of loads. Order matters 
    /// </summary>
    private IEnumerator Load()
    {
        yield return new WaitForSeconds(0.3f);

        // First load the game game data
        SaveController.Instance.Load();
        yield return new WaitForEndOfFrame();

        // Implement the data
        TimeController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        MoneyController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        CompanyController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        InventoryController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        RoadsController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        ContractsController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        WorkersController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        MarketController.Instance.HandleLoad();
        yield return new WaitForEndOfFrame();

        AudioManager.Instance.HandleLoad();
    }
}
