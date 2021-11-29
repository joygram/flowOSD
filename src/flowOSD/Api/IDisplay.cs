namespace flowOSD.Api;

using System;

internal interface IDisplay
{
    IObservable<bool> IsHighRefreshRate { get; }
    IObservable<bool> IsHighRefreshRateSupported { get; }

    void DisableHighRefreshRate();
    void EnableHighRefreshRate();
    void ToggleRefreshRate();
}