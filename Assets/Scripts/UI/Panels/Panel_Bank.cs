using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class Panel_Bank : PanelTabs
{
    [Header("Finances")]
    [SerializeField] GameObject transactionPrefab;
    [SerializeField] Transform incomeContentParent;
    [SerializeField] Transform expenseContentParent;
    [SerializeField] TextMeshProUGUI txt_NETIncome;
    [SerializeField] List<Button> filterButtons;

    [Header("Loans")]
    [SerializeField] List<LoanListing> loanListings;
    [SerializeField] TextMeshProUGUI txt_activeLoans;
    [SerializeField] TextMeshProUGUI txt_weeklyPayment;
    [SerializeField] GameObject activeLoanPrefab;
    [SerializeField] Transform activeLoansContentParent;

    [Header("NET Worth")]
    [SerializeField] TextMeshProUGUI txt_NETWorth;
    [SerializeField] TextMeshProUGUI txt_bankAccount;
    [SerializeField] TextMeshProUGUI txt_tools;
    [SerializeField] TextMeshProUGUI txt_materials;
    [SerializeField] TextMeshProUGUI txt_property;
    [SerializeField] TextMeshProUGUI txt_loans;

    private TimeFiler currentFilter = TimeFiler.Month;

    protected override void Start()
    {
        base.Start();
        MoneyController.Instance.OnMoneyChange += HandleMoneyChange;
    }

    private void OnDestroy()
    {
        //MoneyController.Instance.OnMoneyChange -= HandleMoneyChange;
    }

    private void HandleMoneyChange(int oldMoney, int money)
    {
        SetInfo();
    }

    public void SetInfo()
    {
        DateTime timeFilter = GetStartTimeFromFilter();
        //Finances
        List<Transaction> reversedTransactions = new List<Transaction>(MoneyController.Instance.GetTransactions(timeFilter, TimeController.Instance.CurrentDate));
        reversedTransactions.Reverse();
        PopulateTransactions(reversedTransactions);
        SetNETIncome(MoneyController.Instance.GetNetIncome(timeFilter, TimeController.Instance.CurrentDate));

        //Loans
        SetLoansCount(MoneyController.Instance.ActiveLoans.Count);
        SetWeeklyPayment(MoneyController.Instance.GetTotalWeeklyLoanPayments());
        PopulateActiveLoans(MoneyController.Instance.ActiveLoans);
        UpdateLoansAvaialability();

        //NET Worth
        SetNETWorth();
    }

    public void PopulateTransactions(List<Transaction> transactions)
    {
        // Clear previous UI
        foreach (Transform child in incomeContentParent)
            Destroy(child.gameObject);
        foreach (Transform child in expenseContentParent)
            Destroy(child.gameObject);

        foreach (var t in transactions)
        {
            GameObject obj = Instantiate(transactionPrefab);
            TransactionListing listing = obj.GetComponent<TransactionListing>(); // or custom script for cleaner access

            string dateStr = t.Date.ToString("dd-MM-yy");
            string amountStr = t.Amount.ToString("N0");

            listing.SetDate(t.Date);
            listing.SetSource(t.Source);
            listing.SetAmount($"{(t.IsIncome ? "+" : "-")} ${t.Amount.ToString("N0")}");
            listing.SetColor(t.IsIncome);

            // Parent to appropriate list
            if (t.IsIncome)
                obj.transform.SetParent(incomeContentParent, false);
            else
                obj.transform.SetParent(expenseContentParent, false);
        }
    }

    public void PopulateActiveLoans(List<Loan> activeLoans)
    {
        // Clear previous listings
        foreach (Transform child in activeLoansContentParent)
            Destroy(child.gameObject);

        for (int i = 0; i < activeLoans.Count; i++)
        {
            GameObject obj = Instantiate(activeLoanPrefab);
            ActiveLoanListing listing = obj.GetComponent<ActiveLoanListing>();

            listing.SetInfo(activeLoans[i]);
            listing.LoanID = i;

            obj.transform.SetParent(activeLoansContentParent, false);
        }
    }


    public void SetupLoans(List<Loan> loans)
    {
        if (loans.Count != loanListings.Count)
        {
            Debug.LogError("Loans lists dont match. Either add more UI or remove some.");
            return;
        }

        for (int i = 0; i < loans.Count; i++)
        {
            LoanListing loanListing = loanListings[i];

            loanListing.SetLoanAmount(loans[i].Amount);
            loanListing.SetInterestRate(loans[i].AnnualInterestRate);
            loanListing.SetPeriod(loans[i].TotalPeriods);
            loanListing.SetWeeklyPayment(loans[i].WeeklyPayment);
        }
    }

    public void UpdateLoansAvaialability()
    {
        foreach (LoanListing loan in loanListings)
        {
            loan.ToggleLoanButton();
        }
    }



    public void SetNETIncome(int value)
    {
        txt_NETIncome.text = $"NET Income: ${value.ToString("N0")}";
    }

    public void SetNETWorth()
    {
        int netWorth = MoneyController.Instance.GetNETWorth();
        int bankAccount = MoneyController.Instance.Money;
        int tools = MoneyController.Instance.GetToolsValue();
        int materials = MoneyController.Instance.GetMaterialsValue();
        int property = MoneyController.Instance.GetProperyValue();
        int loans = MoneyController.Instance.GetLoansValue();

        SetNETAmount(txt_bankAccount, bankAccount);
        SetNETAmount(txt_tools, tools);
        SetNETAmount(txt_materials, materials);
        SetNETAmount(txt_property, property);
        SetNETAmount(txt_loans, loans);

        txt_NETWorth.text = $"NET Worth: ${netWorth.ToString("N0")}";
    }

    private void SetNETAmount(TextMeshProUGUI text, int amount)
    {
        text.text = $"{(amount >= 0 ? "+" : "")} ${amount.ToString("N0")}";
    }

    public void SetLoansCount(int amount)
    {
        txt_activeLoans.text = amount.ToString();
    }

    public void SetWeeklyPayment(float amount)
    {
        txt_weeklyPayment.text = $"Weekly payment: ${amount.ToString("#,0.0", CultureInfo.InvariantCulture)}"; // Format N0 + F1 (10,000.0 for example)
    }

    private DateTime GetStartTimeFromFilter()
    {
        switch (currentFilter)
        {
            case TimeFiler.Month:
                return TimeController.Instance.CurrentDate.AddDays(-31);
            case TimeFiler.Year:
                return TimeController.Instance.CurrentDate.AddDays(-356);
            case TimeFiler.Decade:
                return TimeController.Instance.CurrentDate.AddDays(-3560);
            default:
                return TimeController.Instance.CurrentDate.AddDays(-31);
        }
    }

    public void Btn_Filter1()
    {
        currentFilter = TimeFiler.Month;
        SetInfo();
    }

    public void Btn_Filter2()
    {
        currentFilter = TimeFiler.Year;
        SetInfo();
    }

    public void Btn_Filter3()
    {
        currentFilter = TimeFiler.Decade;
        SetInfo();
    }

    public void Btn_SelectButton(Button button)
    {
        foreach (Button item in filterButtons)
        {
            item.interactable = true;
        }

        button.interactable = false;
    }

    private enum TimeFiler
    {
        Month,
        Year,
        Decade
    }
}
