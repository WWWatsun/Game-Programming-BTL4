using System.Collections.Generic;
using UnityEngine;

// Empty tag to test the hand.
public class Card : MonoBehaviour
{
    [SerializeField] public CardScriptables card = null;

    private void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = card.cardSprite;
        CardClicking clicker = GetComponent<CardClicking>();
        clicker.SetUp();
    }
    [ContextMenu("DrawACard")]
    void DrawACard()
    {
        card = Deck.Instance.DrawCard();
        Sprite sprite = card.cardSprite;
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
    }
}
