using Azure.Messaging.ServiceBus;

public sealed class ServiceBusMessageService : IServiceBusMessageService
{
    private readonly ServiceBusSender _sender;

    public ServiceBusMessageService(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public Task SendAsync(
        string body,
        string subject,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = subject
        };

        if (properties is not null)
        {
            foreach (var property in properties)
            {
                message.ApplicationProperties[property.Key] = property.Value;
            }
        }

        return _sender.SendMessageAsync(
            message,
            cancellationToken);
    }
}