using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Contract
{
    public RoadDamage roadDamage;
    public string roadDamageID; // For save
    public ContractState state = ContractState.Available;
    public Action<Contract> OnContractStateChange;

    public int reward;
    public string deadlineString;
    public DateTime deadline
    {
        get => DateTime.Parse(deadlineString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        set => deadlineString = value.ToString("o");
    }

    public int daysToFinish;
    public float progress;

    public Contract(int pReward, RoadDamage pRoadDamage, int pDaysToFinish)
    {
        reward = pReward;
        daysToFinish = pDaysToFinish;
        roadDamageID = pRoadDamage.RoadDamageID;
        roadDamage = pRoadDamage;
        roadDamage.SetAssignedContract(this);
    }

    public void SetContractState(ContractState state)
    {
        if (this.state == state) return;

        this.state = state;
        OnContractStateChange?.Invoke(this);
    }

    public void AcceptContract()
    {
        //Set the deadline before changing state since ohters may depend on it 
        deadline = TimeController.Instance.CurrentDate.AddDays(daysToFinish);

        SetContractState(ContractState.Accepted);
    }

    public void StartContract()
    {
        SetContractState(ContractState.InProgress);
    }

    public void CompleteContract()
    {
        // Dont do anything if its already completed
        if (state == ContractState.Completed)
            return;

        SetContractState(ContractState.Completed);
    }

    public void FailContract()
    {
        SetContractState(ContractState.Failed);
    }
}


public enum ContractState
{
    Available,
    Accepted,
    InProgress,
    Completed,
    Failed,
    Canceled
}