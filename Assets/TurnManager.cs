using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public int turnIndex = -1;
    public string currentGuysName;
    public GameManager gameManager;
    public static TurnManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // optional: protect singleton
            return;
        }
        Instance = this;
    }
    

    [Server]
    public void calculateTurn(int timesToMove = 1, bool reverse = false)
    {
        if(reverse)
        {
            int newIndex = (PlayerManager.Instance.players.Count-1) - turnIndex;
            PlayerManager.Instance.players.Reverse();
            turnIndex = newIndex;
        }
        for (int i = 0; i < timesToMove; i++)
        {
            turnIndex++;
            if(turnIndex > (PlayerManager.Instance.players.Count-1))
                turnIndex = 0;
        }
        setTurnIndex(turnIndex, PlayerManager.Instance.players[turnIndex].GetComponent<NetworkObject>());
    }

    [ObserversRpc]
    public void setTurnIndex(int newIndex, NetworkObject selectedPlayer)
    {
        foreach (Player player in PlayerManager.Instance.players)
        {
            player.myTurn = false;
            if(player.myTurnIndicator != null)
                player.myTurnIndicator.SetActive(false);
        }
        Player _selectedPlayer = selectedPlayer.GetComponent<Player>();
        _selectedPlayer.myTurnIndicator.SetActive(true);
        _selectedPlayer.myTurn = true;
        currentGuysName = _selectedPlayer.nameCanvas.text;
        turnIndex = newIndex;
        Debug.Log($"turn index is {turnIndex}");

    }
}
