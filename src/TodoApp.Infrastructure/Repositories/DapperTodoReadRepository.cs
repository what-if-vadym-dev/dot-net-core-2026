using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;

namespace TodoApp.Infrastructure.Repositories;

public sealed class DapperTodoReadRepository(IConfiguration configuration) : ITodoReadRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=todoapp.db";

    public async Task<int> CountOpenAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM Todos WHERE IsCompleted = 0;";
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<TodoSummaryRow>> GetOverdueAsync(int take, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT Id, Title, DueAtUtc, IsCompleted, Priority
FROM Todos
WHERE IsCompleted = 0 AND DueAtUtc IS NOT NULL AND DueAtUtc < $Now
ORDER BY DueAtUtc ASC
LIMIT $Take;";

        using var connection = CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                Now = DateTime.UtcNow,
                Take = Math.Clamp(take, 1, 100)
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TodoSummaryRow>(command);
        return rows.ToList();
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}