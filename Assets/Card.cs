using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using FishNet.Object;

public class Card : NetworkBehaviour
{
    public TextMeshProUGUI cardText;
    public int myNumber;
    public Color myColor;
    public Outline myOutline;
    public bool special = false;
    public bool wild = false;

    [ObserversRpc]
    public void changeMyColor(int suit)
    {
        GetComponentInChildren<MeshRenderer>().material.color = DeckManager.Instance.suitColorList[suit];
        myColor = DeckManager.Instance.suitColorList[suit];
    }


    
}
