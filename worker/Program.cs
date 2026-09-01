using Amiasea.Worker;
using Amiasea.Speculative;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddSingleton<IBooking>();
builder.Services.AddHostedService<ServiceBusQueueConsumer>();

var host = builder.Build();
host.Run();
