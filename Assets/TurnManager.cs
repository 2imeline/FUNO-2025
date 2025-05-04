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


    [ObserversRpc]
    public void moveTurn()
    {
        List<Player> _players = PlayerManager.Instance.players;
        turnIndex++;
        if(turnIndex > (_players.Count-1))
            turnIndex = 0;
        foreach (Player player in _players)
        {
            player.myTurn = false;
            if(player.myTurnIndicator != null)
                player.myTurnIndicator.SetActive(false);
        }
        _players[turnIndex].myTurnIndicator.SetActive(true);
        _players[turnIndex].myTurn = true;
        currentGuysName = _players[turnIndex].nameCanvas.text;

    }
}
