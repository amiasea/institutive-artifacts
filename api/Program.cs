using Azure.Identity;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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

builder.Services.AddSingleton<IServiceBusMessageService, ServiceBusMessageService>();

var app = builder.Build();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "Amiasea API v1");

    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.MapGet(
    "/",
    () => Results.Redirect("/swagger"));

app.Run();