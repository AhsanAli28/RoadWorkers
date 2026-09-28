using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TMPro;
using UnityEngine;

public class ContractListing : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI damageName;
    [SerializeField] TextMeshProUGUI reward;
    [SerializeField] TextMeshProUGUI count;
    [SerializeField] TextMeshProUGUI time;
    [SerializeField] GameObject acceptedVisual;
    [SerializeField] GameObject acceptBtn;

    protected Panel panel;
    protected Contract contract;

    private void OnDestroy()
    {
        if (contract != null)
            contract.OnContractStateChange -= HandleContractStateChange;

        //Remove from list of the panel
        Panel_Office panel_Office = panel as Panel_Office;
        if (panel_Office != null)
            panel_Office.RemoveContractFromList(this);
    }

    public virtual void SetContract(Panel pPanel, Contract pContract)
    {
        panel = pPanel;
        contract = pContract;
        SetName(pContract.roadDamage.damageName);
        SetReward(pContract.reward);
        SetWorkersCount(pContract.roadDamage.minWorkers);
        SetTime(pContract.daysToFinish);

        contract.OnContractStateChange += HandleContractStateChange;
    }

    protected void SetName(string name)
    {
        damageName.text = name;
    }

    protected void SetReward(int pReward)
    {
        reward.text = pReward.ToString();
    }

    protected void SetWorkersCount(int pCount)
    {
        count.text = pCount.ToString();
    }

    protected void SetTime(int pSeconds)
    {
        UpdateTimerText(pSeconds);
    }

    public void UpdateCount()
    {

    }    

    void UpdateTimerText(float daysToFinish)
    {
        time.text = $"{daysToFinish.ToString()} days"; 
    }

    public void HandleContractStateChange(Contract pContract)
    {
        if (pContract == null)
        {
            Destroy(gameObject);
            return;
        }

        if (pContract.state == ContractState.Accepted)
        {
            //Update UI
            acceptBtn.gameObject.SetActive(false);
            acceptedVisual.gameObject.SetActive(true);
            time.text = pContract.deadline.Date.ToString("dd/MM/yyyy");
        }

        if (pContract.state == ContractState.Completed)
        {
            //TODO: Do some effects or accepting the reward button instead of just self destroying
            Destroy(gameObject);
            return;
        }

        if(pContract.state == ContractState.Canceled)
        {
            Destroy(gameObject);
            return;
        }
    }

    #region Buttons
    public void Btn_Accept()
    {
        contract.AcceptContract();
    }

    public void Btn_Find()
    {
        UIManager.Instance.CloseCurrentPanel();
        CameraController.Instance.FocusOnTarget(contract.roadDamage.transform);
    }

    #endregion
}
