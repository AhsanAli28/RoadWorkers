using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] Slider progressBar;
    [SerializeField] GameObject loadingIcon;

    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
            progressBar.value = progress;
        loadingIcon.transform.Rotate(Vector3.forward, 180 * Time.unscaledDeltaTime);
    }
}
