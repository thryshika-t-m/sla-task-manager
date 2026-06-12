namespace SlaTaskManager.API.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Task Task { get; set; } = null!;
}
