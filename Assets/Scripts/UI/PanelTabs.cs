using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelTabs : Panel
{
    [SerializeField] List<GameObject> _tabs = new List<GameObject>();

    private GameObject currentTab;

    protected virtual void Start()
    {
        SwitchTabID(0);
    }

    public void SwitchTab(GameObject tab)
    {
        if (currentTab == tab)
            return;

        tab.SetActive(true);
        if (currentTab != null)
            currentTab.SetActive(false);
        currentTab = tab;
    }

    public void SwitchTabID(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            Debug.LogWarning("Trying to swtch tab but index is out of range.");
            return;
        }

        if (currentTab == _tabs[index])
            return;

        _tabs[index].SetActive(true);
        if (currentTab != null)
            currentTab.SetActive(false);
        currentTab = _tabs[index];
    }
}
