using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DelayRunner : MonoBehaviour
{
    private static DelayRunner _instance;

    public static DelayRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject runnerObj = new GameObject("CoroutineRunner");
                _instance = runnerObj.AddComponent<DelayRunner>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Calls an Action after a delay in seconds.
    /// </summary>
    /// <param name="pDelay"></param>
    /// <param name="pAction"></param>
    public void Delay(float pDelay, Action pAction)
    {
        StartCoroutine(DelayCoroutine(pDelay, pAction));
    }

    private IEnumerator DelayCoroutine(float pDelay, Action pAction)
    {
        yield return new WaitForSeconds(pDelay);

        pAction?.Invoke();
    }
}
