using Microsoft.EntityFrameworkCore;
using SlaTaskManager.API.Data;
using SlaTaskManager.API.Entities;
using SlaTaskManager.API.Models;
using TaskStatus = SlaTaskManager.API.Entities.TaskStatus;
using SystemTask = System.Threading.Tasks.Task;

namespace SlaTaskManager.API.Services;

public class SlaMonitorService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitorService> _logger;

    public SlaMonitorService(IServiceScopeFactory scopeFactory, ILogger<SlaMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async SystemTask ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA monitor service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckBreachedTasksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error while checking SLA breaches");
            }

            await System.Threading.Tasks.Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async SystemTask CheckBreachedTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SlaTaskManagerDbContext>();
        var taskEventPublisher = scope.ServiceProvider.GetRequiredService<ITaskEventPublisher>();

        var now = DateTime.Now;
        var tasks = await dbContext.Tasks
            .Where(t => t.Status != TaskStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            if (task.DueAt >= now || task.Status == TaskStatus.Escalated)
            {
                continue;
            }

            var previousStatus = task.Status;
            task.Status = TaskStatus.Escalated;
            task.EscalatedAt = now;
            task.LastModified = now;

            dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Action = "Auto-Escalated",
                ChangedAt = now,
                Notes = $"SLA breached at {now:O}. Status changed from {previousStatus} to Escalated."
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await taskEventPublisher.PublishAsync(task.Id, TaskEventType.SlaBreached, cancellationToken);

            _logger.LogWarning(
                "Task {TaskId} auto-escalated due to SLA breach (due at {DueAt})",
                task.Id,
                task.DueAt);
        }
    }
}
