using UnityEngine;

public class PlayerController : MonoBehaviour
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
        if (!isMyTurn)
        {
            Debug.Log($"Player {playerIndex}: Not your turn.");
            return;
        }

        UnoGameManager.Instance.TryPlayCard(this, card);
    }

    public void DrawCard()
    {
        hand.DrawACard();
    }
}