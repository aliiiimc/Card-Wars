// Unified target payload carrying board coordinates, card references, player identifiers, and entity IDs for validators and effects.
using System;

[Serializable]
public struct CardTarget
{
    public CardTargetType type;

    public AxialCoord tile;

    public CardRuntimeState targetCard;

    public string targetPlayerId;

    public string targetEntityId;
}
