using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
public class CardClicking : MonoBehaviour, IPointerClickHandler
{
    public CardScriptables card;
    public Hand hand;

    public void SetUp()
    {
        card = GetComponent<Card>().card;
        hand = GetComponentInParent<Hand>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            hand.Discard(card);
            Debug.Log($"Remove {card.CardName()}");
        }
    }
}
