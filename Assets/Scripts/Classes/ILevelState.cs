
using System.Collections.Generic;

public interface ILevelState
{
    IEnumerable<IItem> ActiveItems { get; }
    IEnumerable<IGate> ActiveGates { get; }
    IPawn Player { get; }
    IPawn Enemy { get; }
    IMap Map { get; }
    IGameStats GameStats { get; }
    IItem ExitItem { get; }
}
