using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveLoanListing : MonoBehaviour
{
    public int LoanID;
    [SerializeField] Button btn_Payout;
    [SerializeField] TextMeshProUGUI txt_periods;
    [SerializeField] TextMeshProUGUI txt_originalAmount;
    [SerializeField] TextMeshProUGUI txt_remainingAmount;
    [SerializeField] TextMeshProUGUI txt_weeklyPayment;

    public void SetOriginalAmount(int value)
    {
        txt_originalAmount.text = $"${value.ToString("N0")} loan";
    }

    public void SetPeriods(int paid, int total)
    {
        txt_periods.text = $"Periods: {paid.ToString()} / {total.ToString()}";
    }

    public void SetRemainingAmount(float amount)
    {
        txt_remainingAmount.text = $"To payout: ${amount.ToString("#,0.0", CultureInfo.InvariantCulture)}"; // Format N0 + F1 (10,000.0 for example)
    }

    public void SetWeeklyPayment(float amount)
    {
        txt_weeklyPayment.text = $"Weekly: ${amount.ToString("#,0.0", CultureInfo.InvariantCulture)}"; // Format N0 + F1 (10,000.0 for example)
    }

    public void SetInfo(Loan loan)
    {
        SetOriginalAmount(loan.Amount);
        SetPeriods(loan.TotalPeriods - loan.RemainingPeriods, loan.TotalPeriods);
        SetRemainingAmount(loan.RemainingBalance);
        SetWeeklyPayment(loan.WeeklyPayment);

        TogglePayoutButton(loan.RemainingBalance < MoneyController.Instance.Money);
    }

    public void TogglePayoutButton(bool value)
    {
        btn_Payout.interactable = value;
    }

    public void Btn_Payout()
    {
        MoneyController.Instance.PayOffLoan(LoanID);
    }
}
