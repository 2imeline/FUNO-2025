using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.Rendering.Universal;


public class Player : NetworkBehaviour
{


#region References
public TextMeshProUGUI nameCanvas;
public Transform cardHolder;
public GameObject myTurnIndicator;
LookAround cameraLogic;
#endregion

#region Player Identity & State
public readonly SyncVar<string> syncedUsername = new();

public bool myTurn = false;
public bool cheats = false;
public bool cardsLevitating = false;
#endregion

#region Card Interaction
public List<Transform> hand = new();
public Transform lastLookedCard;
float cardHandSpacing;

bool imReady = false;
#endregion



    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        syncedUsername.OnChange += (prev, current, asServer) =>
        {
            nameCanvas.text = current;
        };
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        Application.targetFrameRate = 60;

        myTurnIndicator.SetActive(false);
        cameraLogic = GetComponent<LookAround>();
        lastLookedCard = null;
        cardHandSpacing = DeckManager.Instance.cardHandSpacing;
        PlayerManager.Instance.players.Add(this);
        if (!base.IsOwner)
        {
            cameraLogic.cameraTransform.gameObject.SetActive(false);
            cameraLogic.enabled = false;
            this.enabled = false;
            Destroy(myTurnIndicator);
        }
        
            string name = GameObject.Find("UsernameManager").GetComponent<UsernameManager>().username;
            SetUsername(name);
            nameCanvas.text = syncedUsername.Value;
        
    }

    [ServerRpc]
    void SetUsername(string name)
    {
        syncedUsername.Value = name;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R) && !imReady)
            GameManager.Instance.notifyReadiness();

        if(Input.GetKeyDown(KeyCode.C))
            levitationController();

        if(Input.GetKeyDown(KeyCode.P) && myTurn)
            requestDrawCard();


        RaycastHit hit;
        if(Physics.Raycast(cameraLogic.cameraTransform.position, cameraLogic.cameraTransform.forward, out hit, 5))
        {
            if(hit.transform.CompareTag("Card") && cardsLevitating)
            {
                if(hit.transform != lastLookedCard && lastLookedCard != null)
                {
                    requestKillTween(lastLookedCard.transform.GetComponent<NetworkObject>());
                    requestTweenY(lastLookedCard.transform.GetComponent<NetworkObject>(), 0, 0.15f);
                }


                lastLookedCard = hit.transform;
                if(!DOTween.IsTweening(lastLookedCard))
                    requestTweenY(hit.transform.GetComponent<NetworkObject>(), 0.075f, 0.15f);

                //play
                if(myTurn && Input.GetMouseButtonDown(0))
                {
                    attemptPlay(hit.transform.GetComponent<NetworkObject>());
                }
            }else{
                if(lastLookedCard != null && !DOTween.IsTweening(lastLookedCard))
                    requestTweenY(lastLookedCard.transform.GetComponent<NetworkObject>(), 0, 0.15f);
            }
        }

    }

    [ServerRpc]
    public void requestTweenY(NetworkObject nob, float pos, float time)
    {
        performTweenY(nob, pos, time);
    }
    [ObserversRpc]
    public void performTweenY(NetworkObject nob, float pos, float time)
    {
        nob.transform.DOLocalMoveY(pos, time);
    }
    [ServerRpc]
    public void requestKillTween(NetworkObject nob)
    {
        performKillTween(nob);
    }
    [ObserversRpc]
    public void performKillTween(NetworkObject nob)
    {
        nob.transform.DOKill();
    }

    [ServerRpc]
    public void requestDrawCard()
    {
        DeckManager.Instance.drawCard(GetComponent<NetworkObject>());
        
    }

    [ObserversRpc]
    public void runDrawOperations()
    {
        centerCards(hand.Last().GetComponent<NetworkObject>(), false);
        if(!canIPlay(hand.Last().GetComponent<Card>()))
            TurnManager.Instance.calculateTurn();
    }


    [ObserversRpc]
    public void centerCards(NetworkObject cardToPlay, bool playing = true)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            int indexPlayed = hand.IndexOf(cardToPlay.transform);
            float deltaX = 0;
            //do i move left first?
            if(i >= indexPlayed && playing)
            {
                deltaX -= cardHandSpacing;
            }

            if(playing)
                deltaX += (cardHandSpacing/2);
            else
                deltaX -= (cardHandSpacing/2);

            float targetX = deltaX + hand[i].localPosition.x;

            hand[i].DOLocalMoveX(targetX, 0.15f);
        
        }
    }

    //NOTE: remove ability to play any card if last is wild
    //remove when change colors is added
    public bool canIPlay(Card cardToPlay)
    {
        if((cardToPlay.myNumber == DeckManager.Instance.lastPlayedCard.myNumber || cardToPlay.myColor == DeckManager.Instance.lastPlayedCard.myColor || cardToPlay.wild || DeckManager.Instance.lastPlayedCard.wild))
            return true;
        return false;
    }

    [ServerRpc]
    public void attemptPlay(NetworkObject cardToPlay)
    {
        Card card = cardToPlay.GetComponent<Card>();
        Card lastPlayed = DeckManager.Instance.lastPlayedCard;
        if(canIPlay(card))
        {
            centerCards(cardToPlay);
            playCard(cardToPlay);
            switch (card.myNumber)
            {
                case 10:
                    helper.Instance.logToEveryone("Skip played");
                    TurnManager.Instance.calculateTurn(2);
                    break;
                case 11:
                    helper.Instance.logToEveryone("Reverse played");
                    TurnManager.Instance.calculateTurn(1, true);
                    break;
                default:
                    TurnManager.Instance.calculateTurn();
                    break;
            }
        }

    }


    [ObserversRpc]
    void playCard(NetworkObject cardToPlay)
    {
        Card card = cardToPlay.GetComponent<Card>();
        Debug.Log($"Last played {card.myNumber}");
        DeckManager.Instance.requestDiscard(cardToPlay);
        hand.Remove(cardToPlay.transform);

    }
    

    void levitationController()
    {
        if(cardsLevitating)
        {
            helper.Instance.requestSetParent(cardHolder.GetComponent<NetworkObject>(), this.NetworkObject);
            if(lastLookedCard != null)
                requestTweenY(lastLookedCard.transform.GetComponent<NetworkObject>(), 0, 0.15f);
        }
        else
            helper.Instance.requestSetParent(cardHolder.GetComponent<NetworkObject>());
        cardsLevitating = !cardsLevitating;
    }
}
