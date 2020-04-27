public interface IGameStats
{
    int CurrentLevel { get; set; }
    int PlayerScore { get; set; }
    bool ExitUnlocked { get; set; }
    bool GameOver { get; set; }
}
