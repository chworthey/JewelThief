
/// <summary>
/// This is part of the game controller registration system.
/// Once an object is registered, they can get notified about important events.
/// </summary>
public interface IGameEventListener
{
    /// <summary>
    /// A tick that is much slower than an update. It's a tick in which all the game events happen
    /// </summary>
    void LogicalTick();

    /// <summary>
    /// Called when the level is about to end and the players are rushing for the exit!
    /// </summary>
    void ExitUnlocked();
}