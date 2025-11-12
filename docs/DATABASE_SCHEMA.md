# EventEase SaaS - Database Schema Documentation

## Overview

This document describes the complete database schema for the EventEase multi-tenant SaaS platform with AI agents and credit-based pricing.

## Technology Stack

- **Database**: PostgreSQL 15 with pgvector extension
- **ORM**: Entity Framework Core 9.0
- **Multi-tenancy**: Row-Level Security (RLS) with tenant isolation
- **Architecture**: Clean Architecture with Domain-Driven Design

## Multi-Tenancy Strategy

### Tenant Isolation
- **Approach**: Shared database with tenant column (TenantId) in all tenant-specific tables
- **Implementation**: Global query filters in EF Core automatically filter by TenantId
- **Security**: Row-Level Security policies in PostgreSQL for defense-in-depth

### ITenantEntity Interface
All tenant-scoped entities implement `ITenantEntity`:
```csharp
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

## Core Entities

### 1. Tenant
**Purpose**: Represents a company/organization in the multi-tenant system

**Key Fields**:
- `Id` (Guid, PK)
- `Name`, `CompanyRegistrationNumber`, `VatNumber`
- `PrimaryContactEmail`, `PrimaryContactPhone`
- `Status` (Trial, Active, Suspended, Cancelled, Expired)
- `AvailableCredits`, `TotalCreditsPurchased`, `TotalCreditsUsed`
- `StripeCustomerId`, `StripeSubscriptionId`

**Indexes**:
- `PrimaryContactEmail`
- `StripeCustomerId`
- `Status`, `IsActive`, `CreatedAt`

**Relationships**:
- One-to-Many: Users, Events, CreditTransactions, PaymentTransactions

---

### 2. User
**Purpose**: Represents a user within a tenant

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId` (Guid, FK) - Multi-tenancy
- `Email`, `PasswordHash`, `PasswordSalt`
- `FirstName`, `LastName`, `PhoneNumber`
- `Role` (SystemAdmin, TenantOwner, TenantAdmin, EventManager, User)
- `IsActive`, `EmailConfirmed`, `LastLoginAt`
- `RefreshToken`, `PasswordResetToken`

**Indexes**:
- `Email`
- **Unique**: `(TenantId, Email)` - Email unique per tenant
- `Role`, `IsActive`, `TenantId`

**Relationships**:
- Many-to-One: Tenant
- One-to-Many: CreatedEvents, Registrations, AIAgentUsages

---

### 3. Event
**Purpose**: Represents an event created by a tenant

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId` (Guid, FK)
- `CreatedByUserId` (Guid, FK)
- `Name`, `Description`, `Category`
- `Status` (Draft, Published, Full, Cancelled, Completed, Postponed)
- `StartDate`, `EndDate`, `TimeZone`
- `Location`, `Venue`, `VenueAddress` (+ lat/long)
- `IsVirtual`, `VirtualMeetingUrl`
- `MaxAttendees`, `Price`, `Currency`, `IsFree`
- `IsAIGenerated`, `AIGenerationPrompt`, `AIAgentUsageId`

**Indexes**:
- `TenantId`, `CreatedByUserId`, `Status`, `StartDate`, `Category`
- **Composite**: `(TenantId, StartDate)`, `(TenantId, Status)`

**Relationships**:
- Many-to-One: Tenant, CreatedByUser
- One-to-Many: Registrations, Invitations
- One-to-One: Budget

---

### 4. EventRegistration
**Purpose**: Tracks registrations/attendees for events

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `EventId`, `UserId` (nullable for external guests)
- `FirstName`, `LastName`, `Email`, `PhoneNumber`, `Company`
- `Status` (Pending, Confirmed, Cancelled, CheckedIn, NoShow, Waitlisted)
- `NumberOfTickets`, `TotalAmount`
- `RegisteredAt`, `ConfirmedAt`, `CancelledAt`, `CheckedInAt`
- `InvitationId`, `RegistrationSource`

**Indexes**:
- `TenantId`, `EventId`, `UserId`, `Email`, `Status`, `RegisteredAt`
- **Composite**: `(EventId, Status)`, `(EventId, Email)`

---

### 5. Guest
**Purpose**: Contact database for invitation purposes with ML features

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId` (Guid, FK)
- `Email`, `FirstName`, `LastName`, `PhoneNumber`
- `Company`, `JobTitle`, `Industry`, `Department`
- `City`, `Country`
- **ML Features**:
  - `Interests` (JSON array)
  - `PreviousEventCategories` (JSON array)
  - `TotalEventsAttended`, `TotalInvitationsSent`, `TotalInvitationsAccepted`
  - `EngagementScore` (ML-calculated)
  - `LastInvitationSentAt`, `LastEventAttendedAt`
