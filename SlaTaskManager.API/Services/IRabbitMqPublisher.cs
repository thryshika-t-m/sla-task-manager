namespace SlaTaskManager.API.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string routingKey, object message, CancellationToken cancellationToken = default);
}
