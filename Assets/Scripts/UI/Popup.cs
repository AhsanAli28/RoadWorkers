using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Popup : MonoBehaviour
{

    [Tooltip("Optional")] public Animator animator;

    public bool isOpen => gameObject.activeSelf;
    public bool isAnimating => IsAnimatorPlaying();

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void DisableObject()
    {
        gameObject.SetActive(false);
    }

    public void EnableObject()
    {
        gameObject.SetActive(true);
    }

    public void Pop_In()
    {
        EnableObject();
        if (animator)
            animator.Play("Popup_In");
        //So this can be disabled later
        UIManager.Instance.SetCurrentPopup(this);
    }
    public void Pop_Out()
    {
        if (!gameObject.activeInHierarchy) return;

        if (animator)
            animator.Play("Popup_Out");
    }

    bool IsAnimatorPlaying()
    {
        if (animator == null || !animator.enabled || !animator.gameObject.activeInHierarchy)
            return false;

        if (!animator.runtimeAnimatorController)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Animator is in a valid playing state that is not transitioning and not idle
        return stateInfo.normalizedTime < 1f && !animator.IsInTransition(0);
    }
}
