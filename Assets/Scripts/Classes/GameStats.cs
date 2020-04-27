
/// <summary>
/// Implementation of game stats for the game.
/// </summary>
class GameStats : IGameStats
{
    public int CurrentLevel { get; set; }
    public int PlayerScore { get; set; }
    public bool ExitUnlocked { get; set; }
    public bool GameOver { get; set; }
}

