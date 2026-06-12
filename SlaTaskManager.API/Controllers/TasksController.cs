using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SlaTaskManager.API.Data;
using SlaTaskManager.API.Entities;
using SlaTaskManager.API.Hubs;
using SlaTaskManager.API.Models;
using TaskStatus = SlaTaskManager.API.Entities.TaskStatus;
using SlaTaskManager.API.Models.Dtos;
using SlaTaskManager.API.Services;

namespace SlaTaskManager.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly SlaTaskManagerDbContext _dbContext;
    private readonly ISlaStatusCalculator _slaStatusCalculator;
    private readonly ITaskEventPublisher _taskEventPublisher;
    private readonly IHubContext<TaskHub> _taskHubContext;

    public TasksController(
        SlaTaskManagerDbContext dbContext,
        ISlaStatusCalculator slaStatusCalculator,
        ITaskEventPublisher taskEventPublisher,
        IHubContext<TaskHub> taskHubContext)
    {
        _dbContext = dbContext;
        _slaStatusCalculator = slaStatusCalculator;
        _taskEventPublisher = taskEventPublisher;
        _taskHubContext = taskHubContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(tasks.Select(ToTaskResponse));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var activeTasks = tasks.Where(t => t.Status != TaskStatus.Completed).ToList();

        return Ok(new DashboardResponse(
            Green: activeTasks.Count(t => _slaStatusCalculator.Calculate(t) == SlaStatus.Green),
            Amber: activeTasks.Count(t => _slaStatusCalculator.Calculate(t) == SlaStatus.Amber),
            Red: activeTasks.Count(t => _slaStatusCalculator.Calculate(t) == SlaStatus.Red),
            Escalated: tasks.Count(t => t.Status == TaskStatus.Escalated),
            Completed: tasks.Count(t => t.Status == TaskStatus.Completed)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.AuditLogs.OrderByDescending(a => a.ChangedAt))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        return Ok(ToTaskDetailResponse(task));
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = new Entities.Task
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            AssignedTo = request.AssignedTo,
            Priority = request.Priority,
            Status = TaskStatus.Open,
            SlaHours = request.SlaHours,
            CreatedAt = now,
            DueAt = now.AddHours(request.SlaHours),
            LastModified = now
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _taskEventPublisher.PublishAsync(task.Id, TaskEventType.TaskCreated, cancellationToken);

        var taskResponse = ToTaskResponse(task);
        await _taskHubContext.Clients.All.SendAsync("BroadcastTaskUpdate", taskResponse, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, taskResponse);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var previousStatus = task.Status;
        if (previousStatus == request.Status)
        {
            return Ok(ToTaskResponse(task));
        }

        task.Status = request.Status;
        task.LastModified = DateTime.UtcNow;

        if (request.Status == TaskStatus.Escalated)
        {
            task.EscalatedAt = DateTime.UtcNow;
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            Action = $"StatusChanged:{previousStatus}->{request.Status}",
            ChangedAt = task.LastModified,
            Notes = request.Notes ?? $"Status updated from {previousStatus} to {request.Status}"
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _taskEventPublisher.PublishAsync(task.Id, TaskEventType.TaskUpdated, cancellationToken);

        var taskResponse = ToTaskResponse(task);
        await _taskHubContext.Clients.All.SendAsync("BroadcastTaskUpdate", taskResponse, cancellationToken);

        return Ok(taskResponse);
    }

    private TaskResponse ToTaskResponse(Entities.Task task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.AssignedTo,
            task.Priority,
            task.Status,
            task.SlaHours,
            task.CreatedAt,
            task.DueAt,
            task.EscalatedAt,
            task.LastModified,
            _slaStatusCalculator.Calculate(task));

    private TaskDetailResponse ToTaskDetailResponse(Entities.Task task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.AssignedTo,
            task.Priority,
            task.Status,
            task.SlaHours,
            task.CreatedAt,
            task.DueAt,
            task.EscalatedAt,
            task.LastModified,
            _slaStatusCalculator.Calculate(task),
            task.AuditLogs.Select(a => new AuditLogResponse(
                a.Id,
                a.TaskId,
                a.Action,
                a.ChangedAt,
                a.Notes)).ToList());
}
