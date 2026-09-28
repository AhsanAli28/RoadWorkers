using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingSpot : MonoBehaviour
{
    [SerializeField] Transform parkingPoistion_small;
    [SerializeField] Transform parkingPoistion_large;
    [SerializeField] GameObject road_small;
    [SerializeField] GameObject road_large;
    [SerializeField] GameObject grass;
    [SerializeField] GameObject icon_small;
    [SerializeField] GameObject icon_large;
    [SerializeField] SpecialEffect specialEffect;
    public Transport assignedTransport;
    public bool isParked;
    public bool isLarge;

    public void ToggleParkingSpot(bool value)
    {
        if (!isLarge)
        {
            road_small.SetActive(value);
            icon_small.SetActive(value);
        }
        else
        {
            road_large.SetActive(value);
            icon_large.SetActive(value);
        }
        grass.SetActive(!value);
    }

    public Transform GetParkingPosition()
    {
        if (!isLarge)
            return parkingPoistion_small;
        else
            return parkingPoistion_large;
    }

    public void PlaySpecialEffect()
    {
        if (specialEffect != null)
            specialEffect.PlayEffectNoDestroy();
    }
}
