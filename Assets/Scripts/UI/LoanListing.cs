using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoanListing : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txt_loanAmount;
    [SerializeField] TextMeshProUGUI txt_interestRate;
    [SerializeField] TextMeshProUGUI txt_weeklyPayment;
    [SerializeField] TextMeshProUGUI txt_period;
    [SerializeField] Button btn_loan;

    private int loanAmount;

    public void SetLoanAmount(int amount)
    {
        txt_loanAmount.text = $"${amount.ToString("N0")}";
        loanAmount = amount;
    }

    public void SetInterestRate(float rate)
    {
        txt_interestRate.text = $" Interest rate: {(rate * 100).ToString("F1")}%";
    }

    public void SetWeeklyPayment(float amount)
    {
        txt_weeklyPayment.text = $"${amount.ToString("#,0.0", CultureInfo.InvariantCulture)} / week"; // Format N0 + F1 (10,000.0 for example)
    }

    public void SetPeriod(int amount)
    {
        txt_period.text = $"Period: {amount.ToString()} weeks";
    }

    public void ToggleLoanButton()
    {
        btn_loan.interactable = MoneyController.Instance.CanLoanMoney(loanAmount);
    }
}
