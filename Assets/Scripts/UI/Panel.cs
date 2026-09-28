using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel : MonoBehaviour
{
    public Action OnPanelClosed;
    [SerializeField] protected GameObject panelObj;

    [Tooltip("Optional")] public Animator animator;
    [Tooltip("Optional")] public CanvasGroup canvasGroup;
    [Tooltip("If it should be animated for fade in and out")] public bool AnimateFade = true;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ClosePanel()
    {
        OnPanelClosed?.Invoke();

        //Fade out animation
        if (animator != null && AnimateFade)
            animator.Play("Panel_Fade_Out");
        //Just disable it 
        else
            panelObj.SetActive(false);
    }

    public void DisableObject()
    {
        panelObj.SetActive(false);
    }

    public void EnableObject()
    {
        panelObj.SetActive(true);
    }

    public GameObject GetPanelObject()
    {
        return panelObj;
    }
}
