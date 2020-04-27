/// <summary>
/// Represents an item that can be picked up in the game
/// </summary>
public interface IItem : ILogicalSpaceOccupant, IActivatable
{
    /// <summary>
    /// The point value of the item. Gosh! I should have made the items into prefabs first.
    /// </summary>
    int PointValue { get; }

    /// <summary>
    /// If not null, specifies the gate in which this (presumably it's a key) item will unlock
    /// </summary>
    IGate OpensGate { get; }

    /// <summary>
    /// True if the level will end when this item is picked up.
    /// </summary>
    bool EndsLevel { get; }
}
