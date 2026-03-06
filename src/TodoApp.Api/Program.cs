using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FastEndpoints;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using Quartz;
using Refit;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using TodoApp.Api.Auth;
using TodoApp.Api.Communication;
using TodoApp.Api.Errors;
using TodoApp.Api.Filters;
using TodoApp.Api.GraphQL;
using TodoApp.Api.Health;
using TodoApp.Api.Hubs;
using TodoApp.Api.Jobs;
using TodoApp.Api.MinimalApi;
using TodoApp.Api.Options;
using TodoApp.Api.Services;
using TodoApp.Api.Validation;
using TodoApp.Api.WebSockets;
using TodoApp.Domain.Abstractions;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Host.UseNLog();

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqUrl = context.Configuration["Serilog:SeqUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqUrl);
    }

    var elasticUrl = context.Configuration["Serilog:ElasticUrl"];
    if (Uri.TryCreate(elasticUrl, UriKind.Absolute, out var elasticUri))
    {
        loggerConfiguration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticUri)
        {
            AutoRegisterTemplate = true,
            IndexFormat = "todoapp-logs-{0:yyyy.MM}"
        });
    }

});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.SectionName));

builder.Services.AddTodoInfrastructure(builder.Configuration);

builder.Services
    .AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddSignInManager<SignInManager<IdentityUser>>()
    .AddEntityFrameworkStores<TodoDbContext>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var oidcOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>() ?? new OidcOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Dynamic";
        options.DefaultAuthenticateScheme = "Dynamic";
        options.DefaultChallengeScheme = "Dynamic";
    })
    .AddPolicyScheme("Dynamic", "Dynamic authentication selector", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }

            if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return "Basic";
            }

            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    })
    .AddJwtBearer("Keycloak", options =>
    {
        options.Authority = oidcOptions.Authority;
        options.Audience = oidcOptions.KeycloakAudience;
        options.RequireHttpsMetadata = false;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/api/v1/auth/oidc/login";
        options.Cookie.Name = "todoapp.auth";
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", _ => { })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = oidcOptions.Authority;
        options.ClientId = oidcOptions.ClientId;
        options.ClientSecret = oidcOptions.ClientSecret;
        options.RequireHttpsMetadata = false;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            "Basic",
            "Keycloak",
            CookieAuthenticationDefaults.AuthenticationScheme);
    });
});

builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<CachedTodoQueryService>();

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
    options.AddPolicy("DefaultCors", policy =>
    {
        if (origins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("fixed", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddResponseCompression();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("todos", policy => policy.Tag("todos").Expire(TimeSpan.FromSeconds(30)));
});

builder.Services.AddHybridCache();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<CorrelationIdActionFilter>();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFastEndpoints();

builder.Services.AddSignalR();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<TodoGraphQlQuery>()
    .AddMutationType<TodoGraphQlMutation>()
    .AddSubscriptionType<TodoGraphQlSubscription>()
    .AddInMemorySubscriptions();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TodoDbContext>()
    .AddCheck<TodoReadHealthCheck>("todo-read");

builder.Services.AddHostedService<StartupSeedHostedService>();
builder.Services.AddHostedService<TodoTickerBackgroundService>();

builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddQuartz(options =>
{
    var jobKey = new JobKey(nameof(TodoReminderQuartzJob));
    options.AddJob<TodoReminderQuartzJob>(job => job.WithIdentity(jobKey));
    options.AddTrigger(trigger => trigger
        .ForJob(jobKey)
        .WithIdentity($"{nameof(TodoReminderQuartzJob)}-trigger")
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(1)).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var externalApiBaseUrl = builder.Configuration["ExternalApi:BaseUrl"] ?? "https://jsonplaceholder.typicode.com";

builder.Services
    .AddHttpClient("todo-http", client => client.BaseAddress = new Uri(externalApiBaseUrl))
    .AddStandardResilienceHandler();

builder.Services
    .AddRefitClient<IExternalTodoRefitClient>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(externalApiBaseUrl))
    .AddStandardResilienceHandler();

builder.Services.AddScoped<TodoRestSharpClient>();
builder.Services.AddScoped<TodoHttpClientProbeService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference("/scalar");
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.Map("/ws/todos", TodoWebSocketEndpoint.HandleAsync);
app.MapHealthChecks("/health");
app.MapHub<TodoHub>("/hubs/todos");
app.MapGraphQL("/graphql");

app.UseFastEndpoints();
app.MapControllers();
app.MapTodoMinimalEndpoints();

app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<HangfireTodoJobs>(
    "todo-open-count",
    job => job.LogOpenTodoCountAsync(CancellationToken.None),
    Cron.Minutely);

app.MapGet("/", () => Results.Ok(new
{
    service = "TodoApp.Api",
    docs = new[] { "/swagger", "/scalar", "/graphql", "/hangfire", "/health" }
}));

app.Run();
