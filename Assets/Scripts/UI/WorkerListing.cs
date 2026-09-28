using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkerListing : MonoBehaviour
{
    [SerializeField] Image workerIcon;
    [SerializeField] TextMeshProUGUI workerName;
    [SerializeField] TextMeshProUGUI energy;
    [SerializeField] TextMeshProUGUI skill1;
    [SerializeField] TextMeshProUGUI skill2;
    [SerializeField] TextMeshProUGUI status;
    [SerializeField] GameObject highlight;

    [SerializeField] protected Panel panel;
    [SerializeField] protected Worker worker;

    private void OnDestroy()
    {
        worker.OnStatusChange -= HandleStatusChange;
    }

    public void SetWorker(Panel pPanel, Worker pWorker)
    {
        panel = pPanel;
        worker = pWorker;
        worker.OnStatusChange += HandleStatusChange;
        //SetIcon(); TODO: Set random icon and model
        SetName(pWorker.workerName);
        SetEnergy(pWorker.energy);
        SetSkill1(pWorker.skills["Skill1"]);
        SetSkill2(pWorker.skills["Skill2"]);
        SetStatus(pWorker.status.ToString());
    }

    public void HandleStatusChange()
    {
        UpdateWorkerStatus();
        UpdateWorkerSkills();
        UpdateAssignmentAvaialability(worker.status == WorkerStatus.Available);
    }

    public void UpdateWorkerStatus()
    {
        SetEnergy(worker.energy);
        SetStatus(worker.status.ToString());
    }

    public void UpdateWorkerSkills()
    {
        //SetSkill1(worker.skills["Skills1"]);
        //SetSkill2(worker.skills["Skills2"]);
    }

    public virtual void UpdateAssignmentAvaialability(bool value)
    {
        
    }

    public void SetIcon(Sprite pSprite)
    {
        workerIcon.sprite = pSprite;
    }

    public void SetName(string pName)
    {
        workerName.text = pName;
    }

    public void SetEnergy(int pEnergy)
    {
        energy.text = $"{pEnergy.ToString()}%";
    }

    public void SetSkill1(int pValue)
    {
        skill1.text = pValue.ToString();
    }

    public void SetSkill2(int pValue)
    {
        skill2.text = pValue.ToString();
    }

    public void SetStatus(string pStatus)
    {
        status.text = pStatus;
    }

    public void ToggleSelect(bool pValue)
    {
        highlight.SetActive(pValue);
    }

    public void Btn_Select()
    {
        //Because the worker can be either in Office UI or Contract Assignment UI
        if (panel is Panel_Office)
        {
            Panel_Office panel_office = (Panel_Office)panel;
            panel_office.SelectWorker(this);
        }
    }

    public Worker GetWorker()
    {
        return worker;
    }
}
