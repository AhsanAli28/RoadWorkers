using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Globalization;

public class TimeController : MonoBehaviour
{
    public static TimeController Instance;

    public float secondsPerInGameDay = 60f;
    public float timeSpeedMultiplier = 1f;

    public DateTime CurrentDate { get; private set; }
    private float timeAccumulator = 0f;

    public Action<DateTime> OnDayPassed;
    public Action<DateTime> OnMonthPassed;
    public Action<DateTime> OnWeekPassed;

    private int lastMonth = -1; // Track previous month
    private int lastWeek = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void HandleLoad()
    {
        CurrentDate = SaveController.Instance.GetData().GetDate();
        lastMonth = CurrentDate.Month;

        if (UIManager.Instance != null)
            UIManager.Instance.SetTimeAndDate(CurrentDate);
    }

    private void Update()
    {
        float adjustedDeltaTime = Time.deltaTime * timeSpeedMultiplier;
        timeAccumulator += adjustedDeltaTime;

        float secondsPerHour = secondsPerInGameDay / 24f;

        while (timeAccumulator >= secondsPerHour)
        {
            timeAccumulator -= secondsPerHour;
            CurrentDate = CurrentDate.AddHours(1);

            if (UIManager.Instance != null)
                UIManager.Instance.SetTimeAndDate(CurrentDate);

            SaveController.Instance.UpdateData(x => x.SetDate(CurrentDate));
            if (CurrentDate.Hour == 0)
            {
                OnDayPassed?.Invoke(CurrentDate);

                // Check for month change
                if (CurrentDate.Month != lastMonth)
                {
                    lastMonth = CurrentDate.Month;
                    OnMonthPassed?.Invoke(CurrentDate);
                }

                var currentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(CurrentDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                if (currentWeek != lastWeek)
                {
                    lastWeek = currentWeek;
                    OnWeekPassed?.Invoke(CurrentDate);
                }
            }
        }
    }

    public void SetTimeSpeed(int multiplier)
    {
        timeSpeedMultiplier = multiplier;
        Time.timeScale = multiplier;
    }
}
