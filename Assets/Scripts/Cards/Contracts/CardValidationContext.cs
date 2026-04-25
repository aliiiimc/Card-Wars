public sealed class CardValidationContext
{
    public string ActingPlayerKey;
    public string OpponentPlayerKey;
    public IBoardStateReader Board;

    public string ActingPlayerId
    {
        get => ActingPlayerKey;
        set => ActingPlayerKey = value;
    }

    public string OpponentPlayerId
    {
        get => OpponentPlayerKey;
        set => OpponentPlayerKey = value;
    }
}
