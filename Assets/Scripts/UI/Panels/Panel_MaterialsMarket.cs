using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Panel_MaterialsMarket : Panel
{
    [Header("References")]
    [SerializeField] ItemsDatabase _itemsDatabase;
    [SerializeField] GameObject materialUIListingPrefab;
    [SerializeField] Transform contentParent;
    [SerializeField] TextMeshProUGUI txt_totalPrice;
    [SerializeField] Button btn_purchase;

    [Header("Variables")]
    private Dictionary<string, int> addedItems = new Dictionary<string, int>();
    private List<UI_MarketItemListing> listedItems = new List<UI_MarketItemListing>();

    private int totalPrice;

    private void Awake()
    {
        OnPanelClosed += ClearMarket;
    }

    public void Start()
    {
        PopulateMaterials();
    }

    public void Btn_Purchase()
    {
        ConfirmPurchase();
    }

    public void PopulateMaterials()
    {
        foreach (MaterialItem item in _itemsDatabase.materialItems)
        {
            GameObject materialListingObj = Instantiate(materialUIListingPrefab, contentParent);
            UI_MarketItemListing materialListing = materialListingObj.GetComponent<UI_MarketItemListing>();

            materialListing.SetPanel(this);
            materialListing.SetItemID(item.ItemID);
            materialListing.SetName(item.Name);
            materialListing.SetCount(0);
            materialListing.SetIcon(item.Icon);
            materialListing.SetPrice(item.Price);
            listedItems.Add(materialListing);
        }
    }

    public void ClearMarket()
    {
        foreach (UI_MarketItemListing item in listedItems)
        {
            item.SetCount(0);
        }

        addedItems.Clear();
        UpdateTotalPrice();
    }

    public void ConfirmPurchase()
    {
        if(addedItems.Count == 0)        
            return;        

        MarketController.Instance.BuyMaterials(addedItems, totalPrice);

        ClearMarket();
    }

    public void SetItem(string item, int amount)
    {
        if (amount > 0)
        {
            addedItems[item] = amount;
        }
        else
        {
            // If amount is 0 or less, remove the item from the dictionary
            addedItems.Remove(item);
        }

        UpdateTotalPrice();
    }

    //public void AddItem(string itemID, int amount)
    //{
    //    if (addedItems.ContainsKey(itemID))
    //    {
    //        addedItems[itemID] += amount;
    //    }
    //    else
    //    {
    //        addedItems[itemID] = amount;
    //    }

    //    UpdateTotalPrice();
    //}

    //public void RemoveItem(string itemID, int amount)
    //{
    //    if (addedItems.ContainsKey(itemID))
    //    {
    //        addedItems[itemID] -= amount;
    //        if (addedItems[itemID] <= 0)
    //        {
    //            addedItems.Remove(itemID);
    //        }
    //    }

    //    UpdateTotalPrice();
    //}

    private void UpdateTotalPrice()
    {
        totalPrice = 0;
        foreach (var item in addedItems)
        {
            totalPrice += _itemsDatabase.GetItem(item.Key).Price * item.Value;
        }

        txt_totalPrice.text = $"Total: $ {totalPrice.ToString("N0")}";
        btn_purchase.interactable = totalPrice > 0 && totalPrice <= MoneyController.Instance.Money;
    }
}
