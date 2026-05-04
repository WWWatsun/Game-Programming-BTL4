using UnityEngine;
using System.Collections.Generic;

/*
 Hand testing and display, complete it for players later
 */
public class Hand : MonoBehaviour
{
    [SerializeField] List<CardScriptables> handList;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private bool hideCardsFromOtherPlayers = true;
    public IReadOnlyList<CardScriptables> Cards => handList; //nhi: UnoRuleEngine can biet player con bnhiu la de check rule “không được thắng bằng action card"

    private void Start() //nhi: chinh cach các la bai duoc xep o initial position
    {
        HandDisplay();

        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            RefreshLegalVisuals(player);
        }
    }

    [ContextMenu("HandDisplay")]
    public void HandDisplay() //nhi: tao HandDisplay mới de la bai ko bi tran, UI dep hon
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Card checker = child.GetComponent<Card>();

            if (checker != null)
            {
                Destroy(child);
            }
        }

        float desiredSpacing = 2f;
        float minSpacing = 0.8f;
        float maxHandWidth = 12f;

        int count = handList.Count;

        float handSpacing = desiredSpacing;

        if (count > 1)
        {
            handSpacing = Mathf.Min(desiredSpacing, maxHandWidth / (count - 1));
            handSpacing = Mathf.Max(handSpacing, minSpacing);
        }

        float offset = -((count - 1) * handSpacing) / 2f;

        //nhi: khi tạo card mới, local player thấy bài của mình, và thấy sluong của opponent
        int index = 0;

        foreach (CardScriptables card in handList)
        {
            GameObject newCard = Instantiate(cardPrefab, transform);
            newCard.transform.localPosition = new Vector3(offset, 0, 0);

            Card cardObj = newCard.GetComponent<Card>();
            cardObj.card = card;
            cardObj.overrideSpriteByHand = true;

            SpriteRenderer sr = newCard.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = index;
            }

            ApplyCardVisibility(newCard, card);

            offset += handSpacing;
            index++;
        }
        // int index = 0;

        // foreach (CardScriptables card in handList)
        // {
        //     GameObject newCard = Instantiate(cardPrefab, transform);
        //     newCard.transform.localPosition = new Vector3(offset, 0, 0);

        //     Card cardObj = newCard.GetComponent<Card>();
        //     cardObj.card = card;

        //     SpriteRenderer sr = newCard.GetComponent<SpriteRenderer>();
        //     if (sr != null)
        //     {
        //         sr.sortingOrder = index;
        //     }

        //     offset += handSpacing;
        //     index++;
        // }
    }

    public bool HasAnyLegalCard(PlayerController player)
    {
        if (player == null || UnoGameManager.Instance == null)
        {
            return false;
        }

        foreach (CardScriptables card in Cards)
        {
            if (UnoGameManager.Instance.IsLegalMove(player, card))
            {
                return true;
            }
        }

        return false;
    }

    public void ClearHand()
    {
        handList.Clear();
        HandDisplay();

        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            RefreshLegalVisuals(player);
        }
    }

    public void AddCard(CardScriptables card)
    {
        handList.Add(card);
        HandDisplay();

        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            RefreshLegalVisuals(player);
        }
    }
    // [ContextMenu("HandDisplay")]
    // public void HandDisplay() // nhi sua thanh public cho UnoGameManager hoặc PlayerController goi
    // {
    //     //Clease the previous display
    //     for (int i = transform.childCount - 1;  i >= 0; i--)
    //     {
    //         //Destroy cards display only
    //         GameObject child = transform.GetChild(i).gameObject;
    //         Card checker = child.GetComponent<Card>();
    //         if (checker != null)
    //         {
    //             Destroy(transform.GetChild(i).gameObject);
    //         }
    //     }

    //     //Offset initialization
    //     float handSpacing = 2f;
    //     int count = handList.Count;
    //     float offset = -((count - 1) * handSpacing) / 2f;


    //     foreach (CardScriptables card in handList)
    //     {
    //         Vector3 cardPos = transform.position + new Vector3(offset, 0, 0);
    //         //GameObject newCard = Instantiate(cardPrefab, cardPos, transform.rotation);
    //         GameObject newCard = Instantiate(cardPrefab, transform);
    //         newCard.transform.position = cardPos;
    //         Card cardObj = newCard.GetComponent<Card>();
    //         cardObj.card = card;
    //         //SpriteRenderer renderer = newCard.GetComponent<SpriteRenderer>();
    //         //renderer.sprite = card.cardSprite;
    //         offset += handSpacing;
    //     }
    // }

    [ContextMenu("Draw A Card")]
    public void DrawACard()
    {
        DrawOneCardToHand();
    }

    public CardScriptables DrawOneCardToHand()
    {
        CardScriptables card = Deck.Instance.DrawCard();
        handList.Add(card);

        HandDisplay();

        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            RefreshLegalVisuals(player);
        }

        Debug.Log($"Successfully Draw {card.CardName()}");

        return card; //return CardScriptables: UnoGameManager cần biết lá vừa rút có playable không.
    }

    //nhi : khi la bai legal, UnoGameManager goi player.Hand.RemoveFromHandOnly(card); Deck.Instance.GetDiscarded(card);
    public void RemoveFromHandOnly(CardScriptables card)
    {
        handList.Remove(card);

        HandDisplay();

        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            RefreshLegalVisuals(player);
        }

        Debug.Log($"Remove from hand only {card.CardName()}");
    }
    //nhi: hightlight legal/ illegal card + đảm bảo chỉ local player mới thấy highlight, và chỉ thấy bài của mình, và chỉ thấy số lượng bài của opponent 
    public void RefreshLegalVisuals(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        bool isLocalPlayer =
            UnoGameManager.Instance != null &&
            player.PlayerIndex == UnoGameManager.Instance.LocalPlayerIndex;

        foreach (Transform child in transform)
        {
            Card cardObj = child.GetComponent<Card>();
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

            if (cardObj == null || sr == null) continue;

            if (!isLocalPlayer)
            {
                sr.color = Color.white;
                continue;
            }

            if (!player.IsMyTurn)
            {
                sr.color = Color.white;
                continue;
            }

            bool isLegal =
                UnoGameManager.Instance != null &&
                UnoGameManager.Instance.IsLegalMove(player, cardObj.card);

            sr.color = isLegal
                ? Color.white
                : new Color(0.35f, 0.35f, 0.35f, 0.6f);
        }
    }
    private void ApplyCardVisibility(GameObject cardObject, CardScriptables card)
    {
        SpriteRenderer sr = cardObject.GetComponent<SpriteRenderer>();

        if (sr == null || card == null)
        {
            return;
        }

        PlayerController owner = GetComponentInParent<PlayerController>();

        bool shouldHide =
            hideCardsFromOtherPlayers &&
            UnoGameManager.Instance != null &&
            owner != null &&
            owner.PlayerIndex != UnoGameManager.Instance.LocalPlayerIndex;

        if (shouldHide && cardBackSprite != null)
        {
            sr.sprite = cardBackSprite;

            CardClicking clicking = cardObject.GetComponent<CardClicking>();
            if (clicking != null)
            {
                clicking.enabled = false;
            }
        }
        else
        {
            sr.sprite = card.cardSprite;

            CardClicking clicking = cardObject.GetComponent<CardClicking>();
            if (clicking != null)
            {
                clicking.enabled = true;
            }
        }
    }

    public void Discard(CardScriptables card)
    {
        handList.Remove(card);
        Deck.Instance.GetDiscarded(card);
        HandDisplay();
        Debug.Log($"Discard {card.CardName()}");
    }

    [Header("Debug")] public CardScriptables debugcard;
    [ContextMenu("Discard")]
    void Removal()
    {
        Discard(debugcard);
    }
}
