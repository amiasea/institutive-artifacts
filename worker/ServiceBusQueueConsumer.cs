using Amiasea.Speculative;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Octokit.Webhooks.Events;

namespace Amiasea.Worker;

public sealed class ServiceBusQueueConsumer : BackgroundService
{
  private readonly ILogger<ServiceBusQueueConsumer> _logger;
  private readonly ServiceBusClient _client;
  private readonly ServiceBusProcessor _processor;
  private readonly IBooking _booking;
  private readonly string _queueName;

  public ServiceBusQueueConsumer(
      IConfiguration configuration,
      ILogger<ServiceBusQueueConsumer> logger,
      IBooking booking)
  {
    _logger = logger;

    var namespaceName = configuration["ServiceBusNamespace"]
        ?? throw new InvalidOperationException(
            "ServiceBusNamespace is not configured.");

    _queueName = configuration["ServiceBusQueueName"]
        ?? throw new InvalidOperationException(
            "ServiceBusQueueName is not configured.");

    _client = new ServiceBusClient(
        namespaceName,
        new DefaultAzureCredential());

    _processor = _client.CreateProcessor(
        _queueName,
        new ServiceBusProcessorOptions
        {
          AutoCompleteMessages = false,
          MaxConcurrentCalls = 1
        });

    _booking = booking;
  }

  protected override async Task ExecuteAsync(
      CancellationToken stoppingToken)
  {
    _processor.ProcessMessageAsync += MessageHandler;
    _processor.ProcessErrorAsync += ErrorHandler;

    _logger.LogInformation(
        "Service Bus queue consumer listening on {QueueName}.",
        _queueName);

    await _processor.StartProcessingAsync(stoppingToken);

    try
    {
      await Task.Delay(
          Timeout.InfiniteTimeSpan,
          stoppingToken);
    }
    catch (OperationCanceledException)
    {
      // Expected during shutdown.
    }
  }

  private async Task MessageHandler(
      ProcessMessageEventArgs args)
  {
    _logger.LogInformation(
        "Message received: {MessageId}",
        args.Message.MessageId);

    try
    {
      var rawBody = args.Message.Body.ToString();

      var source = args.Message.ApplicationProperties["webhookSource"]?.ToString();

      switch (source)
      {
        case "terraform":
          {
            var notification =
                JsonSerializer.Deserialize<TerraformNotification>(rawBody);

            if (notification is null)
            {
              throw new InvalidOperationException(
                  "Terraform notification payload could not be deserialized.");
            }

            // Process notification...



            break;
          }

        case "github":
          {
            var eventType = args.Message.ApplicationProperties["webhookEvent"]?.ToString();

            switch (eventType)
            {
                case "pull_request":
                {
                    var pullRequest = JsonSerializer.Deserialize<PullRequestEvent>(rawBody);

                    // _booking.CreateTicket

                    // Process PullRequestEvent...

                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported GitHub webhook event '{eventType}'.");
            }

            break;
          }

        default:
          throw new InvalidOperationException(
              $"Unknown webhook source '{source}'.");
      }

      await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(
          ex,
          "Error processing message {MessageId}.",
          args.Message.MessageId);

      await args.AbandonMessageAsync(args.Message);
    }
  }

  private Task ErrorHandler(
      ProcessErrorEventArgs args)
  {
    _logger.LogError(
        args.Exception,
        "Service Bus processor error. Entity: {EntityPath}",
        args.EntityPath);

    return Task.CompletedTask;
  }

  public override async Task StopAsync(
      CancellationToken cancellationToken)
  {
    _logger.LogInformation(
        "Stopping Service Bus queue consumer.");

    await _processor.StopProcessingAsync(cancellationToken);
    await _processor.DisposeAsync();
    await _client.DisposeAsync();

    await base.StopAsync(cancellationToken);
  }
}