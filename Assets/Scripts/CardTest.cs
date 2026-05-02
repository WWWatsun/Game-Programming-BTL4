using System.Collections.Generic;
using UnityEngine;

// Empty tag to test the hand.
public class CardTest : MonoBehaviour
{
    [SerializeField] CardScriptables card = null;

    [ContextMenu("DrawACard")]
    void DrawACard()
    {
        card = Deck.Instance.DrawCard();
        Sprite sprite = card.cardSprite;
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
    }

}
