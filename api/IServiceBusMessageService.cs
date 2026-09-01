using Azure.Messaging.ServiceBus;

public interface IServiceBusMessageService
{
    Task SendAsync(
        string body,
        string subject,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);
}