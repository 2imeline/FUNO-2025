using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using FishNet.Object;
using FishNet;

public class DeckManager : NetworkBehaviour
{
// Card & Deck Setup
public GameObject cardPrefab;
public List<Color> suitColorList = new();
public int numCards;
public int suits;
public int cardsPerPlayer = 7;

// Deck State & Card Tracking
public List<Transform> cards = new();
public Transform deck;
public Vector3 deckLocation;
public Transform discardPile;
public Card lastPlayedCard;
public int numPlayed = 1;

// Spacing & Layout
public float heightIncrease = 0.00103572f;
public float xIncrease = 0.11f;
public float cardHandSpacing = 0.1537f;

// Internal Position Trackers
float lastHeight = 0;
float lastX = 0;
    public static DeckManager Instance { get; private set; }

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
        lastX = -xIncrease;
        lastHeight = -heightIncrease;
        deck.position = deckLocation;
    }

    private void Update()
    {
        fixPosLastPlayed();
    }

    [ServerRpc(RequireOwnership = false)]
    public void requestDiscard(NetworkObject card)
    {
        helper.Instance.logToEveryone("I AM BEING REQUESTED");
        putToDiscard(card);
    }

    [ObserversRpc]
    public void putToDiscard(NetworkObject card)
    {
        parentCard(card, discardPile.GetComponent<NetworkObject>());
        helper.Instance.moveObject(card, new Vector3(0, numPlayed*heightIncrease, 0));
        helper.Instance.setRotation(card, Quaternion.Euler(90, 0, 0));
        lastPlayedCard = card.GetComponent<Card>();
        card.transform.tag = "played";
        numPlayed++;
    }

    public void fixPosLastPlayed()
    {
        if(lastPlayedCard == null)
            return;
        lastPlayedCard.DOKill();
        lastPlayedCard.transform.localPosition = new Vector3(0, numPlayed*heightIncrease, 0);
        lastPlayedCard.transform.localRotation = Quaternion.Euler(90, 0, 0);
    }


    [Server]
    public IEnumerator generateDeck()
    {
        for (int s = 0; s < suits; s++)
        {
            for (int n = 0; n < 10; n++)
            {
                spawnCard(n, s);
                spawnCard(n, s);
            }
            spawnCard(10, s);
            spawnCard(11, s);
            spawnCard(12, s);
            
            spawnCard(10, s);
            spawnCard(11, s);
            spawnCard(12, s);
        }
        for (int i = 0; i < 4; i++)
        {
            spawnCard(13, 0);
            spawnCard(14, 0);
            
        }
        yield return new WaitForSeconds(1);
        shuffle();
    }

    [Server]
    public void spawnCard(int num, int suit)
    {
        Card newCard = Instantiate(cardPrefab).GetComponent<Card>();
        Spawn(newCard.gameObject);
        switch (num)
        {
            case 10:
            case 11:
            case 12:
                generateSpecial(num, suit, newCard);
                break;
            case 13:
            case 14:
                generateWild(num, newCard);
                break;
            default:
                generateCard(num, suit, newCard);
                break;
        }
    }
    [ObserversRpc]
    public void generateCard(int num, int suit, Card newCard)
    {
        newCard.transform.parent = deck;
        newCard.transform.position = Vector3.zero;
        newCard.transform.rotation = Quaternion.Euler(90, 0, 0);
        newCard.myNumber = num;
        newCard.GetComponentInChildren<MeshRenderer>().material.color = suitColorList[suit];
        newCard.myColor = suitColorList[suit];
        newCard.transform.localPosition = new Vector3(lastX+xIncrease, lastHeight+heightIncrease, 0);
        lastHeight += heightIncrease;
        lastX += xIncrease;
        newCard.cardText.text = $"{num}";
        newCard.name = $"{num}";
        cards.Add(newCard.transform);
    }
    [ObserversRpc]
    public void generateSpecial(int num, int suit, Card newCard)
    {
        newCard.transform.parent = deck;
        newCard.transform.position = Vector3.zero;
        newCard.transform.rotation = Quaternion.Euler(90, 0, 0);
        newCard.myNumber = num;
        newCard.GetComponentInChildren<MeshRenderer>().material.color = suitColorList[suit];
        newCard.myColor = suitColorList[suit];
        newCard.transform.localPosition = new Vector3(lastX+xIncrease, lastHeight+heightIncrease, 0);
        lastHeight += heightIncrease;
        lastX += xIncrease;
        string cardText = "temp";
        switch (num)
        {
            //skip
            case 10:
                cardText = "S";
                break;
            //reverse
            case 11:
                cardText = "R";
                break;
            //+2
            case 12:
                cardText = "+";
                break;
        }
        newCard.cardText.text = cardText;
        newCard.name = cardText;
        newCard.special = true;
        cards.Add(newCard.transform);
    }
    [ObserversRpc]
    public void generateWild(int num, Card newCard)
    {
        newCard.transform.parent = deck;
        newCard.transform.position = Vector3.zero;
        newCard.transform.rotation = Quaternion.Euler(90, 0, 0);
        newCard.myNumber = num;
        newCard.GetComponentInChildren<MeshRenderer>().material.color = Color.black;
        newCard.myColor = Color.black;
        newCard.transform.localPosition = new Vector3(lastX+xIncrease, lastHeight+heightIncrease, 0);
        lastHeight += heightIncrease;
        lastX += xIncrease;
        string cardText = "temp";
        switch (num)
        {
            //Wild
            case 13:
                cardText = "W";
                break;
            //+4
            case 14:
                cardText = "+";
                break;
        }
        newCard.cardText.text = cardText;
        newCard.cardText.color = Color.white;
        newCard.name = cardText;
        newCard.wild = true;
        cards.Add(newCard.transform);
    }
    


    [Server]
    public void shuffle()
    {

        // Fisher–Yates Shuffle
        for (int i = 0; i < cards.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, cards.Count);
            (cards[i], cards[rand]) = (cards[rand], cards[i]);
        }

        List<int> orderedCardIDs = new();

        for (int i = 0; i < cards.Count; i++)
        {
            Transform currentCard = cards[i];
            currentCard.localPosition = new Vector3(xIncrease*i, heightIncrease*i, 0);
            orderedCardIDs.Add(currentCard.GetComponent<NetworkObject>().ObjectId);
        }
        cards.Reverse();
        orderedCardIDs.Reverse();
        syncCardPositions(orderedCardIDs, orderedCardIDs.Count, cards[0].GetComponent<Card>().myNumber);
    }

    [ObserversRpc]
    public void syncCardPositions(List<int> idList, int truCount, int zInum)
    {
        List<Transform> tempCards = new List<Transform>(cards);
        cards.Clear();

        for (int i = 0; i < idList.Count; i++)
        {
            for (int k = 0; k < tempCards.Count; k++)
            {
                Transform current = tempCards[k];
                if(current.GetComponent<NetworkObject>().ObjectId == idList[i])
                {
                    tempCards.Remove(current);
                    cards.Add(current);
                }
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Transform currentCard = cards[i];
            currentCard.localPosition = new Vector3(xIncrease*i, heightIncrease*i, 0);
        }
        GameManager.Instance.notifyServerReadyForDeal();
    }


    [Server]
    public IEnumerator handOutCards(List<Player> players)
    {
        yield return new WaitForSeconds(1);
        foreach (Player player in players)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                NetworkObject cardNOB = cards[0].GetComponent<NetworkObject>();
                int stepsFromCenter = (i+1)/2;
                parentCard(cardNOB, player.cardHolder.GetComponent<NetworkObject>());
                if(i % 2 != 0) // right side
                {
                    helper.Instance.moveObject(cardNOB, new Vector3(cardHandSpacing*stepsFromCenter, 0, -0.001114903f*i));
                }else{ // left side
                    helper.Instance.moveObject(cardNOB, new Vector3(-cardHandSpacing*stepsFromCenter, 0, -0.001114903f*i));
                }
                if(i == 0)
                    helper.Instance.moveObject(cardNOB, Vector3.zero);
                helper.Instance.setRotation(cardNOB, quaternion.identity);
                doHandStuff(player, 1, cardNOB);
                cards.RemoveAt(0);
            }
            doHandStuff(player, 2);
        }
        discardLogic();
    }

    [ObserversRpc]
    public void doHandStuff(Player player, int stuffDoing, NetworkObject cardToGive = null)
    {
        switch (stuffDoing)
        {
            case 1:
                player.hand.Add(cardToGive.transform);
                cards.RemoveAt(0);
                break;
            case 2:
                player.hand = player.hand.OrderBy(card => card.localPosition.x).ToList();
                break;
        }
    }
    [ObserversRpc]
    public void parentCard(NetworkObject card, NetworkObject parent)
    {
        card.transform.parent = parent.transform;
    }

    [Server]
    public void discardLogic()
    {
        helper.Instance.logToEveryone("i am being run on server");
        Card firstCard = cards[UnityEngine.Random.Range(0, cards.Count)].GetComponent<Card>();
        putToDiscard(firstCard.GetComponent<NetworkObject>());
        TurnManager.Instance.calculateTurn();
    }




    [Server]
    public void drawCard(NetworkObject playerNOB)
    {
        Player player = playerNOB.GetComponent<Player>();
        //tru index of card 
        float tI = 0;
        if(player.hand.Count % 2 == 0)
            tI+=0.5f;
        foreach (Transform card in player.hand)
        {
            if(card.localPosition.x >= 0)
            {
                Debug.Log($"positive card found: {card.GetComponent<Card>().myNumber}");
                tI++;
            }
        }
        Debug.Log($"tI = {tI}");
        NetworkObject cardDrawn = cards[0].GetComponent<NetworkObject>();
        parentCard(cardDrawn, player.cardHolder.GetComponent<NetworkObject>());
        Vector3 moveLocation = new Vector3(tI*cardHandSpacing, 0, -0.001114903f*player.hand.Count);
        Debug.Log($"intended X for new card: {tI*cardHandSpacing}");
        helper.Instance.moveObject(cardDrawn, moveLocation);
        helper.Instance.setRotation(cardDrawn, Quaternion.identity);
        drawClientStuff(playerNOB, cardDrawn);
        player.runDrawOperations();

    }

    [ObserversRpc]
    public void drawClientStuff(NetworkObject playerNOB, NetworkObject cardDrawn)
    {
        Player player = playerNOB.GetComponent<Player>();
        cards.RemoveAt(0);
        player.hand.Add(cardDrawn.transform);

    }


}
