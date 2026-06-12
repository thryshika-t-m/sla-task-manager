using SlaTaskManager.API.Entities;
using SlaTaskManager.API.Models;
using TaskStatus = SlaTaskManager.API.Entities.TaskStatus;

namespace SlaTaskManager.API.Models.Dtos;

public record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    string AssignedTo,
    TaskPriority Priority,
    TaskStatus Status,
    int SlaHours,
    DateTime CreatedAt,
    DateTime DueAt,
    DateTime? EscalatedAt,
    DateTime LastModified,
    SlaStatus SlaStatus);

public record AuditLogResponse(
    Guid Id,
    Guid TaskId,
    string Action,
    DateTime ChangedAt,
    string Notes);

public record TaskDetailResponse(
    Guid Id,
    string Title,
    string Description,
    string AssignedTo,
    TaskPriority Priority,
    TaskStatus Status,
    int SlaHours,
    DateTime CreatedAt,
    DateTime DueAt,
    DateTime? EscalatedAt,
    DateTime LastModified,
    SlaStatus SlaStatus,
    IReadOnlyList<AuditLogResponse> AuditLogs);

public record CreateTaskRequest(
    string Title,
    string Description,
    string AssignedTo,
    TaskPriority Priority,
    int SlaHours);

public record UpdateTaskRequest(
    TaskStatus Status,
    string? Notes);

public record DashboardResponse(
    int Green,
    int Amber,
    int Red,
    int Escalated,
    int Completed);
