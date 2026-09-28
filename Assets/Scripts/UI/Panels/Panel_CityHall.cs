using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel_CityHall : Panel
{
    [Header("Contracts")]
    [SerializeField] GameObject contractListingPrefab;
    [SerializeField] RectTransform contractsContent;

    public void AddContractToList(Contract pContract)
    {
        GameObject listingObj = Instantiate(contractListingPrefab, contractsContent);
        ContractListing listing = listingObj.GetComponent<ContractListing>();
        listing.SetContract(this, pContract);
    }
}
