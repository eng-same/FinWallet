using FinWallet.MockBankCore.Worker.Messaging.Consumers;
using MassTransit;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddMassTransit(x =>
{
    // Register consumer
    x.AddConsumer<BankTopUpRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configure receive endpoint for top-up requests queue
        cfg.ReceiveEndpoint("finwallet.bank.topup.requests", e =>
        {
            e.ConfigureConsumer<BankTopUpRequestedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
