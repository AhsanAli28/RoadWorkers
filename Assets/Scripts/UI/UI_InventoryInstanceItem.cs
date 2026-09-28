using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_InventoryInstanceItem : UI_InventoryItem
{
    protected InventoryInstanceCategory assignedInstanceCategory;
    protected ItemInstance assignedInstance;
    protected UI_Inventory inventoryUI;

    public void SetPanel(UI_Inventory inventoryUI)
    {
        this.inventoryUI = inventoryUI;
    }
    public void SetInstance(ItemInstance instance)
    {
        assignedInstance = instance;
        assignedInstance.OnInstanceUpdated += HandleInstanceUpdate;
    }

    // Used to unsubscribe from the event
    public void SubscribeToInventoryCategory(InventoryInstanceCategory category)
    {
        assignedInstanceCategory = category;
    }

    protected virtual void HandleInstanceUpdate(ItemInstance instance)
    {

    }

    private void OnDestroy()
    {
        if(assignedInstance != null)
            assignedInstance.OnInstanceUpdated -= HandleInstanceUpdate;
    }
}
