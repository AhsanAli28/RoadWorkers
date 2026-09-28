using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Panel_Gameplay : Panel
{
    [SerializeField] TextMeshProUGUI txt_Date;
    [SerializeField] TextMeshProUGUI txt_Time;
    [SerializeField] TextMeshProUGUI txt_Money;
    [SerializeField] List<Button> timeButtons;
    [SerializeField] GameObject incomePopupPrefab;
    [SerializeField] RectTransform incomePopupParent;

    private void Awake()
    {
        MoneyController.Instance.OnMoneyChange += SetMoneyText;
    }


    private void OnDestroy()
    {
        //MoneyController.Instance.OnMoneyChange -= SetMoneyText;
    }

    public void Btn_SelectButton(Button button)
    {
        foreach (Button item in timeButtons)
        {
            item.interactable = true;
        }

        button.interactable = false;
    }

    public void Btn_Options()
    {

    }

    public void SetTimeAndDate(DateTime time)
    {
        if (txt_Date != null)
            txt_Date.text = time.ToString("dd/MM/yyyy");

        if (txt_Time != null)
            txt_Time.text = time.ToString("HH:00");
    }

    private void SetMoneyText(int oldMoney, int newMoney)
    {
        txt_Money.text = $"${newMoney.ToString("N0")}";

        // Kill ongoing tweens on the transform
        txt_Money.transform.DOKill();
        // Start fresh
        txt_Money.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f, 1, 0.5f);

        // Play effect if its an income
        if(oldMoney < newMoney)
        {
            ShowIncomeEffect(newMoney - oldMoney);
        }
    }

    public void ShowIncomeEffect(int amount)
    {
        GameObject popup = Instantiate(incomePopupPrefab, incomePopupParent);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchoredPosition = incomePopupParent.anchoredPosition;

        popup.GetComponent<IncomePopup>().ShowPopup(amount);
        AudioManager.Instance?.Play_CashierCoins();
    }

}
