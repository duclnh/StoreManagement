# Identity service architecture summary for agent

## 1. Overview

This repository uses an ASP.NET Core microservice-style architecture for the Identity domain. The service is split into clean layers:

- `Identity.Api`: HTTP entry point and routing
- `Identity.Application`: use cases, behavior pipeline, validation, mapping
- `Identity.Contract`: DTOs, request/response contracts and shared abstractions
- `Identity.Domain`: domain models, aggregation rules, repositories contracts
- `Identity.Infrastructure`: JWT, Redis cache, RabbitMQ/MassTransit, Quartz jobs, health checks
- `Identity.Persistence`: EF Core + ASP.NET Identity integration, repositories, UoW, interceptors

The main message flow follows CQRS-style MediatR requests: API endpoint -> query/command contract -> handler -> application services -> infrastructure/persistence.

---

## 2. Startup composition

Main bootstrap is in `src/Services/Identity/Identity.Api/Program.cs`.

Important registrations:

- `AddCarter()` enables minimal API module routing
- Swagger + API versioning + JWT auth are configured here
- `AddMediatorApplication()` registers MediatR and pipeline behaviors
- `AddAutoMapperApplication()` registers AutoMapper
- `AddQuartzInfrastructure()` adds background job hosting
- `AddServiceInfrastructure()` adds JWT and cache services
- `AddMediatRInfrastructure()` registers application handlers from assembly
- `AddRedisInfrastructure()` binds Redis
- `AddMassTransitRabbitMQInfrastructure()` sets up RabbitMQ + consumers
- `AddHealthChecksInfrastructure()` configures health endpoints
- `AddSqlServerPersistence()` configures EF Core + ASP.NET Identity
- `AddRepositoryPersistence()` registers UnitOfWork/Repository abstractions

The app pipeline order is:

1. exception middleware
2. Carter route mapping
3. Swagger in dev/staging
4. HTTPS redirection
5. authentication
6. authorization
7. health checks

---

## 3. HTTP entry point and routing

The HTTP API is defined with Carter, not classic controllers.

Primary file:
- `src/Services/Identity/Identity.Api/Controllers/Identity/AuthApi.cs`

This module exposes versioned endpoints:

- `POST /api/v1/auths/login`
- `POST /api/v1/auths/revoke`
- `POST /api/v1/auths/refresh`

Important detail:

- `AuthApi` inherits from `ApiEndpoint`
- It matches `ISender` from MediatR
- Request body types are defined in `Identity.Contract.Services.V1.Identity.Query`
- `ApiEndpoint.HandlerFailure(...)` converts `Result` failures into `ProblemDetails`

Current code pattern:

```csharp
private async Task<IResult> AuthenticationV1(ISender sender, [FromBody] Login login)
{
    var result = await sender.Send(login);
    if (result.IsFailure)
        return HandlerFailure(result);

    return Results.Ok(result);
}
```

This means the endpoint is thin and does not contain business logic; it simply dispatches to the MediatR request.

---

## 4. Contract layer

Files:
- `src/Services/Identity/Identity.Contract/Services/V1/Identity/Query.cs`
- `src/Services/Identity/Identity.Contract/Abstraction/Message/IQuery.cs`
- `src/Services/Identity/Identity.Contract/Abstraction/Shared/Result.cs`

The requests are defined as record types implementing `IQuery<TResponse>`:

```csharp
public record Login(string Email, string Password) : IQuery<Authenticated>;
public record Token(string? AccessToken, string? RefreshToken) : IQuery<Authenticated>;
public record RefreshToken() : IQuery<Authenticated>;
public record RevokeToken() : IQuery<Unit>;
```

The service uses a custom result wrapper instead of throwing exceptions for normal business validation. `Result<T>` is the universal success/failure envelope used throughout the app.

---

## 5. Application layer and MediatR flow

File:
- `src/Services/Identity/Identity.Application/DependencyInjection/Extensions/ServiceCollectionExtensions.cs`

This layer registers:

- MediatR handlers from `Identity.Application`
- Validation pipeline behavior
- Performance pipeline behavior
- Transaction pipeline behavior
- FluentValidation validators from the contract assembly

The key registration is:

```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformancePipelineBehavior<,>))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
```

That means every request passes through reusable pipeline behaviors before the actual handler runs.

---

## 6. Current login handler and function behavior

Main handler:
- `src/Services/Identity/Identity.Application/UserCases/V1/Queries/Identity/GetLoginQueryHandler.cs`

Current code does the following:

1. Accepts `Query.Login` request with `Email` and `Password`
2. Builds a claim list with email and role
3. Calls `IJwtTokenService.GenerateAccessToken(claims)`
4. Calls `IJwtTokenService.GenerateRefreshToken()`
5. Creates `Response.Authenticated`
6. Stores the authenticated response in Redis cache with `ICacheService.SetAsync(...)`
7. Returns `Result.Success(response)`

Important observation:

- The method contains placeholder comments like `//check user`, so actual user validation is not implemented yet
- It currently creates a role of `"Senior .NET leader"` without checking database or ASP.NET Identity records
- This indicates the login flow is more of a token-generation stub than a finalized authentication process

There is also a `GetRefreshTokenQueryHandler` in the same directory that appears to be a duplicate / placeholder implementation of the same logic.

---

## 7. Authentication and JWT config

File:
- `src/Services/Identity/Identity.Api/DependencyInjection/Extensions/JwtExtensions.cs`

JWT is configured in `AddJwtAuthenticationApplication`:

