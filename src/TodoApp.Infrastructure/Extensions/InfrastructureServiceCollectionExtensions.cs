using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using TodoApp.Domain.Abstractions;
using TodoApp.Infrastructure.Caching;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Decorators;
using TodoApp.Infrastructure.Integrations;
using TodoApp.Infrastructure.Repositories;
using TodoApp.Infrastructure.Security;
using ZiggyCreatures.Caching.Fusion;

namespace TodoApp.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTodoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=todoapp.db";
        var soapBaseUrl = configuration["LegacySoap:BaseUrl"] ?? "https://postman-echo.com";

        services.AddDbContext<TodoDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<ITodoRepository, EfTodoRepository>();
        services.Decorate<ITodoRepository, LoggingTodoRepositoryDecorator>();

        services.AddScoped<ITodoReadRepository, DapperTodoReadRepository>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

        services
            .AddHttpClient<ILegacySoapNotifier, SoapLegacyNotifier>(client =>
            {
                client.BaseAddress = new Uri(soapBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(8);
            });

        services.AddFusionCache();
        services.AddSingleton<IFusionTodoCache, FusionTodoCache>();

        return services;
    }
}