- `OptedOutOfMarketing`
- `ExternalCrmId` (for CRM integrations)

**Indexes**:
- **Unique**: `(TenantId, Email)`
- `EngagementScore`, `LastInvitationSentAt`

**Relationships**:
- Many-to-One: Tenant
- One-to-Many: Invitations

---

### 6. Invitation
**Purpose**: Tracks invitations sent to guests with AI personalization

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `EventId`, `GuestId`, `SentByUserId`
- `Status` (Draft, Queued, Sent, Opened, Clicked, Accepted, Declined, Bounced)
- `Subject`, `MessageBody`
- `IsAIGenerated`, `AIAgentUsageId`
- `ScheduledSendAt` (AI-optimized send time)
- `SentAt`, `OpenedAt`, `ClickedAt`, `RespondedAt`
- `OpenCount`, `ClickCount`
- `TrackingToken` (unique for tracking)
- `SendGridMessageId`, `SendGridStatus`

**Indexes**:
- `TenantId`, `EventId`, `GuestId`, `Status`
- **Unique**: `TrackingToken`
- `ScheduledSendAt`, `SentAt`
- **Composite**: `(EventId, Status)`

---

## Credit & Payment Entities

### 7. CreditPackage
**Purpose**: Defines available credit packages for purchase

**Key Fields**:
- `Id` (Guid, PK)
- `Name`, `Description`
- `Type` (Starter, Pro, Enterprise, PayAsYouGo)
- `Price`, `Currency`
- `BaseCredits`, `BonusCredits`, `TotalCredits` (computed)
- `ValidityDays`
- `StripePriceId`, `StripeProductId`
- `IsActive`, `IsVisible`, `DisplayOrder`
- `IsFeatured`, `BadgeText`

**Seed Data** (Default Packages):
1. **Starter**: €99 = 1,000 credits (6 months)
2. **Pro**: €399 = 6,000 credits (5,000 + 1,000 bonus) (12 months) - Most Popular
3. **Enterprise**: Custom pricing, 20,000+ credits
4. **Pay-as-you-go**: €0.15/credit

**Indexes**:
- **Unique**: `Type`
- `IsActive`, `IsVisible`, `DisplayOrder`

---

### 8. CreditTransaction
**Purpose**: Complete audit trail of all credit movements

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `UserId`
- `Type` (Purchase, Deduction, Refund, Bonus, Expiration, Adjustment)
- `Amount` (positive for additions, negative for deductions)
- `BalanceBefore`, `BalanceAfter`
- `Description`, `Notes`
- `AIAgentUsageId`, `PaymentTransactionId`, `CreditPackageId`
- `ExpiresAt`, `IsExpired`

**Indexes**:
- `TenantId`, `UserId`, `Type`, `CreatedAt`
- **Composite**: `(TenantId, CreatedAt)`
- `ExpiresAt`, `IsExpired`

**Relationships**:
- Many-to-One: Tenant, User, CreditPackage
- One-to-One: AIAgentUsage, PaymentTransaction

---

