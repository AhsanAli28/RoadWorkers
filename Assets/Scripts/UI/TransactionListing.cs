using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TransactionListing : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txt_Date;
    [SerializeField] TextMeshProUGUI txt_Source;
    [SerializeField] TextMeshProUGUI txt_Amount;
    [SerializeField] Color incomeColor = Color.green;
    [SerializeField] Color expenseColor = Color.red;

    public void SetDate(DateTime time)
    {
        txt_Date.text = time.ToString("dd/MM/yy");
    }

    public void SetSource(string name)
    {
        txt_Source.text = name;
    }

    public void SetAmount(string amount)
    {
        txt_Amount.text = amount;
    }

    public void SetColor(bool isIncome = true)
    {
        txt_Amount.color = isIncome ? incomeColor : expenseColor;
    }
}
