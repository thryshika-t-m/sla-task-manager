using SlaTaskManager.API.Entities;
using SlaTaskManager.API.Models;

namespace SlaTaskManager.API.Services;

public class SlaStatusCalculator : ISlaStatusCalculator
{
    public SlaStatus Calculate(Entities.Task task, DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;

        if (task.Status == Entities.TaskStatus.Completed)
        {
            return task.DueAt >= task.LastModified ? SlaStatus.Green : SlaStatus.Red;
        }

        if (now >= task.DueAt)
        {
            return SlaStatus.Red;
        }

        var totalDuration = task.DueAt - task.CreatedAt;
        if (totalDuration <= TimeSpan.Zero)
        {
            return SlaStatus.Red;
        }

        var remaining = task.DueAt - now;
        var remainingPercent = remaining.TotalMilliseconds / totalDuration.TotalMilliseconds;

        return remainingPercent > 0.25 ? SlaStatus.Green : SlaStatus.Amber;
    }
}
