using UnityEngine;

/// <summary>
/// Represents an object that can be moved by a controller
/// (either a player or AI)
/// </summary>
public interface IPawn : ILogicalSpaceOccupant, IActivatable
{
    /// <summary>
    /// Causes the pawn to move along the specified path
    /// </summary>
    /// <param name="path">The path to move down on each tick.</param>
    void PushMotionPath(LogicalPath path);
}

