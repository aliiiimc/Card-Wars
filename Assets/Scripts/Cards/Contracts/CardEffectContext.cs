public sealed class CardEffectContext
{
    public string ActingPlayerKey;
    public string OpponentPlayerKey;

    public IBoardStateReader Board;
    public ICardStateWriter Writer;

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
