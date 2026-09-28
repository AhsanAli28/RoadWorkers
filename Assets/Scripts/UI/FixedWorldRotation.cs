using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedWorldRotation : MonoBehaviour
{
    public Vector3 targetRotation = new Vector3(-45f, 0f, 0f); // Set desired world rotation

    void Start()
    {
        transform.rotation = Quaternion.Euler(targetRotation); // Force world rotation
    }
}
