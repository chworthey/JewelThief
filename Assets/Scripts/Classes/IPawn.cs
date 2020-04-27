using UnityEngine;


public interface IPawn : ILogicalSpaceOccupant, IActivatable
{
    void PushMotionPath(LogicalPath path);
}

