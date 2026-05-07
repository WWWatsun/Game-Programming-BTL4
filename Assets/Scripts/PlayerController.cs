using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private Hand hand;

    private bool isMyTurn;

    public int PlayerIndex => playerIndex;
    public Hand Hand => hand;
    public bool IsMyTurn => isMyTurn;

    private void Awake()
    {
        if (hand == null)
        {
            hand = GetComponentInChildren<Hand>();
        }
    }

    public void Init(int index)
    {
        playerIndex = index;
    }

    public void SetTurnState(bool value)
    {
        isMyTurn = value;

        if (hand != null)
        {
            hand.RefreshLegalVisuals(this);
        }

        Debug.Log($"Player {playerIndex} turn state: {isMyTurn}");
    }

    public void RequestPlayCard(CardScriptables card)
    {
        // 1. Ensure only the local player clicking their own UI can trigger this
        if (!IsOwner) return;

        if (!IsMyTurn)
        {
            Debug.Log("Not your turn.");
            return;
        }

        // 2. Instead of playing it locally, send the ID to the Host!
        RequestPlayCardServerRpc(card.cardID);
    }

    // 3. This code runs ONLY on the Host's machine
    [ServerRpc]
    private void RequestPlayCardServerRpc(int cardIdRequested)
    {
        // Host looks up the card using the ID
        CardScriptables cardToPlay = Deck.Instance.CardDatabase[cardIdRequested];

        // Host runs your existing local logic!
        UnoGameManager.Instance.TryPlayCard(this, cardToPlay);
    }

    public CardScriptables DrawCard()// nhi chinh cach nut Draw hoat dong
    {
        return hand.DrawOneCardToHand();
    }

    public void RequestDrawCard()// nhi chinh cach nut Draw hoat dong
    {
        if (!isMyTurn)
        {
            Debug.Log($"Player {playerIndex}: Not your turn, cannot draw.");
            return;
        }

        UnoGameManager.Instance.TryDrawCard(this);
    }

    // The Host calls this, but it ONLY executes on the targeted client's machine
    [ClientRpc]
    public void ReceiveCardClientRpc(int cardId, ClientRpcParams rpcParams = default)
    {
        // 1. Look up the card from the ID
        CardScriptables drawnCard = Deck.Instance.CardDatabase[cardId];

        // 2. Add it to this local client's hand UI
        Hand.AddCard(drawnCard);

        Debug.Log($"I received card: {drawnCard.CardName()}");
    }

    [ClientRpc]
    public void PromptColorSelectionClientRpc(ClientRpcParams rpcParams = default)
    {
        // This only runs on the specific client who played the Wild card
        Debug.Log("I need to choose a color!");

        // Tell the local UI to show the color picker panel
        UnoGameManager.Instance.OpenColorPickerUI();
    }

    // Call this from your Color UI Buttons!
    public void SubmitColorChoice(int colorIndex)
    {
        if (!IsOwner) return;
        SubmitColorChoiceServerRpc(colorIndex);
    }

    [ServerRpc]
    private void SubmitColorChoiceServerRpc(int colorIndex)
    {
        // The client just told the Host what color they picked.
        // Now the Host actually applies the rule.
        UnoGameManager.Instance.ApplyColorChoiceOnServer(colorIndex);
    }

    [ClientRpc]
    public void RemoveCardClientRpc(int cardId, ClientRpcParams rpcParams = default)
    {
        // 1. Look up the card by ID
        CardScriptables cardToRemove = Deck.Instance.CardDatabase[cardId];

        // 2. Remove it from the local UI
        Hand.RemoveFromHandOnly(cardToRemove);

        Debug.Log($"Removed {cardToRemove.CardName()} from my screen!");
    }
}