### 9. PaymentTransaction
**Purpose**: Tracks all Stripe payment webhooks

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId` (Guid, FK)
- `StripePaymentIntentId`, `StripeChargeId`, `StripeInvoiceId`
- `Amount`, `Currency`, `RefundedAmount`
- `Status` (Pending, Succeeded, Failed, Refunded, Disputed, Cancelled)
- `FailureReason`
- `CreditPackageId`, `CreditTransactionId`
- `PaymentMethod`, `CardBrand`, `CardLast4`
- `PaidAt`, `RefundedAt`, `DisputedAt`
- `WebhookEventId`, `WebhookEventType`
- `RawWebhookData` (JSONB - full Stripe payload)
- `ReceiptUrl`, `InvoiceUrl`

**Indexes**:
- **Unique**: `StripePaymentIntentId`
- `TenantId`, `Status`, `CreatedAt`
- **Composite**: `(TenantId, Status)`

---

## AI Agent Entities

### 10. AIAgentUsage
**Purpose**: Detailed audit trail for all AI agent operations

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `UserId`
- `AgentType` (Planning, Invitation, Analytics, Budget, Integration)
- `Provider` (OpenAI, Anthropic)
- `ModelUsed` ("gpt-4", "claude-3-5-sonnet-20241022", etc.)
- **Request/Response**:
  - `PromptInput` (text - user input)
  - `SystemPrompt` (text - system instructions)
  - `AgentResponse` (text - AI output)
- **Cost Tracking**:
  - `CreditsCost` (charged to tenant)
  - `InputTokens`, `OutputTokens`, `TotalTokens`
  - `ProviderCost` (actual cost from AI provider for analytics)
- **Performance**:
  - `ResponseTimeMs`
  - `IsSuccess`, `ErrorMessage`, `RetryCount`
- `EventId` (if related to an event)
- `CreditTransactionId`

**Indexes**:
- `TenantId`, `UserId`, `AgentType`, `Provider`, `CreatedAt`
- **Composite**: `(TenantId, AgentType, CreatedAt)`
- `IsSuccess`

**Credit Costs (per operation)**:
- AI event creation: 15 credits
- AI guest recommendations: 10 credits
- AI-personalized invites: 0.5 credits per guest
- Attendance prediction: 5 credits
- AI insights/analytics: 20-30 credits

---

## Budget Management Entities

### 11. Budget
**Purpose**: Event budget tracking (managed by Budget Agent)

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `EventId`
- `TotalBudget`, `AllocatedAmount`, `SpentAmount`
- `RemainingAmount` (computed: TotalBudget - SpentAmount)
- `Currency`
- `IsAIGenerated`, `AIAgentUsageId`
- `IsApproved`, `ApprovedByUserId`, `ApprovedAt`
- `AIRecommendations`

**Indexes**:
- **Unique**: `EventId` (one budget per event)
- `TenantId`, `IsApproved`

**Relationships**:
- One-to-One: Event
- One-to-Many: BudgetItems

---

### 12. BudgetItem
**Purpose**: Line items in event budgets

**Key Fields**:
- `Id` (Guid, PK)
- `TenantId`, `BudgetId`
- `Category` ("Venue", "Catering", "Marketing", etc.)
- `Name`, `Description`
- `EstimatedCost`, `ActualCost`
- `Variance` (computed: ActualCost - EstimatedCost)
- `VendorName`, `VendorContact`, `VendorEmail`, `VendorPhone`
- `IsPaid`, `PaidAt`, `PaymentReference`
- **AI Features**:
  - `IsAIRecommended`, `AIRecommendationReason`
  - `AlternativeVendors` (JSON array)
- `InvoiceUrl`, `ReceiptUrl`, `ContractUrl`

**Indexes**:
- `TenantId`, `BudgetId`, `Category`, `IsPaid`

---

## Enums

### UserRole
- `SystemAdmin` (0) - Platform super admin
- `TenantOwner` (1) - Company owner, manages billing
- `TenantAdmin` (2) - Manages users and events
- `EventManager` (3) - Creates and manages events
- `User` (4) - Basic access, can register for events

### TenantStatus
- `Trial` (0)
- `Active` (1)
- `Suspended` (2)
- `Cancelled` (3)
- `Expired` (4)

### AIAgentType
- `Planning` (0) - Event creation, venue recommendations, timeline generation
- `Invitation` (1) - Guest selection, personalized emails, send time optimization
- `Analytics` (2) - Attendance prediction, sentiment analysis, ROI calculation
- `Budget` (3) - Cost estimation, vendor comparison, expense tracking
- `Integration` (4) - Calendar sync, CRM integration, notifications

### EventStatus
- `Draft` (0)
- `Published` (1)
- `Full` (2)
- `Cancelled` (3)
- `Completed` (4)
- `Postponed` (5)

### RegistrationStatus
- `Pending` (0)
- `Confirmed` (1)
- `Cancelled` (2)
- `CheckedIn` (3)
- `NoShow` (4)
- `Waitlisted` (5)

### InvitationStatus
- `Draft` (0)
- `Queued` (1)
- `Sent` (2)
- `Opened` (3)
- `Clicked` (4)
- `Accepted` (5)
- `Declined` (6)
- `Bounced` (7)

---

## PostgreSQL Row-Level Security (RLS)

### Purpose
Defense-in-depth security ensuring tenant isolation at the database level.

### Implementation (To be added via migrations)
```sql
-- Enable RLS on tenant-scoped tables
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE events ENABLE ROW LEVEL SECURITY;
ALTER TABLE event_registrations ENABLE ROW LEVEL SECURITY;
-- ... (all tables with TenantId)

