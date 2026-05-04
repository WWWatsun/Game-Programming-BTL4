public class TurnManager
{
    private int currentPlayerIndex = 0;
    private int direction = 1;

    private readonly int playerCount;

    public int CurrentPlayerIndex => currentPlayerIndex;
    public int Direction => direction;

    public TurnManager(int playerCount)
    {
        this.playerCount = playerCount;
    }

    public void ReverseDirection()
    {
        direction *= -1;
    }

    public void NextTurn(int extraSkip = 0)
    {
        int step = 1 + extraSkip;
        currentPlayerIndex = Mod(currentPlayerIndex + direction * step, playerCount);
    }

    private int Mod(int value, int modulo)
    {
        return (value % modulo + modulo) % modulo;
    }
}