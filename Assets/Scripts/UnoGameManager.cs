using UnityEngine;
using System.Collections.Generic;

public class UnoGameManager : MonoBehaviour
{
    public static UnoGameManager Instance { get; private set; }

    [Header("Players")]
    [SerializeField] private List<PlayerController> players = new List<PlayerController>();

    [Header("Current Game State")]
    [SerializeField] private CardScriptables topDiscardCard;
    [SerializeField] private CardColor currentColor;

    private TurnManager turnManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        turnManager = new TurnManager(players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].Init(i);
        }

        InitTopDiscardCardIfNeeded();
        UpdateTurnVisuals();
    }

    private void InitTopDiscardCardIfNeeded()
    {
        if (topDiscardCard != null)
        {
            currentColor = topDiscardCard.cardColor;
            Deck.Instance.GetDiscarded(topDiscardCard);
            return;
        }

        topDiscardCard = Deck.Instance.DrawCard();

        while (topDiscardCard.cardColor == CardColor.NEUTRAL)
        {
            Deck.Instance.GetDiscarded(topDiscardCard);
            topDiscardCard = Deck.Instance.DrawCard();
        }

        currentColor = topDiscardCard.cardColor;
        Deck.Instance.GetDiscarded(topDiscardCard);

        Debug.Log($"Initial top discard card: {topDiscardCard.CardName()}");
    }

    public bool IsLegalMove(PlayerController player, CardScriptables card)
    {
        if (player == null || card == null) return false;
        if (turnManager == null) return false;

        if (player.PlayerIndex != turnManager.CurrentPlayerIndex)
        {
            return false;
        }

        return UnoRuleEngine.IsLegalMove(
            playedCard: card,
            topCard: topDiscardCard,
            currentColor: currentColor,
            playerCardCount: player.Hand.Cards.Count
        );
    }

    public void TryPlayCard(PlayerController player, CardScriptables card)
    {
        if (!IsLegalMove(player, card))
        {
            Debug.Log($"Illegal move: {card.CardName()}");
            return;
        }

        player.Hand.RemoveFromHandOnly(card);
        Deck.Instance.GetDiscarded(card);

        topDiscardCard = card;

        if (card.cardColor != CardColor.NEUTRAL)
        {
            currentColor = card.cardColor;
        }

        Debug.Log($"Player {player.PlayerIndex} played {card.CardName()}");

        ApplyBasicCardEffect(card);
        UpdateTurnVisuals();
    }
    private void ApplyBasicCardEffect(CardScriptables card)
    {
        switch (card.cardValue)
        {
            case CardValue.REVERSE:
                turnManager.ReverseDirection();
                turnManager.NextTurn();
                break;

            case CardValue.SKIP:
                turnManager.NextTurn(extraSkip: 1);
                break;

            case CardValue.PLUS2:
                turnManager.NextTurn();
                players[turnManager.CurrentPlayerIndex].DrawCard();
                players[turnManager.CurrentPlayerIndex].DrawCard();
                turnManager.NextTurn();
                break;

            default:
                turnManager.NextTurn();
                break;
        }
    }

    private void UpdateTurnVisuals()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetTurnState(i == turnManager.CurrentPlayerIndex);
        }

        Debug.Log($"Current turn: Player {turnManager.CurrentPlayerIndex}, direction: {turnManager.Direction}");
    }
}