
public interface IItem : ILogicalSpaceOccupant, IActivatable
{
    int PointValue { get; }
    IGate OpensGate { get;  }
    bool EndsLevel { get; }
}
