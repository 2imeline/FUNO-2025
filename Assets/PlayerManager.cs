using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public List<Player> players = new();

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // optional: protect singleton
            return;
        }
        Instance = this;
    }

    [ServerRpc]
    public void recievePlayer(Player player)
    {
        updateList(player);
    }

    [ObserversRpc]
    public void updateList(Player player)
    {
        players.Add(player);
    }
}