-- Create policy for tenant isolation
CREATE POLICY tenant_isolation_policy ON users
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

-- Similar policies for all tenant-scoped tables
```

### Setting Tenant Context
```csharp
// Set tenant context before queries
await _dbContext.Database.ExecuteSqlRawAsync(
    "SET LOCAL app.current_tenant_id = {0}",
    tenantId
);
```

---

## Indexes & Performance

### Indexing Strategy
1. **Primary Keys**: All entities have Guid PK with clustered index
2. **Foreign Keys**: Indexed for join performance
3. **Query Filters**: Indexed on commonly filtered columns (Status, IsActive, CreatedAt)
4. **Composite Indexes**: For common multi-column queries
5. **Unique Constraints**: Email uniqueness per tenant, tracking tokens, etc.

### Key Composite Indexes
- `(TenantId, Email)` on Users
- `(TenantId, StartDate)` on Events
- `(TenantId, AgentType, CreatedAt)` on AIAgentUsages
- `(EventId, Status)` on Registrations

---

## Data Types

### Precision Fields
- **Monetary**: `decimal(18, 2)` for amounts and prices
- **Coordinates**: `decimal(10, 7)` for latitude/longitude
- **ML Scores**: `decimal(5, 2)` for engagement scores

### Text Fields
- **Short strings**: `nvarchar(100-500)` with explicit max length
- **Long text**: `text` for descriptions, prompts, responses
- **JSON data**: `jsonb` (PostgreSQL) for structured metadata

---

## Audit Trail

### Base Entities
All entities inherit from `BaseAuditableEntity`:
- `CreatedAt` (DateTime UTC)
- `CreatedBy` (string - UserId)
- `UpdatedAt` (DateTime UTC, nullable)
- `UpdatedBy` (string - UserId, nullable)

### Automatic Tracking
Audit fields are automatically set in `ApplicationDbContext.SaveChangesAsync()`.

---

## Migration Strategy

### Local Development
```bash
# Add migration
dotnet ef migrations add InitialCreate --project EventEase.Infrastructure --startup-project EventEase.API

# Update database
dotnet ef database update --project EventEase.Infrastructure --startup-project EventEase.API
```

### Production Deployment
1. Generate migration scripts
2. Review SQL for performance impact
3. Apply via CI/CD pipeline with rollback plan
4. Monitor for lock timeouts on large tables

---

## Testing Requirements

See [TEST_REQUIREMENTS.md](./TEST_REQUIREMENTS.md) for comprehensive testing strategy.

---

## Future Enhancements

1. **pgvector Extension**: For semantic search and AI embeddings
2. **Partitioning**: Partition large tables (AIAgentUsages, CreditTransactions) by date
3. **Read Replicas**: For analytics and reporting queries
4. **Caching**: Redis for frequently accessed tenant data
5. **Event Sourcing**: For critical audit requirements
