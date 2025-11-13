using System.Text;
using System.Threading.RateLimiting;
using EventEase.API.GraphQL;
using EventEase.API.GraphQL.DataLoaders;
using EventEase.API.GraphQL.Types;
using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using EventEase.Infrastructure.HealthChecks;
using EventEase.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var configuration = builder.Configuration;

// ===== Database Configuration =====
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
    });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add DbContextFactory for GraphQL DataLoaders
builder.Services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===== Application Services =====
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// Authentication & Authorization Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Context Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();

// ===== AI Services Configuration =====
// HTTP clients for AI providers
builder.Services.AddHttpClient<IOpenAIService, EventEase.Infrastructure.Services.AI.OpenAIService>();
builder.Services.AddHttpClient<IAnthropicService, EventEase.Infrastructure.Services.AI.AnthropicService>();
builder.Services.AddHttpClient<IDeepSeekService, EventEase.Infrastructure.Services.AI.DeepSeekService>();
builder.Services.AddHttpClient<IOllamaService, EventEase.Infrastructure.Services.AI.OllamaService>();

// AI Model Router - Intelligent model selection
builder.Services.AddScoped<IAIModelRouter, EventEase.Infrastructure.Services.AI.AIModelRouter>();

// Credit deduction service
builder.Services.AddScoped<ICreditDeductionService, EventEase.Infrastructure.Services.AI.CreditDeductionService>();

// AI Agent services (Phase 1.4)
builder.Services.AddScoped<IPlanningAgentService, EventEase.Infrastructure.Services.AI.Agents.PlanningAgentService>();
builder.Services.AddScoped<IInvitationAgentService, EventEase.Infrastructure.Services.AI.Agents.InvitationAgentService>();
builder.Services.AddScoped<IAnalyticsAgentService, EventEase.Infrastructure.Services.AI.Agents.AnalyticsAgentService>();
builder.Services.AddScoped<IBudgetAgentService, EventEase.Infrastructure.Services.AI.Agents.BudgetAgentService>();
builder.Services.AddScoped<IIntegrationAgentService, EventEase.Infrastructure.Services.AI.Agents.IntegrationAgentService>();

// Phase 2.0 - Next-Gen AI Services
builder.Services.AddScoped<IEventAssistantService, EventEase.Infrastructure.Services.AI.EventAssistantService>();
builder.Services.AddScoped<IMatchmakingService, EventEase.Infrastructure.Services.AI.MatchmakingService>();
builder.Services.AddScoped<IPredictiveAnalyticsService, EventEase.Infrastructure.Services.AI.PredictiveAnalyticsService>();
builder.Services.AddScoped<IContentGenerationService, EventEase.Infrastructure.Services.AI.ContentGenerationService>();

// ===== Payment Services Configuration =====
builder.Services.AddScoped<IStripePaymentService, EventEase.Infrastructure.Services.Payment.StripePaymentService>();
builder.Services.AddScoped<IPaymentWebhookService, EventEase.Infrastructure.Services.Payment.PaymentWebhookService>();
builder.Services.AddScoped<IInvoiceService, EventEase.Infrastructure.Services.Payment.InvoiceService>();

// ===== Analytics & Reporting Services Configuration (Phase 1.8) =====
builder.Services.AddScoped<IAnalyticsService, EventEase.Infrastructure.Services.Analytics.AnalyticsService>();
builder.Services.AddScoped<IReportService, EventEase.Infrastructure.Services.Analytics.ReportService>();

// ===== Email & Notification Services Configuration =====
builder.Services.AddScoped<IEmailService, EventEase.Infrastructure.Services.Notifications.SendGridEmailService>();
builder.Services.AddScoped<INotificationService, EventEase.Infrastructure.Services.Notifications.NotificationService>();

// ===== Real-Time Services Configuration (SignalR) =====
builder.Services.AddScoped<IRealtimeService, EventEase.Infrastructure.Services.Realtime.RealtimeService>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
    options.StreamBufferCapacity = 10;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
})
.AddStackExchangeRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
{
    // Redis backplane for SignalR scale-out (horizontal scaling)
    options.Configuration.AbortOnConnectFail = false;
});

// ===== Health Checks Configuration =====
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not configured");

