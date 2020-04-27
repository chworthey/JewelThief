
using System.Collections.Generic;

/// <summary>
/// Represents the logical state of the level (minus the scene graph)
/// </summary>
public interface ILevelState
{
    /// <summary>
    /// The list of all pickup items in the current level
    /// </summary>
    IEnumerable<IItem> ActiveItems { get; }

    /// <summary>
    /// The list of all locked gates in the current level
    /// </summary>
    IEnumerable<IGate> ActiveGates { get; }

    /// <summary>
    /// The player's pawn
    /// </summary>
    IPawn Player { get; }

    /// <summary>
    /// The enemy's pawn
    /// </summary>
    IPawn Enemy { get; }

    /// <summary>
    /// The tile map
    /// </summary>
    IMap Map { get; }

    /// <summary>
    /// The status of the game, currently
    /// </summary>
    IGameStats GameStats { get; }

    /// <summary>
    /// The item that leads to victory or defeat
    /// </summary>
    IItem ExitItem { get; }
}
