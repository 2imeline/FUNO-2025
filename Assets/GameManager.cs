using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public DeckManager deckManager;
    public PlayerManager playerManager;
    public TurnManager turnManager;
    public int numReady;
    public bool gameStarted = false;
    public TextMeshProUGUI readyTxt;

    public static GameManager Instance { get; private set; }

    public int readyForDeal = 0;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // optional: protect singleton
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        deckManager = DeckManager.Instance;
        
    }
    void StartGame()
    {
        gameStarted = true;
        StartCoroutine(deckManager.generateDeck());

        //Card firstCard = deckManager.cards[Random.Range(0, deckManager.cards.Count)].GetComponent<Card>();
        //deckManager.putToDiscard(firstCard.transform);
        //deckManager.lastPlayedCard = firstCard;

        //turnManager.moveTurn();

    }

    [ServerRpc(RequireOwnership = false)]
    public void notifyServerReadyForDeal()
    {
        readyForDeal++;
        if(readyForDeal >= PlayerManager.Instance.players.Count)
        {
            StartCoroutine(deckManager.handOutCards(PlayerManager.Instance.players));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartDeck()
    {
        deckManager.generateDeck();
    }

    [ServerRpc(RequireOwnership = false)]
    public void notifyReadiness()
    {

        updateReadyText();
    }

    [ObserversRpc]
    public void updateReadyText()
    {
        numReady++;
        readyTxt.text = $"{numReady}/{playerManager.players.Count} Ready";
        if(numReady >= playerManager.players.Count)
        {
            readyTxt.enabled = false;
            StartGame();
        }
    }

    private void Update()
    {

        if(Input.GetKeyDown(KeyCode.L))
        {
            deckManager.handOutCards(playerManager.players);
        }
    }
}
