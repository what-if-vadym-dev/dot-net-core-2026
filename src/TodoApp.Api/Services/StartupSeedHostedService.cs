using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Api.Services;

public sealed class StartupSeedHostedService(IServiceProvider serviceProvider, ILogger<StartupSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.Todos.AnyAsync(cancellationToken))
        {
            db.Todos.AddRange(
                new TodoItem
                {
                    Title = "Create architecture diagram",
                    Description = "Publish initial system topology",
                    Priority = TodoPriority.High,
                    DueAtUtc = DateTime.UtcNow.AddDays(2)
                },
                new TodoItem
                {
                    Title = "Implement refresh token flow",
                    Description = "Finish auth endpoints",
                    Priority = TodoPriority.Critical,
                    DueAtUtc = DateTime.UtcNow.AddDays(1)
                });

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial Todo data");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByNameAsync("admin");
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = "admin",
                Email = "admin@todo.local",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "P@ssw0rd!");
            if (!result.Succeeded)
            {
                logger.LogWarning("Admin user creation failed: {Errors}", string.Join(',', result.Errors.Select(e => e.Description)));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}