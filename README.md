# TodoApp ASP.NET Core 2026 Tech Stack Sample

This solution demonstrates a Todo domain across most technologies for ASP.NET Core stack.

## Projects

- `src/TodoApp.Domain` — domain entities, abstractions, query models
- `src/TodoApp.Infrastructure` — EF Core + SQLite, Dapper, Scrutor decorator, SOAP client, FusionCache wrapper
- `src/TodoApp.Api` — REST controllers, Minimal APIs, FastEndpoints, GraphQL, SignalR/WebSockets, auth, docs, scheduling, caching
- `src/TodoApp.Grpc` — gRPC Todo service

## Run

```powershell
dotnet restore asp-net-core.sln --configfile NuGet.config
dotnet run --project src/TodoApp.Api/TodoApp.Api.csproj
```

In another terminal:

```powershell
dotnet run --project src/TodoApp.Grpc/TodoApp.Grpc.csproj
```

## API Endpoints

- REST (versioned controllers): `/api/v1/todos`
- Minimal APIs: `/minimal/todos`
- FastEndpoints: `/fast/todos`
- Auth (Identity/JWT/Cookie/OIDC): `/api/v1/auth/*`
- API communication demos (Refit/RestSharp/HttpClientFactory): `/api/v1/communication/*`
- GraphQL + subscriptions: `/graphql`
- SignalR hub: `/hubs/todos`
- Raw WebSocket endpoint: `/ws/todos`
- Health checks: `/health`
- Hangfire dashboard: `/hangfire`
- Swagger UI: `/swagger`
- Scalar API reference: `/scalar`

## Included Technologies

- Basics: routing, middleware, filters, options, auth attributes, authorization, error handling, `ProblemDetails`, hosted/background services, health checks, response compression, rate limiting, API versioning, controllers, minimal APIs, FastEndpoints
- REST fundamentals: status codes, HATEOAS, filtering/sorting/pagination/data shaping
- Data: EF Core + Dapper
- DI: Microsoft DI + Scrutor decorator
- Validation: DataAnnotations + FluentValidation
- Mapping: manual mapping only
- Logging/monitoring: Microsoft logging, Serilog, NLog, Seq/Elasticsearch/Loki sink wiring
- API docs: OpenAPI, Swagger, Scalar
- Security: CORS, Keycloak/OIDC configuration, cookies, JWT, refresh tokens, basic auth, OAuth2 flow setup, ASP.NET Core Identity
- API communication: REST, gRPC, HttpClientFactory, GraphQL (HotChocolate), SOAP (legacy client style)
- Scheduling: background ticker, Hangfire, Quartz
- HTTP clients: Refit, RestSharp, Polly via `Microsoft.Extensions.Http.Resilience`
- Caching: OutputCache, HybridCache, FusionCache, StackExchange.Redis package included
- Real-time: WebSockets, SignalR, GraphQL subscriptions

## Notes

- Default SQLite files are local (`todoapp*.db`).
- A known warning exists in transitive `Newtonsoft.Json` 11.0.1 from third-party packages.