- default scheme is JWT bearer
- issuer/audience are read from `JwtOptions`
- signing key is read from configuration (`SecretKey`)
- `ValidateLifetime = true`
- `ClockSkew = TimeSpan.Zero`
- custom header `IS-TOKEN-EXPIRED` is added when the token is expired

This is the auth boundary for incoming API requests. The `RequireAuthorization()` call on the Carter group means auth is enforced at route-level unless `AllowAnonymous()` is set.

---

## 8. Infrastructure services

Files:
- `src/Services/Identity/Identity.Infrastructure/DependencyInjections/Extensions/ServiceCollectionExtensions.cs`

This layer adds cross-cutting infrastructure:

### JWT and cache
- `IJwtTokenService` -> `JwtTokenService`
- `ICacheService` -> `CacheService`

### Redis
- `AddStackExchangeRedisCache` using `ConnectionStrings:Redis`

### MassTransit + RabbitMQ
- Registers consumers from the current assembly
- Uses Kebab-case naming formatter
- Uses RabbitMQ host from `MassTransitOptions`
- Adds retry policy via `MessageBusOptions`
- Configures `NewtonsoftJson` serializers with custom converters
- Connects logging observers for Receive/Consume/Publish/Send

### Quartz
- Adds `ProcessOutboxMessagesJob`
- Schedules the job to run repeatedly with a short interval

### Health checks
- SQL Server
- Redis
- RabbitMQ
- MongoDB

This explains why infrastructure is intentionally shared between async messaging, cache, and background processing.

---

## 9. Persistence layer

Files:
- `src/Services/Identity/Identity.Persistence/DependencyInjections/Extensions/ServiceCollectionExtensions.cs`
- `src/Services/Identity/Identity.Persistence/ApplicationDbContext.cs`

This layer wires the database stack:

- `AddDbContextPool<DbContext, ApplicationDbContext>()`
- `UseSqlServer(...)` with `DefaultConnectString`
- `UseLazyLoadingProxies(true)`
- detailed errors + sensitive logging enabled
- `AddInterceptors(...)` for domain event outbox and auditing

It also configures ASP.NET Identity:

```csharp
services.AddIdentityCore<AppUser>(opt => { ... })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

This means identity tables, roles, and user management are stored in the same SQL database that the service uses for persistence.

The repository abstraction is registered as:

- `IUnitOfWork`
- `IRepositoryBase<,>`

---

## 10. Domain and event/outbox model

The project includes Outbox patterns and domain event conversion.

Relevant infrastructure:
- `Identity.Persistence/Interceptors/ConvertDomainEventsToOutboxMessagesInterceptor.cs`
- `Identity.Persistence/Outbox/*`
- `Identity.Infrastructure/Consumers/*`

The project keeps domain events and integration events separated from immediate business handling. The interceptor and Quartz background worker suggest an outbox pattern to ensure messages are dispatched reliably after database commits.

---

## 11. Request flow in one path

### Login request path

```text
HTTP request
  -> AuthApi.AuthenticationV1
  -> MediatR sender.Send(login)
  -> Query.Login
  -> GetLoginQueryHandler.Handle
  -> IJwtTokenService.GenerateAccessToken
  -> IJwtTokenService.GenerateRefreshToken
  -> ICacheService.SetAsync(...)
  -> Result.Success(Response.Authenticated)
  -> ApiEndpoint.HandlerFailure or Results.Ok
  -> HTTP response
```

### Why this matters for agent work

When implementing or fixing auth behavior, this is the exact path to modify:

1. `src/Services/Identity/Identity.Api/Controllers/Identity/AuthApi.cs`
2. `src/Services/Identity/Identity.Contract/Services/V1/Identity/Query.cs`
3. `src/Services/Identity/Identity.Application/UserCases/V1/Queries/Identity/GetLoginQueryHandler.cs`
4. JWT service in the infrastructure layer
5. identity persistence / user repository if real user validation is added

---

## 12. Important implementation notes / caveats

These are the main things an agent should know before changing behavior:

- `AuthApi` uses Carter, not MVC controllers, so route registration is in ICarterModule classes
- `RequireAuthorization()` is applied to group, but `AllowAnonymous()` is explicitly set on login/revoke/refresh endpoints
- The auth logic is partially stubbed; actual user lookup is commented out
- Refresh and revoke endpoints currently use the same `Login` request model as login, which is likely incomplete or accidental
- The app is configured as a hybrid service: SQL for identity data + Redis + RabbitMQ + background jobs
- The app is built around durable event processing, not just a simple API

---

## 13. Best starting points for agent changes

If the goal is to fix or extend business logic, start here:

- `AuthApi.cs` for endpoint contract and route exposure
- `Query.cs` for API contracts / request types
- `GetLoginQueryHandler.cs` for actual authentication token creation logic
- `JwtExtensions.cs` for JWT validation rules
- `ApplicationDbContext.cs` and ASP.NET Identity setup for user/role persistence
- `ServiceCollectionExtensions.cs` in infrastructure for cache and RabbitMQ behavior

---

## 14. Short mental model

Think of this service as:

- API shell: route + auth guard
- Application core: MediatR handlers + validation + business rules
- Domain + persistence: user/role storage, repository, unit of work
- Infrastructure: JWT, Redis, messaging, job scheduler, observability

The agent should not treat this as just a classic Controller + Service pattern; the real orchestration is driven by Carter + MediatR + infrastructure registration + EF Identity.
