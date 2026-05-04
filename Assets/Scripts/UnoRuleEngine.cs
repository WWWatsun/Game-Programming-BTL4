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
        int playerCardCount,
        int pendingPenalty // <-- THÊM MỚI ĐỂ CHECK STACKING
    )
    {
        if (playedCard == null || topCard == null) return false;

        // Custom rule trong spec: không được thắng bằng action/special card.
        if (playerCardCount == 1 && IsActionCard(playedCard))
        {
            return false;
        }

        if (playedCard == null || topCard == null) return false;

        // Luật 4.4: Không được thắng (hết bài) bằng Action Card
        if (playerCardCount == 1 && IsActionCard(playedCard))
        {
            return false;
        }

        // Luật 4.5: Xử lý Stacking khi đang bị phạt rút bài
        if (pendingPenalty > 0)
        {
            // Nếu đang bị +2, chỉ có thể đỡ bằng +2 hoặc +4
            if (topCard.cardValue == CardValue.PLUS2) return playedCard.cardValue == CardValue.PLUS2 || playedCard.cardValue == CardValue.PLUS4;

            // Nếu đang bị +4, CHỈ có thể đỡ bằng +4
            if (topCard.cardValue == CardValue.PLUS4) return playedCard.cardValue == CardValue.PLUS4;

            // Không thể đánh lá khác
            return false; 
        }

        // Nếu không bị phạt, áp dụng luật bình thường

        // Wild hoặc +4.
        if (playedCard.cardColor == CardColor.NEUTRAL) return true;
        // Cùng màu hiện tại.
        if (playedCard.cardColor == currentColor) return true;
        // Cùng số hoặc cùng action type.
        if (playedCard.cardValue == topCard.cardValue) return true;

        return false;
    }
}