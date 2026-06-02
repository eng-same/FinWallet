using System.Text;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Infrastructure.Authentication;
using FinWallet.Infrastructure.Messaging.Consumers;
using FinWallet.Infrastructure.Persistence;
using MassTransit;
using FinWallet.Infrastructure.Persistence.Behaviors;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FinWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database persistence setup (PostgreSQL)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=finwallet_db;Username=finwallet_user;Password=finwallet_password";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 2. Identity configuration
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. JWT authentication setup
        var secretKey = configuration["Jwt:SecretKey"] ?? "FinWalletSuperSecretJWTKey2026!WithExtraSecurityCharacters";
        var issuer = configuration["Jwt:Issuer"] ?? "FinWallet.Api";
        var audience = configuration["Jwt:Audience"] ?? "FinWallet.Frontend";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("RequireUser", policy => policy.RequireRole("User"));
        });

        // 4. Register Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // 5. MassTransit with RabbitMQ configuration
        services.AddMassTransit(x =>
        {
            // Register consumer for top-up responses
            x.AddConsumer<BankTopUpProcessedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var username = configuration["RabbitMq:Username"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configure endpoint for top-up responses queue
                cfg.ReceiveEndpoint("finwallet.bank.topup.responses", e =>
                {
                    e.ConfigureConsumer<BankTopUpProcessedConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
