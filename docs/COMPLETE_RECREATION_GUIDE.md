# Complete Recreation Guide: EventEaseApp

**Version:** 2.0.0
**Date:** 2025-11-14
**Estimated Effort:** 280 hours (7 weeks for 1 developer)
**Difficulty:** Advanced

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Solution Structure Creation](#solution-structure-creation)
4. [Domain Layer Implementation](#domain-layer-implementation)
5. [Application Layer Implementation](#application-layer-implementation)
6. [Infrastructure Layer Implementation](#infrastructure-layer-implementation)
7. [API Layer Implementation](#api-layer-implementation)
8. [Database Setup & Migrations](#database-setup--migrations)
9. [Configuration Files](#configuration-files)
10. [Testing Implementation](#testing-implementation)
11. [Deployment](#deployment)
12. [Verification & Testing](#verification--testing)

---

## Prerequisites

### Required Knowledge
- **C# 12.0** - Advanced level (records, pattern matching, nullable reference types)
- **ASP.NET Core 9.0** - Intermediate to Advanced
- **Entity Framework Core 9.0** - Intermediate (migrations, query filters, configurations)
- **PostgreSQL** - Basic to Intermediate
- **Redis** - Basic
- **JWT Authentication** - Intermediate
- **Multi-tenancy Patterns** - Intermediate
- **REST API Design** - Intermediate
- **GraphQL** - Basic to Intermediate
- **SignalR** - Basic
- **Docker** - Basic
- **Kubernetes** - Basic (for deployment)

### Development Tools
- **Visual Studio 2022** (17.8+) or **JetBrains Rider** (2023.3+)
- **.NET 9.0 SDK** (9.0.100+)
- **PostgreSQL 15+** with pgvector extension
- **Redis 7+**
- **Docker Desktop** (optional but recommended)
- **Postman** or **Insomnia** (for API testing)
- **Git** (for version control)

### External Service Accounts (Optional for Full Functionality)
- **Stripe** (test mode) - For payments
- **SendGrid** - For emails
- **OpenAI API** - For AI features
- **Anthropic API** - For Claude AI
- **DeepSeek API** - For DeepSeek AI
- **Ollama** (local) - For free local AI models

---

## Environment Setup

### Step 1: Install .NET 9.0 SDK

```bash
# Download from https://dotnet.microsoft.com/download/dotnet/9.0
# Verify installation
dotnet --version
# Should output: 9.0.100 or higher
```

### Step 2: Install PostgreSQL 15+

```bash
# macOS (Homebrew)
brew install postgresql@15
brew services start postgresql@15

# Ubuntu/Debian
sudo apt update
sudo apt install postgresql-15 postgresql-contrib-15
sudo systemctl start postgresql

# Windows
# Download installer from https://www.postgresql.org/download/windows/

# Install pgvector extension
git clone https://github.com/pgvector/pgvector.git
cd pgvector
make
sudo make install

# Or use Docker
docker run --name eventease-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=your_password_here \
  -e POSTGRES_DB=eventease_saas \
  -p 5432:5432 \
  -d ankane/pgvector:latest
```

### Step 3: Install Redis 7+

```bash
# macOS (Homebrew)
brew install redis
brew services start redis

# Ubuntu/Debian
sudo apt install redis-server
sudo systemctl start redis-server

# Windows - Use Docker
docker run --name eventease-redis \
  -p 6379:6379 \
  -d redis:7-alpine

# Verify
redis-cli ping
# Should output: PONG
```

### Step 4: Install Ollama (Optional - for FREE local AI)

```bash
# macOS/Linux
curl -fsSL https://ollama.ai/install.sh | sh

# Windows
# Download from https://ollama.ai/download

# Pull models
ollama pull llama3
ollama pull mistral
ollama pull codellama

# Verify
ollama list
```

---

## Solution Structure Creation

### Step 1: Create Solution Directory

```bash
mkdir EventEaseApp
cd EventEaseApp
```

### Step 2: Create Solution and Projects

```bash
# Create solution
dotnet new sln -n EventEase

# Create Domain project (Class Library)
dotnet new classlib -n EventEase.Domain -f net9.0
dotnet sln add EventEase.Domain/EventEase.Domain.csproj

# Create Application project (Class Library)
dotnet new classlib -n EventEase.Application -f net9.0
dotnet sln add EventEase.Application/EventEase.Application.csproj

# Create Infrastructure project (Class Library)
dotnet new classlib -n EventEase.Infrastructure -f net9.0
dotnet sln add EventEase.Infrastructure/EventEase.Infrastructure.csproj

# Create API project (Web API)
dotnet new webapi -n EventEase.API -f net9.0
dotnet sln add EventEase.API/EventEase.API.csproj

# Create test projects
dotnet new xunit -n EventEase.Domain.Tests -f net9.0
dotnet sln add tests/EventEase.Domain.Tests/EventEase.Domain.Tests.csproj

dotnet new xunit -n EventEase.Application.Tests -f net9.0
dotnet sln add tests/EventEase.Application.Tests/EventEase.Application.Tests.csproj

dotnet new xunit -n EventEase.Infrastructure.Tests -f net9.0
dotnet sln add tests/EventEase.Infrastructure.Tests/EventEase.Infrastructure.Tests.csproj

dotnet new webapi -n EventEase.API.Tests -f net9.0
dotnet sln add tests/EventEase.API.Tests/EventEase.API.Tests.csproj
```

### Step 3: Add Project References

```bash
# Application depends on Domain
dotnet add EventEase.Application/EventEase.Application.csproj reference EventEase.Domain/EventEase.Domain.csproj

# Infrastructure depends on Domain and Application
dotnet add EventEase.Infrastructure/EventEase.Infrastructure.csproj reference EventEase.Domain/EventEase.Domain.csproj
dotnet add EventEase.Infrastructure/EventEase.Infrastructure.csproj reference EventEase.Application/EventEase.Application.csproj

# API depends on Application and Infrastructure
dotnet add EventEase.API/EventEase.API.csproj reference EventEase.Application/EventEase.Application.csproj
dotnet add EventEase.API/EventEase.API.csproj reference EventEase.Infrastructure/EventEase.Infrastructure.csproj

# Test projects (add necessary references)
```

### Step 4: Install NuGet Packages

```bash
# Domain project - No external dependencies

# Application project
cd EventEase.Application
dotnet add package MediatR --version 12.4.1
dotnet add package FluentValidation --version 11.10.0
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.10.0

# Infrastructure project
cd ../EventEase.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.2
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.Design --version 2.0.0
dotnet add package Pgvector.EntityFrameworkCore --version 0.2.2
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.2.1
dotnet add package Stripe.net --version 46.4.0
dotnet add package SendGrid --version 9.29.3
dotnet add package Microsoft.AspNetCore.SignalR.Core --version 9.0.0
dotnet add package QuestPDF --version 2024.12.3
dotnet add package ClosedXML --version 0.104.2
dotnet add package AspNetCore.HealthChecks.Npgsql --version 9.0.0
dotnet add package AspNetCore.HealthChecks.Redis --version 9.0.0
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 9.0.0

# API project
cd ../EventEase.API
dotnet add package HotChocolate.AspNetCore --version 14.1.0
dotnet add package HotChocolate.Data --version 14.1.0
dotnet add package HotChocolate.Data.EntityFramework --version 14.1.0
dotnet add package HotChocolate.Subscriptions.InMemory --version 14.1.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add package QRCoder --version 1.6.0
dotnet add package Swashbuckle.AspNetCore --version 7.2.0

# Test projects
cd ../tests/EventEase.Domain.Tests
dotnet add package xunit --version 2.9.2
dotnet add package xunit.runner.visualstudio --version 2.8.2
dotnet add package FluentAssertions --version 6.12.1
dotnet add package Moq --version 4.20.72
dotnet add package coverlet.collector --version 6.0.2

# Repeat for other test projects
```

---

## Domain Layer Implementation

### Directory Structure

```
EventEase.Domain/
├── Common/
│   ├── BaseEntity.cs
│   ├── BaseAuditableEntity.cs
│   └── ITenantEntity.cs
├── Entities/
│   ├── Tenant.cs
│   ├── User.cs
│   ├── Event.cs
│   ├── EventRegistration.cs
│   ├── Guest.cs
│   ├── Invitation.cs
│   ├── CreditPackage.cs
│   ├── CreditTransaction.cs
│   ├── PaymentTransaction.cs
│   ├── AIAgentUsage.cs
│   ├── Budget.cs
│   ├── BudgetItem.cs
│   ├── Notification.cs
│   └── MobileDevice.cs
├── Enums/
│   ├── UserRole.cs
│   ├── TenantStatus.cs
│   ├── EventStatus.cs
│   ├── RegistrationStatus.cs
│   ├── InvitationStatus.cs
│   ├── PaymentStatus.cs
│   ├── CreditTransactionType.cs
│   ├── CreditPackageType.cs
│   ├── AIAgentType.cs
│   ├── AIProvider.cs
│   ├── EmailTemplate.cs
│   └── NotificationType.cs
└── Interfaces/
    └── (domain interfaces if needed)
```

### Step 1: Create Base Entities

**Common/BaseEntity.cs:**
```csharp
namespace EventEase.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
```

**Common/BaseAuditableEntity.cs:**
```csharp
namespace EventEase.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**Common/ITenantEntity.cs:**
```csharp
namespace EventEase.Domain.Common;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### Step 2: Create All Enums

Copy all 12 enums from the analysis. Each enum should follow this pattern:

**Enums/UserRole.cs:**
```csharp
namespace EventEase.Domain.Enums;

public enum UserRole
{
    SystemAdmin = 0,
    TenantOwner = 1,
    TenantAdmin = 2,
    EventManager = 3,
    User = 4
}
```

### Step 3: Create All Entities

Create all 16 entities with their properties and relationships. Example:

**Entities/Tenant.cs:**
```csharp
using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

public class Tenant : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Contact Information
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string? PrimaryContactPhone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Subscription & Status
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? SubscriptionStartedAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }

    // Credits
    public decimal AvailableCredits { get; set; } = 0;
    public decimal TotalCreditsPurchased { get; set; } = 0;
    public decimal TotalCreditsUsed { get; set; } = 0;

    // Stripe Integration
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    // Settings
    public string? TimeZone { get; set; } = "UTC";
    public string? DefaultCurrency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
```

*Repeat for all 16 entities using the analysis as reference.*

---

## Application Layer Implementation

### Directory Structure

```
EventEase.Application/
├── Common/
│   ├── Result.cs
│   └── AuthenticationResult.cs
├── DTOs/
│   └── (various DTOs)
├── Interfaces/
│   ├── IApplicationDbContext.cs
│   ├── IAuthenticationService.cs
│   ├── ITokenService.cs
│   ├── IPasswordHasher.cs
│   ├── ICurrentUserService.cs
│   ├── ICurrentTenantService.cs
│   ├── IOpenAIService.cs
│   ├── IAnthropicService.cs
│   ├── IDeepSeekService.cs
│   ├── IOllamaService.cs
│   ├── IAIModelRouter.cs
│   ├── ICreditDeductionService.cs
│   ├── IPlanningAgentService.cs
│   ├── IInvitationAgentService.cs
│   ├── IAnalyticsAgentService.cs
│   ├── IBudgetAgentService.cs
│   ├── IIntegrationAgentService.cs
│   ├── IEventAssistantService.cs
│   ├── IMatchmakingService.cs
│   ├── IPredictiveAnalyticsService.cs
│   ├── IContentGenerationService.cs
│   ├── IStripePaymentService.cs
│   ├── IPaymentWebhookService.cs
│   ├── IInvoiceService.cs
│   ├── IEmailService.cs
│   ├── INotificationService.cs
│   ├── IRealtimeService.cs
│   ├── IAnalyticsService.cs
│   └── IReportService.cs
└── Features/
    └── (CQRS commands/queries if using MediatR)
```

### Step 1: Create Common Utilities

**Common/Result.cs:**
```csharp
namespace EventEase.Application.Common;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public string? Error { get; protected set; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value, string? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

**Common/AuthenticationResult.cs:**
```csharp
namespace EventEase.Application.Common;

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthenticationResult SuccessResult(string accessToken, string refreshToken, DateTime expiresAt)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public static AuthenticationResult Failure(string errorMessage)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
```

### Step 2: Create All Service Interfaces

Create all 32 service interfaces. Example:

**Interfaces/IAuthenticationService.cs:**
```csharp
using EventEase.Application.Common;

namespace EventEase.Application.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> RegisterTenantAsync(
        string companyName,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);
}
```

*Create all other interfaces following the same pattern.*

---

## Infrastructure Layer Implementation

### Directory Structure

```
EventEase.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Configurations/
│       ├── UserConfiguration.cs
│       ├── TenantConfiguration.cs
│       ├── EventConfiguration.cs
│       └── (12 total configurations)
├── HealthChecks/
│   ├── StripeHealthCheck.cs
│   ├── SendGridHealthCheck.cs
│   ├── OpenAIHealthCheck.cs
│   └── OllamaHealthCheck.cs
├── Hubs/
│   ├── EventHub.cs
│   ├── NotificationHub.cs
│   └── AnalyticsHub.cs
├── Services/
│   ├── AuthenticationService.cs
│   ├── TokenService.cs
│   ├── PasswordHasher.cs
│   ├── CurrentUserService.cs
│   ├── CurrentTenantService.cs
│   ├── AI/
│   │   ├── OpenAIService.cs
│   │   ├── AnthropicService.cs
│   │   ├── DeepSeekService.cs
│   │   ├── OllamaService.cs
│   │   ├── AIModelRouter.cs
│   │   ├── CreditDeductionService.cs
│   │   ├── EventAssistantService.cs
│   │   ├── MatchmakingService.cs
│   │   ├── PredictiveAnalyticsService.cs
│   │   ├── ContentGenerationService.cs
│   │   └── Agents/
│   │       ├── PlanningAgentService.cs
│   │       ├── InvitationAgentService.cs
│   │       ├── AnalyticsAgentService.cs
│   │       ├── BudgetAgentService.cs
│   │       └── IntegrationAgentService.cs
│   ├── Payment/
│   │   ├── StripePaymentService.cs
│   │   ├── PaymentWebhookService.cs
│   │   └── InvoiceService.cs
│   ├── Notifications/
│   │   ├── SendGridEmailService.cs
│   │   └── NotificationService.cs
│   ├── Realtime/
│   │   └── RealtimeService.cs
│   └── Analytics/
│       ├── AnalyticsService.cs
│       └── ReportService.cs
└── Migrations/
    └── (EF Core migrations)
```

### Step 1: Implement ApplicationDbContext

**Data/ApplicationDbContext.cs** - Copy from analysis with:
- All DbSet properties
- Global query filters for multi-tenancy
- Automatic audit field setting
- Automatic TenantId assignment

### Step 2: Create Entity Configurations

Create 12 entity configuration files. Example:

**Data/Configurations/UserConfiguration.cs:**
```csharp
using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Step 3: Implement Core Services

Implement all 43 services. Key services to implement first:

1. **PasswordHasher** - BCrypt password hashing
2. **TokenService** - JWT token generation
3. **CurrentUserService** - Extract user from HTTP context
4. **CurrentTenantService** - Extract tenant from HTTP context
5. **AuthenticationService** - Full implementation from analysis

### Step 4: Implement AI Services

Implement all 4 AI provider services and the AIModelRouter. Example:

**Services/AI/OpenAIService.cs:**
```csharp
using EventEase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace EventEase.Infrastructure.Services.AI;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiKey = configuration["OpenAI:ApiKey"];
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<AIResponse> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        double? temperature = null,
        int? maxTokens = null)
    {
        // Implementation...
    }
}
```

### Step 5: Implement Payment Services

Implement Stripe integration with webhooks:

**Services/Payment/StripePaymentService.cs:**
```csharp
using EventEase.Application.Interfaces;
using Stripe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.Payment;

public class StripePaymentService : IStripePaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    // Implementation...
}
```

### Step 6: Implement SignalR Hubs

Create 3 SignalR hubs for real-time features:

**Hubs/EventHub.cs:**
```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace EventEase.Infrastructure.Hubs;

[Authorize]
public class EventHub : Hub
{
    public async Task SendEventUpdate(Guid eventId, string message)
    {
        await Clients.Group($"event-{eventId}").SendAsync("ReceiveEventUpdate", message);
    }

    public async Task JoinEventGroup(Guid eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }

    public async Task LeaveEventGroup(Guid eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }
}
```

---

## API Layer Implementation

### Directory Structure

```
EventEase.API/
├── Controllers/
│   ├── AuthenticationController.cs
│   ├── EventsController.cs
│   ├── RegistrationsController.cs
│   ├── GuestsController.cs
│   ├── CreditsController.cs
│   ├── AIAgentsController.cs
│   ├── PaymentsController.cs
│   ├── WebhooksController.cs
│   ├── NotificationsController.cs
│   ├── MobileController.cs
│   ├── AIAssistantController.cs
│   ├── ReportsController.cs
│   ├── AnalyticsController.cs
│   └── DevicesController.cs
├── DTOs/
│   └── (60+ request/response DTOs)
├── GraphQL/
│   ├── Query.cs
│   ├── Mutation.cs
│   ├── Subscription.cs
│   ├── Types/
│   │   ├── EventType.cs
│   │   ├── GuestType.cs
│   │   ├── RegistrationType.cs
│   │   ├── TenantType.cs
│   │   └── UserType.cs
│   └── DataLoaders/
│       ├── EventByIdDataLoader.cs
│       ├── GuestByIdDataLoader.cs
│       ├── RegistrationByIdDataLoader.cs
│       ├── TenantByIdDataLoader.cs
│       ├── UserByIdDataLoader.cs
│       └── RegistrationsByEventIdDataLoader.cs
├── Middleware/
│   └── (custom middleware if needed)
├── Extensions/
│   └── (extension methods)
└── Program.cs
```

### Step 1: Implement Controllers

Create all 14 controllers. Example:

**Controllers/AuthenticationController.cs:**
```csharp
using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventEase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request)
    {
        var result = await _authenticationService.RegisterTenantAsync(
            request.CompanyName,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber
        );

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new AuthenticationResponse
        {
            AccessToken = result.Value!.AccessToken,
            RefreshToken = result.Value.RefreshToken,
            ExpiresAt = result.Value.ExpiresAt
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authenticationService.LoginAsync(
            request.Email,
            request.Password
        );

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.ErrorMessage });

        return Ok(new AuthenticationResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt
        });
    }

    // Implement other endpoints: refresh, logout, forgot-password, reset-password
}
```

### Step 2: Implement GraphQL

**GraphQL/Query.cs:**
```csharp
using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using HotChocolate;
using HotChocolate.Authorization;

namespace EventEase.API.GraphQL;

public class Query
{
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Event> GetEvents([Service] ApplicationDbContext context)
    {
        return context.Events;
    }

    [Authorize]
    [UseProjection]
    public IQueryable<Guest> GetGuests([Service] ApplicationDbContext context)
    {
        return context.Guests;
    }

    // Add more queries...
}
```

### Step 3: Configure Program.cs

Copy the comprehensive `Program.cs` from the analysis, which includes:
- Database configuration (PostgreSQL with retry logic)
- All service registrations (43 services)
- JWT authentication configuration
- Authorization policies (4 policies)
- Rate limiting (4 policies)
- CORS configuration
- GraphQL configuration
- SignalR configuration
- Health checks (4 checks)
- Swagger configuration
- Security headers
- Middleware pipeline

---

## Database Setup & Migrations

### Step 1: Create Initial Migration

```bash
cd EventEase.Infrastructure

# Add EF Core tools
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../EventEase.API

# Review the generated migration in Migrations/ folder
```

### Step 2: Apply Migration to Database

```bash
# Ensure PostgreSQL is running
# Update connection string in appsettings.json

# Apply migration
dotnet ef database update --startup-project ../EventEase.API
```

### Step 3: Seed Data (Optional)

Create a seed data class:

```csharp
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed credit packages
        if (!context.CreditPackages.Any())
        {
            var packages = new List<CreditPackage>
            {
                new CreditPackage
                {
                    Name = "Starter Pack",
                    Type = CreditPackageType.Starter,
                    Price = 99.00m,
                    Currency = "EUR",
                    BaseCredits = 1000,
                    BonusCredits = 0,
                    ValidityDays = 180,
                    IsActive = true,
                    IsVisible = true,
                    DisplayOrder = 1
                },
                new CreditPackage
                {
                    Name = "Pro Pack",
                    Type = CreditPackageType.Pro,
                    Price = 399.00m,
                    Currency = "EUR",
                    BaseCredits = 5000,
                    BonusCredits = 1000,
                    ValidityDays = 365,
                    IsActive = true,
                    IsVisible = true,
                    DisplayOrder = 2,
                    IsFeatured = true,
                    BadgeText = "Most Popular"
                }
            };

            context.CreditPackages.AddRange(packages);
            await context.SaveChangesAsync();
        }
    }
}
```

---

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventease_saas;Username=postgres;Password=your_password_here",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-minimum-32-characters-change-in-production",
    "Issuer": "EventEase",
    "Audience": "EventEaseAPI",
    "ExpiryMinutes": 60
  },
  "Stripe": {
    "SecretKey": "sk_test_your_stripe_secret_key",
    "PublishableKey": "pk_test_your_stripe_publishable_key",
    "WebhookSecret": "whsec_your_webhook_secret"
  },
  "SendGrid": {
    "ApiKey": "your_sendgrid_api_key",
    "FromEmail": "noreply@eventease.com",
    "FromName": "EventEase"
  },
  "OpenAI": {
    "ApiKey": "your_openai_api_key",
    "Model": "gpt-4o"
  },
  "Anthropic": {
    "ApiKey": "your_anthropic_api_key",
    "Model": "claude-3-5-sonnet-20241022"
  },
  "DeepSeek": {
    "ApiKey": "your_deepseek_api_key",
    "BaseUrl": "https://api.deepseek.com/v1"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "http://localhost:8080"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## Testing Implementation

### Step 1: Write Unit Tests

Example domain test:

```csharp
namespace EventEase.Domain.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void Tenant_ShouldStartWithTrialStatus()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Name = "Test Company",
            PrimaryContactEmail = "test@example.com"
        };

        // Assert
        tenant.Status.Should().Be(TenantStatus.Trial);
        tenant.AvailableCredits.Should().Be(0);
    }

    [Fact]
    public void Tenant_ShouldCalculateRemainingCredits()
    {
        // Arrange
        var tenant = new Tenant
        {
            AvailableCredits = 100,
            TotalCreditsUsed = 30
        };

        // Act & Assert
        tenant.AvailableCredits.Should().Be(100);
    }
}
```

### Step 2: Write Integration Tests

Example infrastructure test:

```csharp
public class AuthenticationServiceTests : IAsyncLifetime
{
    private ApplicationDbContext _context;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RegisterTenant_ShouldCreateTenantAndOwner()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var tokenService = new TokenService(/* config */);
        var logger = new Mock<ILogger<AuthenticationService>>().Object;

        var service = new AuthenticationService(
            _context,
            passwordHasher,
            tokenService,
            logger
        );

        // Act
        var result = await service.RegisterTenantAsync(
            "Test Company",
            "test@example.com",
            "Password123",
            "John",
            "Doe"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.Tenants.Should().HaveCount(1);
        _context.Users.Should().HaveCount(1);
        _context.CreditTransactions.Should().HaveCount(1);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

---

## Deployment

### Step 1: Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EventEase.API/EventEase.API.csproj", "EventEase.API/"]
COPY ["EventEase.Infrastructure/EventEase.Infrastructure.csproj", "EventEase.Infrastructure/"]
COPY ["EventEase.Application/EventEase.Application.csproj", "EventEase.Application/"]
COPY ["EventEase.Domain/EventEase.Domain.csproj", "EventEase.Domain/"]
RUN dotnet restore "EventEase.API/EventEase.API.csproj"
COPY . .
WORKDIR "/src/EventEase.API"
RUN dotnet build "EventEase.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventEase.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventEase.API.dll"]
```

### Step 2: Create docker-compose.yml

```yaml
version: '3.8'

services:
  postgres:
    image: ankane/pgvector:latest
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: your_password_here
      POSTGRES_DB: eventease_saas
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: EventEase.API/Dockerfile
    ports:
      - "5000:80"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=eventease_saas;Username=postgres;Password=your_password_here
      - ConnectionStrings__Redis=redis:6379
      - JwtSettings__Secret=your-super-secret-key-minimum-32-characters
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_data:
```

### Step 3: Kubernetes Deployment (Optional)

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eventease-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: eventease-api
  template:
    metadata:
      labels:
        app: eventease-api
    spec:
      containers:
      - name: api
        image: eventease-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: eventease-secrets
              key: db-connection-string
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: eventease-api-service
spec:
  selector:
    app: eventease-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: LoadBalancer
```

---

## Verification & Testing

### Step 1: Start the Application

```bash
# Using dotnet run
cd EventEase.API
dotnet run

# Or using Docker Compose
docker-compose up -d

# Check logs
docker-compose logs -f api
```

### Step 2: Test Health Endpoints

```bash
# Liveness probe
curl http://localhost:5000/health/live

# Readiness probe
curl http://localhost:5000/health/ready

# Comprehensive health check
curl http://localhost:5000/health | jq
```

### Step 3: Test Authentication

```bash
# Register a new tenant
curl -X POST http://localhost:5000/api/authentication/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Test Company",
    "email": "test@example.com",
    "password": "Password123",
    "firstName": "John",
    "lastName": "Doe"
  }'

# Login
curl -X POST http://localhost:5000/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123"
  }'
```

### Step 4: Test with Swagger

Navigate to `http://localhost:5000` (in development) to access Swagger UI and test all endpoints interactively.

### Step 5: Test GraphQL

Navigate to `http://localhost:5000/graphql` to access Banana Cake Pop UI.

Example query:
```graphql
query {
  events {
    id
    name
    description
    startDate
    endDate
  }
}
```

### Step 6: Test SignalR

Use a SignalR client to connect to:
- `/hubs/events`
- `/hubs/notifications`
- `/hubs/analytics`

---

## Common Issues & Troubleshooting

### Issue: Database connection fails

**Solution:**
```bash
# Verify PostgreSQL is running
psql -U postgres -h localhost

# Check connection string in appsettings.json
# Ensure password matches
```

### Issue: Redis connection fails

**Solution:**
```bash
# Verify Redis is running
redis-cli ping

# Check Redis connection string
```

### Issue: JWT token validation fails

**Solution:**
- Ensure `JwtSettings:Secret` is at least 32 characters
- Verify issuer and audience match in configuration
- Check token expiration

### Issue: Migration fails

**Solution:**
```bash
# Drop database and recreate
dotnet ef database drop --force --startup-project ../EventEase.API
dotnet ef database update --startup-project ../EventEase.API

# Or delete migrations and start fresh
rm -rf Migrations/
dotnet ef migrations add InitialCreate --startup-project ../EventEase.API
```

---

## Next Steps After Recreation

1. **Frontend Development** - Create a React/Vue/Angular frontend
2. **Mobile Apps** - Build iOS/Android apps
3. **CI/CD Pipeline** - Setup GitHub Actions or Azure DevOps
4. **Monitoring** - Add Application Insights or Datadog
5. **Load Testing** - Use k6 or JMeter
6. **Security Audit** - Conduct penetration testing
7. **Documentation** - Generate API docs with Swagger/OpenAPI
8. **Localization** - Add multi-language support
9. **Email Templates** - Design HTML email templates
10. **Analytics Dashboard** - Build real-time analytics UI

---

## Estimated Timeline

| Phase | Duration | Description |
|-------|----------|-------------|
| Setup | 1 day | Environment, tools, accounts |
| Domain | 1 day | Entities, enums, interfaces |
| Application | 1.5 days | Service interfaces, DTOs |
| Infrastructure - Data | 2 days | DbContext, configurations, migrations |
| Infrastructure - Auth | 1.5 days | Authentication, authorization |
| Infrastructure - AI | 4 days | AI providers, agents, routing |
| Infrastructure - External | 2.5 days | Stripe, SendGrid, analytics |
| Infrastructure - Realtime | 1.5 days | SignalR hubs |
| API - Controllers | 5 days | All controllers and DTOs |
| API - GraphQL | 3 days | Schema, resolvers, data loaders |
| API - Configuration | 1.5 days | Program.cs, middleware |
| Testing | 5 days | Unit, integration, API tests |
| Deployment | 2 days | Docker, K8s, CI/CD |
| Documentation | 2 days | README, guides, OpenAPI |
| **Total** | **35 days** | **7 weeks for 1 developer** |

---

## Success Criteria

You've successfully recreated EventEaseApp when:

- [ ] All 4 layers are implemented
- [ ] All 16 entities are created with relationships
- [ ] All 32 service interfaces are implemented
- [ ] Multi-tenancy isolation works (users can't access other tenant data)
- [ ] Authentication works (register, login, refresh, password reset)
- [ ] At least 1 AI provider is integrated
- [ ] Credit system works (deduction, balance tracking)
- [ ] Database migrations run without errors
- [ ] Health checks return healthy status
- [ ] At least 10 API endpoints work
- [ ] GraphQL queries work
- [ ] SignalR connections work
- [ ] Tests pass (at least 80% coverage)
- [ ] Application runs in Docker
- [ ] Swagger UI is accessible
- [ ] Can create a tenant, event, and registration

---

## Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Clean Architecture by Jason Taylor](https://github.com/jasontaylordev/CleanArchitecture)
- [Stripe .NET SDK](https://github.com/stripe/stripe-dotnet)
- [HotChocolate GraphQL Documentation](https://chillicream.com/docs/hotchocolate/v13)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Multi-tenancy in ASP.NET Core](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)

---

**Good luck with your recreation! This is an enterprise-grade application with advanced features. Take it one step at a time, and don't hesitate to consult the original codebase for reference.**
