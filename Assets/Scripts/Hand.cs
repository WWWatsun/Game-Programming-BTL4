using UnityEngine;
using System.Collections.Generic;

/*
 Hand testing and display, complete it for players later
 */
public class Hand : MonoBehaviour
{
    [SerializeField] List<CardScriptables> handList;
    [SerializeField] GameObject cardPrefab;

    [ContextMenu("HandDisplay")]
    void HandDisplay()
    {
        //Clease the previous display
        for (int i = transform.childCount - 1;  i >= 0; i--)
        {
            //Destroy cards display only
            GameObject child = transform.GetChild(i).gameObject;
            CardTest checker = child.GetComponent<CardTest>();
            if (checker != null)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        //Offset initialization
        float handSpacing = 2f;
        int count = handList.Count;
        float offset = -((count - 1) * handSpacing) / 2f;


        foreach (CardScriptables card in handList)
        {
            Vector3 cardPos = transform.position + new Vector3(offset, 0, 0);
            //GameObject newCard = Instantiate(cardPrefab, cardPos, transform.rotation);
            GameObject newCard = Instantiate(cardPrefab, transform);
            newCard.transform.position = cardPos;
            SpriteRenderer renderer = newCard.GetComponent<SpriteRenderer>();
            renderer.sprite = card.cardSprite;
            offset += handSpacing;
        }
    }

    [ContextMenu("Draw A Card")]
    void DrawACard()
    {
        CardScriptables card = Deck.Instance.DrawCard();
        handList.Add(card);
        HandDisplay();
        Debug.Log($"Successfully Draw {card.CardName()}");
    }

    void Discard(CardScriptables card)
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
