namespace LayoutEngine.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts.Platform.Updates;

public class UpdateScheduler : IUpdateScheduler
{
    public void ScheduleUpdate(IVisualUpdate update)
    {
        throw new NotImplementedException();
    }

    public void ScheduleUpdate(IVisualUpdate update, UpdatePriority priority)
    {
        throw new NotImplementedException();
    }

    public Boolean CancelUpdate(IVisualUpdate update)
    {
        throw new NotImplementedException();
    }

    public void PauseUpdates()
    {
        throw new NotImplementedException();
    }

    public void ResumeUpdates()
    {
        throw new NotImplementedException();
    }

    public Task ProcessUpdatesAsync(double timeBudgetMilliseconds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void ProcessUpdates(double timeBudgetMilliseconds)
    {
        throw new NotImplementedException();
    }

    public Int32 GetPendingUpdateCount()
    {
        throw new NotImplementedException();
    }
}