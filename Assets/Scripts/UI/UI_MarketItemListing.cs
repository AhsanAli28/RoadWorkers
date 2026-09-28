using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MarketItemListing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI Txt_name;
    [SerializeField] TextMeshProUGUI Txt_price;
    [SerializeField] TextMeshProUGUI Txt_count;
    [SerializeField] Button btn_reduce;

    [Header("Paramters")]
    [SerializeField] private int changeIteration;

    private int itemCount;
    private string itemID;
    private Panel_MaterialsMarket panel_MaterialsMarket;

    public void SetPanel(Panel_MaterialsMarket market)
    {
        panel_MaterialsMarket = market;
    }

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetItemID(string pItemID)
    {
        itemID = pItemID;
    }

    public void SetName(string name)
    {
        Txt_name.text = name;
    }

    public void SetPrice(int price)
    {
        Txt_price.text = $"$ {price.ToString("N0")}";
    }

    private void UpdateCount()
    {
        Txt_count.text = $"{itemCount.ToString("N0")} m³";
        btn_reduce.interactable = itemCount > 0;
    }

    public void SetCount(int count)
    {
        itemCount = count;
        UpdateCount();
        panel_MaterialsMarket.SetItem(itemID, itemCount);
    }

    public void AddMaterialCount()
    {
        itemCount += changeIteration;
        UpdateCount();
        panel_MaterialsMarket.SetItem(itemID, itemCount);
    }

    public void ReduceMaterialCount()
    {
        itemCount -= changeIteration;

        if(itemCount < 0)
            itemCount = 0;

        UpdateCount();
        panel_MaterialsMarket.SetItem(itemID, itemCount);
    }

    public void Btn_Add()
    {
        AddMaterialCount();
    }

    public void Btn_Reduce()
    {
        ReduceMaterialCount();
    }
}
