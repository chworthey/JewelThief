/// <summary>
/// Represents an object that can be activated and deactivated at will.
/// Currently this is how we "load/unload" levels.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Turns the object on
    /// </summary>
    void Activate();

    /// <summary>
    /// Turns the object off
    /// </summary>
    void Deactivate();
}
