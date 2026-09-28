using Spirit604.Attributes;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class FPSDisplay : MonoBehaviour
    {
        private const int MaxIndex = 10000;
        private const int MaxFPS = 1000;
        private const string NanText = "NaN";

        [SerializeField]
        private GameObject fpsPanel;

        [SerializeField]
        private TextMeshProUGUI currentFPSText;

        [SerializeField]
        private TextMeshProUGUI minFPSText;

        [SerializeField]
        private TextMeshProUGUI avgFPSText;

        [SerializeField]
        private TextMeshProUGUI maxFSPText;

        [SerializeField]
        private TextMeshProUGUI minOnePercFPSText;

        [SerializeField]
        private TextMeshProUGUI minZeroOnePercFPSText;

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private float updateInterval = 0.05f;

        [SerializeField]
        private float uiUpdateInterval = 0.25f;

        private float currentFPS = 0;
        private float averageFPS = 0;
        private float minimumFPS = 0;
        private float maximumFPS = 0;

        private int totalFrameCount, tempFrameCount, tickCount;
        private double tStamp, tStampTemp;
        private double uiTStampTemp;

        private int[] fpsData;
        private string[] fpsStrings;
        private int currentMaxIndex;
        private int currentMinIndex;
        private float minOnePerc;
        private float minZeroOnePerc;

        private void Awake()
        {
            if (resetButton)
            {
                resetButton.onClick.AddListener(() => ResetCounter(false));
            }
        }

        private void OnEnable()
        {
            Initialize();
            Enable();
        }

        private void Update()
        {
            CalcFps();
        }

        private void CalcFps()
        {
            tempFrameCount++;
            totalFrameCount++;

            if (Time.realtimeSinceStartup - tStampTemp > updateInterval)
            {
                tickCount++;
                currentFPS = (float)(tempFrameCount / (Time.realtimeSinceStartup - tStampTemp));
                averageFPS = (float)(totalFrameCount / (Time.realtimeSinceStartup - tStamp));
                if (currentFPS < minimumFPS) minimumFPS = currentFPS;
                if (currentFPS > maximumFPS) maximumFPS = currentFPS;

                tStampTemp = Time.realtimeSinceStartup;

                var currentFpsRounded = (float)Math.Round(currentFPS, 1);
                var currentFpsIndex = Mathf.Clamp(Mathf.RoundToInt(currentFpsRounded * 10), 0, MaxIndex - 1);

                if (currentFpsIndex > currentMaxIndex)
                {
                    currentMaxIndex = currentFpsIndex;
                }

                if (currentFpsIndex < currentMinIndex)
                {
                    currentMinIndex = currentFpsIndex;
                }

                fpsData[currentFpsIndex]++;

                var minOneIndex = currentMinIndex + Mathf.RoundToInt((float)(currentMaxIndex - currentMinIndex) * 0.01f);
                var minZeroOneIndex = currentMinIndex + Mathf.RoundToInt((float)(currentMaxIndex - currentMinIndex) * 0.001f);

                var startIndex = currentMinIndex;
                var minOneFpsCount = Mathf.RoundToInt(tickCount * 0.01f);
                var minZeroOneFpsCount = Mathf.RoundToInt(tickCount * 0.001f);
                var counter = 0;
                var fpsCounter2 = 0;
                var fpsCounter = 0;
                minOnePerc = 0;

                while (true)
                {
                    if (startIndex >= currentMaxIndex + 1)
                    {
                        break;
                    }

                    if (fpsData[startIndex] > 0)
                    {
                        var ticks = fpsData[startIndex];
                        counter += ticks;
                        fpsCounter += startIndex;
                        fpsCounter2 += startIndex * ticks;
                    }

                    if (counter >= minOneFpsCount && counter > 0)
                    {
                        minOnePerc = (float)(fpsCounter2) / counter * 0.1f;
                        break;
                    }

                    startIndex++;
                }

                startIndex = currentMinIndex;
                counter = 0;
                fpsCounter2 = 0;
                fpsCounter = 0;
                minZeroOnePerc = 0;

                while (true)
                {
                    if (startIndex >= currentMaxIndex + 1)
                    {
                        break;
                    }

                    if (fpsData[startIndex] > 0)
                    {
                        var ticks = fpsData[startIndex];
                        counter += ticks;
                        fpsCounter += startIndex;
                        fpsCounter2 += startIndex * ticks;
                    }

                    if (counter >= minZeroOneFpsCount && counter > 0)
                    {
                        minZeroOnePerc = (float)(fpsCounter2) / counter * 0.1f;
                        break;
                    }

                    startIndex++;
                }

                tempFrameCount = 0;
            }

            if (Time.realtimeSinceStartup - uiTStampTemp > uiUpdateInterval)
            {
                uiTStampTemp = Time.realtimeSinceStartup;

                currentFPSText.SetText(GetFpsText(currentFPS));
                avgFPSText.SetText(GetFpsText(averageFPS));
                maxFSPText.SetText(GetFpsText(maximumFPS));

                if (minFPSText)
                {
                    minFPSText.SetText(GetFpsText(minimumFPS));
                }

                if (minOnePercFPSText)
                {
                    if (minOnePerc > 0)
                    {
                        minOnePercFPSText.SetText(GetFpsText(minOnePerc));
                    }
                    else
                    {
                        minOnePercFPSText.SetText(NanText);
                    }
                }

                if (minZeroOnePercFPSText)
                {
                    if (minZeroOnePerc > 0)
                    {
                        minZeroOnePercFPSText.SetText(GetFpsText(minZeroOnePerc));
                    }
                    else
                    {
                        minZeroOnePercFPSText.SetText(NanText);
                    }
                }
            }
        }

        public void Stop()
        {
            enabled = false;
        }

        public void Enable()
        {
            enabled = true;
            fpsPanel.gameObject.SetActive(true);
            tStampTemp = Time.realtimeSinceStartup;
            uiTStampTemp = Time.realtimeSinceStartup;
        }

        [Button]
        public void ResetCounter(bool enableAfterReset = false)
        {
            Initialize();

            tStamp = Time.realtimeSinceStartup;
            tStampTemp = Time.realtimeSinceStartup;
            uiTStampTemp = Time.realtimeSinceStartup;

            currentFPS = 0;
            averageFPS = 0;
            minimumFPS = 999.9f;
            maximumFPS = 0;

            tempFrameCount = 0;
            totalFrameCount = 0;
            tickCount = 0;

            minOnePerc = 0;
            minZeroOnePerc = 0;

            currentMaxIndex = 0;
            currentMinIndex = int.MaxValue;

            for (int i = 0; i < fpsData.Length; i++)
            {
                fpsData[i] = 0;
            }

            if (enableAfterReset)
            {
                Enable();
            }
        }

        public void ResetWithDelay(float delayDuration, bool enableAfterReset = false, Func<bool> waitCallback = null)
        {
            StartCoroutine(ResetCoroutine(delayDuration, enableAfterReset));
        }

        private string GetFpsText(float fps)
        {
            var fpsIndex = Mathf.Clamp(Mathf.RoundToInt(fps * 10), 0, fpsStrings.Length - 1);

            return fpsStrings[fpsIndex];
        }

        private IEnumerator ResetCoroutine(float delayDuration, bool enableAfterReset = false, Func<bool> waitCallback = null)
        {
            if (waitCallback != null)
            {
                yield return new WaitWhile(waitCallback);
            }

            if (delayDuration > 0)
            {
                yield return new WaitForSeconds(delayDuration);
            }

            ResetCounter(enableAfterReset);
        }

        private void Initialize()
        {
            if (fpsStrings == null || fpsStrings.Length == 0)
            {
                fpsData = new int[MaxIndex];
                fpsStrings = new string[MaxFPS * 10];

                for (int i = 0; i < fpsStrings.Length; i++)
                {
                    float value = (float)Math.Round((float)i / 10, 1);

                    fpsStrings[i] = value.ToString("0.0");
                }
            }
        }
    }
}