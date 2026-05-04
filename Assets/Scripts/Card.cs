using System.Collections.Generic;
using UnityEngine;

// Empty tag to test the hand.
public class Card : MonoBehaviour
{
    [SerializeField] public CardScriptables card = null;

    public bool overrideSpriteByHand = false; //nhi: player0 (hardcode local) chỉ tháy bài của mình, và chỉ thấy số lượng bài của các player khác

    private void Start() //nhi: player0 (hardcode local) chỉ tháy bài của mình, và chỉ thấy số lượng bài của các player khác
    {
        if (!overrideSpriteByHand && card != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = card.cardSprite;
            }
        }

        CardClicking clicker = GetComponent<CardClicking>();

        if (clicker != null)
        {
            clicker.SetUp();
        }
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
