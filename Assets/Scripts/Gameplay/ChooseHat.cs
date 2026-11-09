using System.Collections.Generic;
using UnityEngine;

public class ChooseHat : MonoBehaviour
{
    [SerializeField] private List<GameObject> parentToPlace;
    [SerializeField] private List<GameObject> selections;

    [HideInInspector] public Hat hatToRecive;

    private int playerHasHat = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.GetComponent<ChooseHat>().enabled)
            return;

        hatToRecive = other.GetComponent<HatGiver>().hatToGive;

        if (playerHasHat == 0)
        {
            Instantiate(hatToRecive.hat, parentToPlace[0].transform.position, Quaternion.identity, parentToPlace[0].transform);
            selections[0].SetActive(false);
            playerHasHat++;
        }

        else
        {
            Instantiate(hatToRecive.hat, parentToPlace[1].transform.position, Quaternion.identity, parentToPlace[1].transform);
            selections[1].SetActive(false);
            gameObject.GetComponent<ChooseHat>().enabled = false;
        }
    }
}
