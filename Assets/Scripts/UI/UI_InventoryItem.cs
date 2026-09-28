using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InventoryItem : MonoBehaviour
{
    public bool DisplayAvaiables = true;
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI Txt_name;
    [SerializeField] TextMeshProUGUI Txt_count;

    private InventoryCategory assignedCategory;
    private string itemID;

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetName(string name)
    {
        Txt_name.text = name;
    }

    public void SetItemID(string itemID)
    {
        this.itemID = itemID;
    }

    public void SetCount(int totalCount, int availableCount)
    {
        if (DisplayAvaiables)
        {
            Txt_count.text = $"{availableCount.ToString()} / {totalCount.ToString()}";
        }
        else
        {
            Txt_count.text = $"{totalCount.ToString()}";
        }
    }

    public void HandleItemUpdate(string itemID, int totalCount, int availableCount)
    {
        if (this.itemID == itemID)
            SetCount(totalCount, availableCount);
    }

    //Used to unsubscribe from the event
    public void SubscribeToInventoryCategory(InventoryCategory category)
    {
        assignedCategory = category;
        if (assignedCategory != null)
            assignedCategory.OnItemUpdated += HandleItemUpdate;
    }

    private void OnDestroy()
    {
        if (assignedCategory != null)
            assignedCategory.OnItemUpdated -= HandleItemUpdate;
    }
}