builder.Services.AddHealthChecks()
    // PostgreSQL database health check
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "postgres", "ready" })
    // Redis cache health check (optional, degraded if unavailable)
    .AddRedis(
        configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cache", "redis" })
    // External service health checks
    .AddCheck<StripeHealthCheck>("stripe", HealthStatus.Unhealthy, new[] { "payment", "external" })
    .AddCheck<SendGridHealthCheck>("sendgrid", HealthStatus.Degraded, new[] { "email", "external" })
    .AddCheck<OpenAIHealthCheck>("openai", HealthStatus.Degraded, new[] { "ai", "external" })
    .AddCheck<OllamaHealthCheck>("ollama", HealthStatus.Degraded, new[] { "ai", "external" });

// ===== Rate Limiting Configuration =====
builder.Services.AddRateLimiter(options =>
{
    // Default rate limit for API endpoints: 100 requests per minute per IP
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Strict rate limit for AI endpoints: 10 requests per minute per IP (expensive operations)
    options.AddFixedWindowLimiter("ai", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Authentication endpoints: 5 requests per minute per IP (prevent brute force)
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    // Sliding window for GraphQL: 50 requests per minute
    options.AddSlidingWindowLimiter("graphql", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 50;
        opt.QueueLimit = 5;
        opt.SegmentsPerWindow = 6; // 10-second segments
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Default rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : null
        }, cancellationToken: cancellationToken);
    };
});

// ===== Distributed Caching Configuration (Redis) =====
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "EventEase:";
});

// ===== JWT Authentication Configuration =====
var jwtSecret = configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "EventEase";
var jwtAudience = configuration["JwtSettings:Audience"] ?? "EventEaseAPI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero // No tolerance for token expiration
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Allow SignalR to receive JWT from query string
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ===== Authorization Policies =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireRole("SystemAdmin"));

    options.AddPolicy("TenantOwnerOrAdmin", policy =>
        policy.RequireRole("SystemAdmin", "TenantOwner", "TenantAdmin"));

    options.AddPolicy("EventManagerOrAbove", policy =>
        policy.RequireRole("SystemAdmin", "TenantOwner", "TenantAdmin", "EventManager"));

    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" }
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// ===== GraphQL Configuration =====
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddType<EventType>()
    .AddType<GuestType>()
    .AddType<RegistrationType>()
    .AddType<TenantType>()
    .AddType<UserType>()
    .AddDataLoader<EventByIdDataLoader>()
    .AddDataLoader<GuestByIdDataLoader>()
    .AddDataLoader<RegistrationByIdDataLoader>()
    .AddDataLoader<TenantByIdDataLoader>()
    .AddDataLoader<UserByIdDataLoader>()
    .AddDataLoader<RegistrationsByEventIdDataLoader>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .AddAuthorization()
    .AddInMemorySubscriptions()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        opt.ExecutionTimeout = TimeSpan.FromSeconds(30);
    });

// ===== API Controllers =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ===== API Documentation (Swagger) =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventEase API",
        Version = "v1",
        Description = "Multi-tenant SaaS platform for event management with AI agents",
        Contact = new OpenApiContact
        {
            Name = "EventEase Support",
            Email = "support@eventease.com"
        }
    });

    // JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ===== Build Application =====
var app = builder.Build();

// ===== HTTP Request Pipeline Configuration =====

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EventEase API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseHttpsRedirection();
app.UseCors();

// Rate Limiting
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

// ===== GraphQL Endpoint =====
app.MapGraphQL("/graphql")
    .WithOptions(new HotChocolate.AspNetCore.GraphQLServerOptions
    {
        Tool = {
            Enable = builder.Environment.IsDevelopment()
        }
    })
    .RequireRateLimiting("graphql");

// ===== SignalR Hub Endpoints =====
app.MapHub<EventEase.Infrastructure.Hubs.EventHub>("/hubs/events")
    .RequireAuthorization();

app.MapHub<EventEase.Infrastructure.Hubs.NotificationHub>("/hubs/notifications")
    .RequireAuthorization();

app.MapHub<EventEase.Infrastructure.Hubs.AnalyticsHub>("/hubs/analytics")
    .RequireAuthorization("TenantOwnerOrAdmin");

// ===== Health Check Endpoints =====
// Comprehensive health check with all dependencies
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data,
                tags = e.Value.Tags
            })
        }, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = app.Environment.IsDevelopment()
        });
        await context.Response.WriteAsync(result);
    }
})
.WithTags("Health")
.AllowAnonymous();

// Kubernetes/Docker readiness probe (checks database only)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        });
        await context.Response.WriteAsync(result);
    }
})
.WithTags("Health")
.AllowAnonymous();

// Kubernetes/Docker liveness probe (simple alive check)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks, just return 200 if app is running
})
.WithTags("Health")
.AllowAnonymous();

// Error handling endpoint
app.MapGet("/error", () => Results.Problem("An error occurred processing your request"))
    .ExcludeFromDescription();

// ===== Run Application =====
app.Logger.LogInformation("EventEase API starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Swagger UI available at: {Url}", app.Environment.IsDevelopment() ? "http://localhost:5000" : "N/A");
app.Logger.LogInformation("GraphQL endpoint available at: /graphql");
app.Logger.LogInformation("GraphQL Banana Cake Pop UI available at: /graphql (in development mode)");

app.Run();
