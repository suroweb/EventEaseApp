using System.Text;
using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using EventEase.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

// Credit deduction service
builder.Services.AddScoped<ICreditDeductionService, EventEase.Infrastructure.Services.AI.CreditDeductionService>();

// AI Agent services
builder.Services.AddScoped<IPlanningAgentService, EventEase.Infrastructure.Services.AI.Agents.PlanningAgentService>();
builder.Services.AddScoped<IInvitationAgentService, EventEase.Infrastructure.Services.AI.Agents.InvitationAgentService>();
builder.Services.AddScoped<IAnalyticsAgentService, EventEase.Infrastructure.Services.AI.Agents.AnalyticsAgentService>();
builder.Services.AddScoped<IBudgetAgentService, EventEase.Infrastructure.Services.AI.Agents.BudgetAgentService>();
builder.Services.AddScoped<IIntegrationAgentService, EventEase.Infrastructure.Services.AI.Agents.IntegrationAgentService>();

// ===== Payment Services Configuration =====
builder.Services.AddScoped<IStripePaymentService, EventEase.Infrastructure.Services.Payment.StripePaymentService>();
builder.Services.AddScoped<IPaymentWebhookService, EventEase.Infrastructure.Services.Payment.PaymentWebhookService>();
builder.Services.AddScoped<IInvoiceService, EventEase.Infrastructure.Services.Payment.InvoiceService>();

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

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}))
.WithTags("Health")
.AllowAnonymous();

// Error handling endpoint
app.MapGet("/error", () => Results.Problem("An error occurred processing your request"))
    .ExcludeFromDescription();

// ===== Run Application =====
app.Logger.LogInformation("EventEase API starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Swagger UI available at: {Url}", app.Environment.IsDevelopment() ? "http://localhost:5000" : "N/A");

app.Run();
