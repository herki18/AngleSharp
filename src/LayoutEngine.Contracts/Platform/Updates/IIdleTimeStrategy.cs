namespace LayoutEngine.Contracts.Platform.Updates;

using System;

public interface IIdleTimeStrategy
{
    bool HasIdleTime(out double availableTimeMs);
    void ScheduleIdleWork(Action work, double timeoutMs);
}