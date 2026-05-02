using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    public static Deck Instance { get; private set; }
    [SerializeField] CardScriptables[] cardDeck;

    [SerializeField] List<CardScriptables> activeDeck = new List<CardScriptables>();
    [SerializeField] List<CardScriptables> discardPile = new List<CardScriptables>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        activeDeck.AddRange(cardDeck);
        Shuffle();
    }

    public void Shuffle()
    {
        Debug.Log("Start Shuffling");
        //Fisher - Yates
        int n = activeDeck.Count - 1;
        //Iterate from last card, switch with random card
        while (n > 0)
        {
            int k = Random.Range(0, n);
            CardScriptables card = activeDeck[k];
            activeDeck[k] = activeDeck[n];
            activeDeck[n] = card;
            n--;
        }
    }

    [ContextMenu("DrawCard")]
    public CardScriptables DrawCard()
    {
        // Shuffle the discard pile if out of card
        if (activeDeck.Count <= 0)
        {
            activeDeck.Clear();
            activeDeck.AddRange(discardPile);
            discardPile.Clear();
            Shuffle();
        }
        int pos = activeDeck.Count - 1;
        CardScriptables card = activeDeck[pos];
        activeDeck.RemoveAt(pos);

        Debug.Log($"Active: {activeDeck.Count}, Discard: {discardPile.Count}");
        return card;
    }

    public void GetDiscarded(CardScriptables card)
    {
        discardPile.Add(card);
    }
}
