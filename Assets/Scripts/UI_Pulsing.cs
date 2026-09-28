using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Pulsing : MonoBehaviour
{
    public float pulseDuration = 0.6f; // Time for one pulse
    public float scaleMultiplier = 1.1f; // How much it grows

    private void Start()
    {
        PulseButton();
    }

    private void PulseButton()
    {
        transform.DOScale(Vector3.one * scaleMultiplier, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo); // Loops forever
    }
}
