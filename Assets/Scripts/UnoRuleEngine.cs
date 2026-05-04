public static class UnoRuleEngine
{
    public static bool IsActionCard(CardScriptables card)
    {
        return card.cardValue == CardValue.REVERSE
            || card.cardValue == CardValue.SKIP
            || card.cardValue == CardValue.PLUS2
            || card.cardValue == CardValue.PLUS4
            || card.cardValue == CardValue.WILD;
    }

    public static bool IsLegalMove(
        CardScriptables playedCard,
        CardScriptables topCard,
        CardColor currentColor,
        int playerCardCount
    )
    {
        if (playedCard == null || topCard == null) return false;

        // Custom rule trong spec: không được thắng bằng action/special card.
        if (playerCardCount == 1 && IsActionCard(playedCard))
        {
            return false;
        }

        // Wild hoặc +4.
        if (playedCard.cardColor == CardColor.NEUTRAL)
        {
            return true;
        }

        // Cùng màu hiện tại.
        if (playedCard.cardColor == currentColor)
        {
            return true;
        }

        // Cùng số hoặc cùng action type.
        if (playedCard.cardValue == topCard.cardValue)
        {
            return true;
        }

        return false;
    }
}