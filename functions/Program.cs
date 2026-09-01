using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<ServiceBusClient>(serviceProvider =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var namespaceName =
        configuration["ServiceBusNamespace"]
        ?? throw new InvalidOperationException(
            "ServiceBusNamespace is not configured.");

    return new ServiceBusClient(
        namespaceName,
        new DefaultAzureCredential());
});

builder.Services.AddSingleton<ServiceBusSender>(serviceProvider =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var queueName =
        configuration["ServiceBusQueueName"]
        ?? throw new InvalidOperationException(
            "ServiceBusQueueName is not configured.");

    var client =
        serviceProvider.GetRequiredService<ServiceBusClient>();

    return client.CreateSender(queueName);
});

builder.Build().Run();