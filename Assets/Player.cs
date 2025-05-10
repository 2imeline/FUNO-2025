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
public GameObject colorSelector;
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
        if(colorSelector.activeInHierarchy)
        {
            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                wildLogic(1);
                colorSelector.SetActive(false);
            }
            if(Input.GetKeyDown(KeyCode.Alpha2))
            {
                wildLogic(2);
                colorSelector.SetActive(false);
            }
            if(Input.GetKeyDown(KeyCode.Alpha3))
            {
                wildLogic(3);
                colorSelector.SetActive(false);
            }
            if(Input.GetKeyDown(KeyCode.Alpha4))
            {
                wildLogic(4);
                colorSelector.SetActive(false);
            }
        }

        if(Input.GetKeyDown(KeyCode.R) && !imReady)
            GameManager.Instance.notifyReadiness();

        if(Input.GetKeyDown(KeyCode.C))
            levitationController();

        if(Input.GetKeyDown(KeyCode.P) && myTurn)
            requestDrawCard();


        RaycastHit hit;
        if(Physics.Raycast(cameraLogic.cameraTransform.position, cameraLogic.cameraTransform.forward, out hit, 5))
        {
            if(hit.transform.IsChildOf(cardHolder) && cardsLevitating)
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
        if((cardToPlay.myNumber == DeckManager.Instance.lastPlayedCard.myNumber || cardToPlay.myColor == DeckManager.Instance.lastPlayedCard.myColor || cardToPlay.wild))
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
                case 12:
                    helper.Instance.logToEveryone("+2 played");
                    StartCoroutine(runPlusOperations(TurnManager.Instance.turnIndex));
                    TurnManager.Instance.calculateTurn(1);
                    break;
                case 13:
                    helper.Instance.logToEveryone("WILD played");
                    colorSelector.SetActive(true);
                    break;
                case 14:
                    helper.Instance.logToEveryone("WILD +4 played");
                    StartCoroutine(runPlusOperations(TurnManager.Instance.turnIndex, 4));
                    colorSelector.SetActive(true);
                    break;

                default:
                    TurnManager.Instance.calculateTurn();
                    break;
            }
        }

    }

    [ServerRpc]
    public void wildLogic(int colorSelected)
    {
        int trueColorIndex = colorSelected-1;
        DeckManager.Instance.lastPlayedCard.changeMyColor(trueColorIndex);
        TurnManager.Instance.calculateTurn();
    }

    [Server]
    public IEnumerator runPlusOperations(int curTi, int hM = 2)
    {
        yield return new WaitForSeconds(0.2f);
        helper.Instance.logToEveryone("hi");
        int tI = curTi;
        tI++;
            if(tI > (PlayerManager.Instance.players.Count-1))
                tI = 0;
        for (int i = 0; i < hM; i++)
        {
            DeckManager.Instance.drawCard(PlayerManager.Instance.players[tI].GetComponent<NetworkObject>());
            yield return new WaitForSeconds(0.2f);
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
