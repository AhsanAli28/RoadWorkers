using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField] UnityEvent OnClick;

    public void Interact()
    {
        OnClick?.Invoke();
    }
}
