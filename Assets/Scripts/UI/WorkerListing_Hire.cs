using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WorkerListing_Hire : WorkerListing
{
    [SerializeField] TextMeshProUGUI txt_Salary;

    public void SetSalary(int amount)
    {
        txt_Salary.text = $"${amount.ToString("N0")} / month";
    }

    public void Btn_Hire()
    {
        WorkersController.Instance.HireWorker(worker);
        worker.SaveData();
    }
}
