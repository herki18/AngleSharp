namespace LayoutEngine.Contracts.Platform.Events;

using Infrastructure.EventAggregator.API.Events;

/// <summary>
/// Event raised when the device pixel ratio changes.
/// </summary>
public class DevicePixelRatioChangedEvent : EventBase
{
    /// <summary>
    /// Gets the old device pixel ratio.
    /// </summary>
    public double OldDevicePixelRatio { get; }

    /// <summary>
    /// Gets the new device pixel ratio.
    /// </summary>
    public double NewDevicePixelRatio { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePixelRatioChangedEvent"/> class.
    /// </summary>
    /// <param name="oldDevicePixelRatio">The old device pixel ratio.</param>
    /// <param name="newDevicePixelRatio">The new device pixel ratio.</param>
    public DevicePixelRatioChangedEvent(double oldDevicePixelRatio, double newDevicePixelRatio)
    {
        OldDevicePixelRatio = oldDevicePixelRatio;
        NewDevicePixelRatio = newDevicePixelRatio;
    }
}