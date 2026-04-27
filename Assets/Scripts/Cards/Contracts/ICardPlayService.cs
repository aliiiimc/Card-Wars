public interface ICardPlayService
{
    CardPlayResult PlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target);
}
