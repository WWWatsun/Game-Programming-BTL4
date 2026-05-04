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
    public void OnPointerClick(PointerEventData eventData) // nhi code player thao tac choi uno
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            PlayerController player = hand.GetComponentInParent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning("Cannot find PlayerController from this card.");
                return;
            }

            player.RequestPlayCard(card);
        }
    }
}
