using UnityEngine;

public enum CardValue // 0-9, reverse, skip, plus2, plus4 and wild
{
    ZERO, ONE, TWO, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, REVERSE, SKIP, PLUS2, PLUS4, WILD
}

public enum CardColor // 4 colors, neutral for +4 and wild
{
    RED, BLUE, GREEN, YELLOW, NEUTRAL
}


[CreateAssetMenu(fileName = "CardScriptables", menuName = "Scriptable Objects/CardScriptables")]
public class CardScriptables : ScriptableObject
{
    public CardValue cardValue;
    public CardColor cardColor;
    public Sprite cardSprite; 

    public string CardName()
    {
        string color = "";
        switch (cardColor)
        {
            case CardColor.RED: color = "Red"; break;
            case CardColor.BLUE: color = "Blue"; break;
            case CardColor.GREEN: color = "Green"; break;
            case CardColor.YELLOW: color = "Yellow"; break;
        }
        string value = "";
        switch (cardValue)
        {
            case CardValue.ZERO: value = "0"; break;
            case CardValue.ONE: value = "1"; break;
            case CardValue.TWO: value = "2"; break;
            case CardValue.THREE: value = "3"; break;
            case CardValue.FOUR: value = "4"; break;
            case CardValue.FIVE: value = "5"; break;
            case CardValue.SIX: value = "6"; break;
            case CardValue.SEVEN: value = "7"; break;
            case CardValue.EIGHT: value = "8"; break;
            case CardValue.NINE: value = "9"; break;
            case CardValue.REVERSE: value = "REV"; break;
            case CardValue.PLUS2: value = "P2"; break;
            case CardValue.SKIP: value = "SK"; break;
            case CardValue.PLUS4: return "P4"; break;
            case CardValue.WILD: return "Wild"; break;
        }

        return color + value;
    }
}
