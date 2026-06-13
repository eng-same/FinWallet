using FinWallet.MockBankCore.Worker.Messaging.Consumers;
using MassTransit;
using MassTransit.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

// Register distributed tracing (OpenTelemetry → Jaeger)
// Opt-in: set Observability:Enabled=true via config or environment variable
var observabilityEnabled = builder.Configuration.GetValue<bool>("Observability:Enabled");
if (observabilityEnabled)
{
    var serviceName = builder.Configuration["Observability:ServiceName"] ?? "FinWallet.MockBankCore.Worker";
    var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(
                serviceName: serviceName,
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
        .WithTracing(tracing =>
        {
            tracing
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(otlpEndpoint);
                });
        });
}

var host = builder.Build();
host.Run();
