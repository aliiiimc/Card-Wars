namespace FortGame.UI
{
    /// <summary>
    /// Represents the state of player card selection flow.
    /// </summary>
    public enum CardSelectionState
    {
        Idle,
        CardSelected,
        WaitingForTarget,
        Confirmed,
        Cancelled
    }
}
