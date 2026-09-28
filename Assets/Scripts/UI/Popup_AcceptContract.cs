using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Popup_AcceptContract : Popup
{
    public TextMeshProUGUI Txt_Title;
    public TextMeshProUGUI Txt_Reward;
    public TextMeshProUGUI Txt_Workers;
    public TextMeshProUGUI Txt_Tools;
    public TextMeshProUGUI Txt_Deadline;
    public List<GameObject> materialsObj;
    public List<TextMeshProUGUI> Txt_Materials;

    public void SetMaterialsInfo(Dictionary<string, int> materials)
    {
        int i = 0;
        foreach (var item in materials)
        {
            materialsObj[i].SetActive(true);
            Txt_Materials[i].text = item.Value.ToString();
            i++;
        }
    }
}
