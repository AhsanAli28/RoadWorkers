using UnityEngine;

public class Panel_Options : Panel
{
    float rememberedTimeSpeed = 1;
    [SerializeField] Panel_SettingsMenu settingsMenu;

    private void Update()
    {
        // TODO: Move to input controller or something 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!panelObj.activeSelf && !settingsMenu.GetPanelObject().activeSelf)
                Btn_Options();
            else
                Btn_Resume();
        }
    }

    public void Btn_Options()
    {
        print("Options");
        UIManager.Instance.ShowPanel(this);
        rememberedTimeSpeed = TimeController.Instance.timeSpeedMultiplier;
        TimeController.Instance.SetTimeSpeed(0);
    }

    public void Btn_Resume()
    {
        UIManager.Instance.CloseCurrentPanel();
        settingsMenu?.ClosePanel();
        TimeController.Instance.SetTimeSpeed(Mathf.RoundToInt(rememberedTimeSpeed));
    }

    public void Btn_BackToMenu()
    {
        //SceneController.Instance.UnloadMainScene();
        Btn_Resume();
        SaveController.Instance.Save();
        SceneController.Instance.LoadMainMenu();
    }

    public void Btn_Quit()
    {
        Application.Quit();
    }

    #region Settings



    #endregion

}
