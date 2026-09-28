using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class MoneyController : MonoBehaviour
{
    public int Money { get; private set; }
    public int LoanedMoney { get; private set; }

    public Action<int, int> OnMoneyChange;
    public List<Loan> ActiveLoans = new List<Loan>();

    private List<Transaction> transactions = new List<Transaction>();
    List<Loan> loans = new List<Loan>();

    //Singleton 
    private static MoneyController _instance;

    public static MoneyController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<MoneyController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<MoneyController>().Length > 1)
                {
                    Debug.LogWarning("More than one MoneyController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("MoneyController");
                    _instance = singletonObject.AddComponent<MoneyController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of MoneyController in the scene
        if (_instance == null)
        {
            _instance = this;
        }

        DelayRunner.Instance.Delay(0.1f, () => TimeController.Instance.OnMonthPassed += HandleMonthPassed);
        DelayRunner.Instance.Delay(0.1f, () => TimeController.Instance.OnWeekPassed += HandleWeekPassed);
        //GiveStartingMoney();
    }

    private void OnDisable()
    {
        TimeController.Instance.OnMonthPassed -= HandleMonthPassed;
        TimeController.Instance.OnWeekPassed -= HandleWeekPassed;
    }


    private void Start()
    {
        SetupLoans();
    }

    private void Update()
    {
        ProcessLoanPayments();

        // DEBUG
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    AddMoney(100000, "Cheat");
        //}
    }

    public void HandleLoad()
    {
        Money = SaveController.Instance.GetData().Money;
        transactions = new List<Transaction>(SaveController.Instance.GetData().Transactions);
        ActiveLoans = new List<Loan>(SaveController.Instance.GetData().Loans);
        OnMoneyChange?.Invoke(Money, Money);
    }

    private void GiveStartingMoney()
    {
        if (TimeController.Instance.CurrentDate == new DateTime(2000, 01, 01))
            Money = 50000;
        SaveController.Instance.UpdateData(x => x.Money = Money);
    }

    private void HandleMonthPassed(DateTime time)
    {
        PaySalaries();
    }

    private void HandleWeekPassed(DateTime time)
    {
        PayRents();
    }

    public void AddMoney(int amount, string source = "Other")
    {
        int oldMoney = Money;
        Money += amount;
        Transaction transaction = new Transaction(amount, source, true, TimeController.Instance.CurrentDate.ToString("o"));
        transactions.Add(transaction); // Use your game time
        OnMoneyChange?.Invoke(oldMoney, Money);
        SaveController.Instance.UpdateData(x => x.Money = Money);
        SaveController.Instance.UpdateData(x => x.Transactions.Add(transaction));
    }

    public bool SpendMoney(int amount, string source = "Other")
    {
        if (Money >= amount)
        {
            int oldMoney = Money;
            Money -= amount;
            Transaction transaction = new Transaction(amount, source, false, TimeController.Instance.CurrentDate.ToString("o"));
            transactions.Add(transaction);
            OnMoneyChange?.Invoke(oldMoney, Money);
            SaveController.Instance.UpdateData(x => x.Money = Money);
            SaveController.Instance.UpdateData(x => x.Transactions.Add(transaction));
            return true;
        }
        return false;
    }

    public void PayoutMoney(int amount, string source = "Other")
    {
        int oldMoney = Money;
        Money -= amount;
        Transaction transaction = new Transaction(amount, source, false, TimeController.Instance.CurrentDate.ToString("o"));
        transactions.Add(transaction);
        OnMoneyChange?.Invoke(oldMoney, Money);
        SaveController.Instance.UpdateData(x => x.Money = Money);
        SaveController.Instance.UpdateData(x => x.Transactions.Add(transaction));

    }

    private void PaySalaries()
    {
        print("Paying salaries");

        int totalSalaries = 0;
        foreach (var worker in WorkersController.Instance.GetHiredWorkers())
        {
            totalSalaries += worker.salary;
        }

        if (totalSalaries > 0)
        {
            PayoutMoney(totalSalaries, "Worker Salaries");
        }
    }

    private void PayRents()
    {
        print("Paying rents");
        int totalRent = 0;

        foreach (EquipmentInstance instance in InventoryController.Instance.Equipment.Instances)
        {
            if (instance.IsRented)
            {
                totalRent += CalculateEquipmentRent(instance.CurrentPrice);
            }
        }

        if (totalRent > 0)
        {
            PayoutMoney(totalRent, "Equipment Rent");
        }
    }

    #region Loans
    public void TakeLoan(int loanID)
    {
        if (loanID < 0 || loanID >= loans.Count)
        {
            Debug.LogError("Invalid loan ID");
            return;
        }

        var template = loans[loanID];
        // instantiate a fresh Loan, scheduling first payment next Monday @12
        var newLoan = new Loan(
            template.Id,
            template.Amount,
            template.AnnualInterestRate,
            template.TotalPeriods,
            template.WeeklyPayment,
            TimeController.Instance.CurrentDate);

        ActiveLoans.Add(newLoan);

        // give the principal to the player
        AddMoney(newLoan.Amount, "Loan");
        UIManager.Instance.RefreshBankUI();

        // Save it 
        SaveController.Instance.UpdateData(x => x.Loans.Add(newLoan));
    }

    private void ProcessLoanPayments()
    {
        DateTime now = TimeController.Instance.CurrentDate;

        foreach (var loan in ActiveLoans)
        {
            if (loan.IsPaidOff)
                continue;

            if (now >= loan.NextPaymentDate)
            {
                int pay = Mathf.CeilToInt(loan.WeeklyPayment);
                if (Money >= pay)
                {
                    PayoutMoney(pay, $"Loan Payment #{loan.Id}");
                    loan.MakeWeeklyPayment();

                    if (loan.IsPaidOff)
                        ActiveLoans.Remove(loan);
                }
                else
                {
                    // optional: handle default / partial payment
                    PayoutMoney(pay, $"Loan Payment #{loan.Id}");
                    loan.MakeWeeklyPayment();
                }

                UIManager.Instance.RefreshBankUI();
            }
        }
    }


    public void PayOffLoan(int takenLoanIndex)
    {
        if (takenLoanIndex < 0 || takenLoanIndex >= ActiveLoans.Count)
            return;

        Loan loan = ActiveLoans[takenLoanIndex];
        int payoff = Mathf.CeilToInt(loan.RemainingBalance);

        if (Money >= payoff)
        {
            PayoutMoney(payoff, $"Loan Payoff #{loan.Id}");
            // zero out remaining
            while (!loan.IsPaidOff)
                loan.MakeWeeklyPayment();

            ActiveLoans.Remove(loan);
            UIManager.Instance.RefreshBankUI();

            // Save
            SaveController.Instance.UpdateData(x => x.Loans.Remove(loan));
        }

    }

    public void SetupLoans()
    {
        loans.Clear(); // just in case

        loans.Add(new Loan(
            id: 0,
            principal: 10000,
            annualInterestRate: 0.20f,
            periods: 30,
            weeklyPayment: CalculateLoanInterestAmount(10000, 0.20f, 30),
            startDate: DateTime.MinValue // placeholder for templates
        ));

        loans.Add(new Loan(
            id: 1,
            principal: 50000,
            annualInterestRate: 0.20f,
            periods: 40,
            weeklyPayment: CalculateLoanInterestAmount(50000, 0.20f, 40),
            startDate: DateTime.MinValue
        ));

        loans.Add(new Loan(
            id: 2,
            principal: 100000,
            annualInterestRate: 0.15f,
            periods: 60,
            weeklyPayment: CalculateLoanInterestAmount(100000, 0.15f, 60),
            startDate: DateTime.MinValue
        ));

        loans.Add(new Loan(
            id: 3,
            principal: 500000,
            annualInterestRate: 0.10f,
            periods: 100,
            weeklyPayment: CalculateLoanInterestAmount(500000, 0.10f, 100),
            startDate: DateTime.MinValue
        ));

        // Push them to the UI
        UIManager.Instance.SetLoans(loans);
    }

    #endregion

    public int GetNETWorth()
    {
        return Money + GetToolsValue() + GetMaterialsValue() + GetProperyValue() + GetLoansValue();
    }

    public int GetToolsValue()
    {
        return InventoryController.Instance.GetInstancesValue(InventoryController.Instance.Equipment);
    }

    public int GetMaterialsValue()
    {
        return InventoryController.Instance.GetValueOfcategory(InventoryController.Instance.Materials);
    }

    public int GetProperyValue()
    {
        //TODO: add property value, like upgrades
        return 100000;
    }

    public int GetLoansValue()
    {
        // negative liability
        int sum = 0;
        foreach (var loan in ActiveLoans)
            sum -= Mathf.CeilToInt(loan.RemainingBalance);
        return sum;
    }

    public float CalculateLoanInterestAmount(int loanAmount, float annualInterestRate, int weeks)
    {
        float weeklyInterestRate = annualInterestRate / 52f;
        float r = weeklyInterestRate;
        int n = weeks;

        // Amortized loan formula:
        // P = L * r / (1 - (1 + r)^-n)
        float weeklyPayment = (loanAmount * r) / (1 - Mathf.Pow(1 + r, -n));

        return weeklyPayment;
    }

    public List<Transaction> GetTransactions(DateTime start, DateTime end)
    {
        return transactions.FindAll(t => t.Date >= start && t.Date <= end);
    }

    public int GetNetIncome(DateTime start, DateTime end)
    {
        var filtered = GetTransactions(start, end);
        int income = 0, expenses = 0;

        foreach (var t in filtered)
        {
            if (t.IsIncome) income += t.Amount;
            else expenses += t.Amount;
        }

        return income - expenses;
    }

    public float GetTotalWeeklyLoanPayments()
    {
        float total = 0f;

        foreach (Loan loan in ActiveLoans)
        {
            total += loan.WeeklyPayment;
        }

        return total;
    }

    public bool CanAfford(int price)
    {
        return Money >= price;
    }

    public bool CanLoanMoney(int newLoanAmount)
    {
        int maxTotalLoan = Mathf.RoundToInt(GetNETWorth() * 2f); // Allow borrowing up to 2x net worth

        //Max 10 loans as well
        if (LoanedMoney + newLoanAmount > maxTotalLoan || ActiveLoans.Count >= 10)
        {
            return false;
        }

        return true;
    }

    public int CalculateEquipmentRent(float fullPrice)
    {

        return Mathf.RoundToInt(fullPrice / 80);
    }

    public int CalculateMaintenance(EquipmentInstance equipment)
    {
        float condition = equipment.CurrentCondition;
        float damage = Mathf.Clamp01(1f - condition / 100f);

        // Base maintenance cost curve
        float baseCost = Mathf.Lerp(1000f, 10000f, damage);

        if (condition == 0)
            baseCost = 30000f;

        return Mathf.RoundToInt(baseCost);
    }


}

