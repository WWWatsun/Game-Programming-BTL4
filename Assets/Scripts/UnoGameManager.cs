using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UnoGameManager : MonoBehaviour
{
    public static UnoGameManager Instance { get; private set; }

    [Header("Players")]
    [SerializeField] private List<PlayerController> players = new List<PlayerController>();

    [Header("Current Game State")]
    [SerializeField] private CardScriptables topDiscardCard;
    [SerializeField] private CardColor currentColor;

    [Header("Initial Deal")]
    [SerializeField] private bool dealCardsOnStart = true;
    [SerializeField] private int initialCardCount = 7;

    [Header("Local View Debug")] //nhi: gia lap may hien tai la Player0, sau nay sua lại de lam feature multiplayer
    [SerializeField] private int localPlayerIndex = 0;

    public int LocalPlayerIndex => localPlayerIndex;

    private TurnManager turnManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AutoAssignPlayersIfNeeded();

        if (players.Count == 0)
        {
            Debug.LogError("No players found. Please add PlayerController objects to the scene.");
            return;
        }

        turnManager = new TurnManager(players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].Init(i);
        }

        if (dealCardsOnStart)
        {
            DealInitialCards();
        }

        InitTopDiscardCardIfNeeded();
        UpdateTurnVisuals();
    }

    private void AutoAssignPlayersIfNeeded()
    {
        // Xóa các slot bị None trước
        players = players.Where(player => player != null).ToList();

        // Nếu list vẫn có player hợp lệ rồi thì không cần tự tìm nữa
        if (players.Count > 0)
        {
            return;
        }

        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        players = foundPlayers
            .OrderBy(player => player.transform.GetSiblingIndex())
            .ToList();

        Debug.Log($"Auto assigned {players.Count} players.");
    }

    private void DealInitialCards()
    {
        foreach (PlayerController player in players)
        {
            if (player == null || player.Hand == null)
            {
                Debug.LogWarning("Skip dealing card because player or hand is null.");
                continue;
            }

            player.Hand.ClearHand();
        }

        for (int cardIndex = 0; cardIndex < initialCardCount; cardIndex++)
        {
            foreach (PlayerController player in players)
            {
                if (player == null || player.Hand == null)
                {
                    continue;
                }

                CardScriptables card = Deck.Instance.DrawCard();
                player.Hand.AddCard(card);
            }
        }

        Debug.Log($"Dealt {initialCardCount} cards to each player.");
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
    //nhi: chinh cach nut Draw hoat dong
    /*Người chơi chỉ được Draw khi:
        1. Đang tới lượt mình
        2. Không có lá bài legal nào trong hand

        Sau khi Draw:
        - Nếu lá vừa rút playable → vẫn giữ lượt, người chơi có thể đánh lá đó
        - Nếu lá vừa rút không playable → kết thúc lượt, chuyển sang player tiếp theo
    */
    public void RequestCurrentPlayerDraw()
    {
        if (turnManager == null || players == null || players.Count == 0)
        {
            Debug.LogWarning("Cannot draw because TurnManager or players list is not ready.");
            return;
        }

        PlayerController currentPlayer = players[turnManager.CurrentPlayerIndex];

        TryDrawCard(currentPlayer);
    }

    public void TryDrawCard(PlayerController player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot draw because player is null.");
            return;
        }

        if (turnManager == null)
        {
            Debug.LogWarning("Cannot draw because TurnManager is not ready.");
            return;
        }

        if (player.PlayerIndex != turnManager.CurrentPlayerIndex)
        {
            Debug.Log($"Player {player.PlayerIndex}: Not your turn, cannot draw.");
            return;
        }

        if (player.Hand.HasAnyLegalCard(player))
        {
            Debug.Log($"Player {player.PlayerIndex}: You still have legal cards, cannot draw.");
            return;
        }

        CardScriptables drawnCard = player.DrawCard();

        bool drawnCardPlayable = IsLegalMove(player, drawnCard);

        if (drawnCardPlayable)
        {
            Debug.Log($"Player {player.PlayerIndex} drew a playable card: {drawnCard.CardName()}. Player can still play.");

            UpdateTurnVisuals();
            return;
        }

        Debug.Log($"Player {player.PlayerIndex} drew an unplayable card. Turn ends.");

        turnManager.NextTurn();
        UpdateTurnVisuals();
    }
}