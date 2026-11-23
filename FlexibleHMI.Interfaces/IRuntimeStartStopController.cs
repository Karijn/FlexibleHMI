namespace FlexibleHMI.Interfaces;

/// <summary>Controls an item's runtime activity.</summary>
public interface IRuntimeStartStopController
{
    /// <summary>Starts runtime activity.</summary>
    void Start();

    /// <summary>Stops runtime activity.</summary>
    void Stop();
}
