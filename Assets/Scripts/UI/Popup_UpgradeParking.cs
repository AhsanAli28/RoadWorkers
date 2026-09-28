using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup_UpgradeParking : Popup
{
    [SerializeField] int price_small;
    [SerializeField] int price_large;
    [SerializeField] Button btn_Build_Small;
    [SerializeField] Button btn_Build_Large;
    [SerializeField] Button btn_Icon;
    [SerializeField] TextMeshProUGUI txt_price_small;
    [SerializeField] TextMeshProUGUI txt_price_large;
    private void Awake()
    {
        txt_price_small.text = "$" + price_small.ToString("N0");
        txt_price_large.text = "$" + price_large.ToString("N0");
    }

    public void Btn_Build_Small()
    {
        if (!MoneyController.Instance.CanAfford(price_small))
        {
            Pop_Out();
            return;
        }

        CompanyController.Instance.BuySmallSpot();


        Pop_Out();
    }

    public void Btn_Build_Large()
    {
        if (!MoneyController.Instance.CanAfford(price_large))
        {
            Pop_Out();
            return;
        }

        CompanyController.Instance.BuyLargeSpot();

        Pop_Out();
    }

    public void Btn_Icon()
    {
        Pop_In();
        UpdateBuildButton();
    }

    public void UpdateBuildButton()
    {
        btn_Build_Small.interactable = MoneyController.Instance.CanAfford(price_small);
        btn_Build_Large.interactable = MoneyController.Instance.CanAfford(price_large);
    }

    public void ToggleIconButton(bool value)
    {
        btn_Icon.gameObject.SetActive(value);
    }
}
