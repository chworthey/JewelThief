/// <summary>
/// Represents general statistics on the game so far
/// </summary>
public interface IGameStats
{
    /// <summary>
    /// The current level number
    /// </summary>
    int CurrentLevel { get; set; }

    /// <summary>
    /// The current player score
    /// </summary>
    int PlayerScore { get; set; }

    /// <summary>
    /// True if the exit has been unlocked during the current level
    /// </summary>
    bool ExitUnlocked { get; set; }

    /// <summary>
    /// True if the game is over (win or lose)
    /// </summary>
    bool GameOver { get; set; }
}