[System.Serializable]
public struct Transaction
{
    public int Amount;
    public string Source;
    public bool IsIncome;
    public string DateString;
    public DateTime Date
    {
        get => DateTime.Parse(DateString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        set => DateString = value.ToString("o");
    }

    public Transaction(int amount, string source, bool isIncome, string dateString)
    {
        Amount = amount;
        Source = source;
        IsIncome = isIncome;
        DateString = dateString;
    }
}

[Serializable]
public class Loan
{
    public int Id;
    public int Amount;
    public float AnnualInterestRate;
    public int TotalPeriods;
    public float WeeklyPayment;
    public int RemainingPeriods;
    public string NextPaymentDateString;

    public DateTime NextPaymentDate
    {
        get => DateTime.Parse(NextPaymentDateString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        set => NextPaymentDateString = value.ToString("o");
    }

    public bool IsPaidOff => RemainingPeriods <= 0;
    public float RemainingBalance => RemainingPeriods * WeeklyPayment;

    public Loan(int id, int principal, float annualInterestRate, int periods, float weeklyPayment, DateTime startDate)
    {
        Id = id;
        Amount = principal;
        AnnualInterestRate = annualInterestRate;
        TotalPeriods = periods;
        WeeklyPayment = weeklyPayment;
        RemainingPeriods = periods;
        NextPaymentDate = CalculateNextMondayNoon(startDate);
    }

    public void MakeWeeklyPayment()
    {
        if (RemainingPeriods <= 0) return;
        RemainingPeriods--;
        NextPaymentDate = NextPaymentDate.AddDays(7);
    }

    private DateTime CalculateNextMondayNoon(DateTime from)
    {
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && from.TimeOfDay >= TimeSpan.FromHours(12))
            daysUntilMonday = 7;

        return from.Date.AddDays(daysUntilMonday).AddHours(12);
    }

    // Required for deserialization
    public Loan() { }
}

