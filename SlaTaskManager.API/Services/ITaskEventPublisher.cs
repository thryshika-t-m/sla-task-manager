using SlaTaskManager.API.Models;

namespace SlaTaskManager.API.Services;

public interface ITaskEventPublisher
{
    Task PublishAsync(Guid taskId, TaskEventType eventType, CancellationToken cancellationToken = default);
}
