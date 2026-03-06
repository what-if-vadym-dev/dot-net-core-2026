using Microsoft.EntityFrameworkCore;
using TodoApp.Grpc.Services;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTodoInfrastructure(builder.Configuration);
builder.Services.AddGrpc();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
	await dbContext.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
app.MapGrpcService<TodoGrpcService>();
app.MapGet("/", () => "Todo gRPC service is running. Use a gRPC client to call TodoService endpoints.");

app.Run();
