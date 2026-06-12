namespace SlaTaskManager.API.Models;

public class TaskEventMessage
{
    public Guid TaskId { get; set; }
    public TaskEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
}
