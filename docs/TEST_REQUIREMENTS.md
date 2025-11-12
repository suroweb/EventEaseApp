# EventEase SaaS - Testing Requirements

## Overview

This document outlines the testing strategy for the EventEase multi-tenant SaaS platform. Tests should be written after MVP completion, but this document serves as a comprehensive guide for what needs to be tested.

---

## Testing Pyramid

```
                    /\
                   /  \
                  / E2E \
                 /--------\
                /   API    \
               /  Integration\
              /----------------\
             /    Unit Tests    \
            /____________________\
```

**Distribution**:
- **Unit Tests**: 70% (Fast, isolated, focused)
- **Integration Tests**: 20% (Database, services)
- **E2E Tests**: 10% (Critical user journeys)

---

## 1. Unit Tests

### Domain Layer Tests

#### **Entities**
- ✅ Test entity construction and default values
- ✅ Test computed properties (e.g., `Budget.RemainingAmount`, `CreditPackage.TotalCredits`)
- ✅ Test navigation property initialization
- ✅ Validate required fields throw appropriate exceptions

**Example Test Cases**:
```csharp
// CreditPackage
- Should_Calculate_TotalCredits_Correctly()
- Should_Have_Default_Values_On_Construction()

// Budget
- Should_Calculate_RemainingAmount_Correctly()
- Should_Update_RemainingAmount_When_SpentAmount_Changes()

// Event
- Should_Not_Allow_EndDate_Before_StartDate()
- Should_Have_Default_Status_Draft()
```

#### **Enums**
- ✅ Test enum values are correctly defined
- ✅ Test enum-to-string conversions

---

### Application Layer Tests

#### **CQRS Commands & Queries**
Test all MediatR handlers in isolation with mocked dependencies.

**Commands to Test**:
- `CreateEventCommand`
- `RegisterForEventCommand`
- `PurchaseCreditsCommand`
- `CreateInvitationCommand`
- `UseAIAgentCommand`

**Queries to Test**:
- `GetEventByIdQuery`
- `GetUpcomingEventsQuery`
- `GetCreditBalanceQuery`
- `GetAIAgentUsageHistoryQuery`

**Test Cases**:
```csharp
// CreateEventCommand
- Should_Create_Event_With_Valid_Data()
- Should_Set_TenantId_From_Current_Context()
- Should_Fail_When_User_Not_Authorized()
- Should_Fail_When_MaxAttendees_Is_Negative()

// PurchaseCreditsCommand
- Should_Add_Credits_To_Tenant_Balance()
- Should_Create_CreditTransaction_Record()
- Should_Set_Expiration_Date_Based_On_Package()
- Should_Fail_When_Payment_Fails()
```

#### **Validators (FluentValidation)**
- ✅ Test all validation rules
- ✅ Test edge cases (null, empty, max length)
- ✅ Test business rule validations

**Example Validators**:
```csharp
// CreateEventCommandValidator
- Should_Require_Name()
- Should_Require_StartDate_In_Future()
- Should_Require_MaxAttendees_Greater_Than_Zero()
- Should_Validate_Email_Format_For_Contact()

// RegisterForEventCommandValidator
- Should_Prevent_Duplicate_Registration()
- Should_Validate_Event_Not_Full()
- Should_Validate_Registration_Within_Open_Period()
```

---

## 2. Integration Tests

### Infrastructure Layer Tests

#### **Database Tests (ApplicationDbContext)**

**Multi-Tenancy Tests**:
```csharp
- Should_Filter_Entities_By_TenantId()
- Should_Not_Return_Other_Tenant_Data()
- Should_Automatically_Set_TenantId_On_Insert()
- Should_Throw_When_Accessing_Wrong_Tenant_Data()
```

**Audit Trail Tests**:
```csharp
- Should_Set_CreatedAt_On_Insert()
- Should_Set_CreatedBy_From_Current_User()
- Should_Set_UpdatedAt_On_Update()
- Should_Set_UpdatedBy_From_Current_User()
```

**Entity Configuration Tests**:
```csharp
- Should_Enforce_Unique_Constraint_On_Tenant_Email()
- Should_Enforce_Unique_Constraint_On_User_Email_Per_Tenant()
- Should_Have_Correct_Indexes_On_Critical_Columns()
- Should_Cascade_Delete_Event_Registrations()
- Should_Restrict_Delete_On_Tenant_Users()
```

**Relationship Tests**:
```csharp
- Should_Load_Event_With_Registrations()
- Should_Load_Tenant_With_Users()
- Should_Load_Budget_With_BudgetItems()
- Should_Correctly_Track_CreditTransaction_To_AIAgentUsage()
```

---

#### **External Service Integration Tests**

**Stripe Integration**:
```csharp
// Use Stripe Test Mode
- Should_Create_Payment_Intent_Successfully()
- Should_Handle_Successful_Payment_Webhook()
- Should_Handle_Failed_Payment_Webhook()
- Should_Handle_Refund_Webhook()
- Should_Update_CreditTransaction_On_Payment_Success()
```

**SendGrid Integration**:
```csharp
// Use SendGrid Test API Key
- Should_Send_Email_Successfully()
- Should_Track_Email_Delivery_Status()
- Should_Handle_Bounced_Email()
- Should_Update_Invitation_Status_On_Open()
```

**OpenAI Integration**:
```csharp
// Use OpenAI Test API or Mocks
- Should_Generate_Event_Description()
- Should_Generate_Personalized_Invitation()
- Should_Track_Token_Usage()
- Should_Calculate_Credits_Correctly()
- Should_Handle_API_Rate_Limits()
- Should_Retry_On_Transient_Errors()
```

**Anthropic Claude Integration**:
```csharp
// Use Anthropic Test API or Mocks
- Should_Generate_Budget_Recommendations()
- Should_Calculate_Cost_Estimation()
- Should_Track_Token_Usage()
- Should_Handle_API_Errors_Gracefully()
```

---

### Repository Pattern Tests
```csharp
// If using Repository pattern
- Should_Add_Entity_To_Database()
- Should_Update_Existing_Entity()
- Should_Delete_Entity_By_Id()
- Should_Get_Entity_By_Id()
- Should_Get_Paginated_Results()
- Should_Apply_Tenant_Filter_Automatically()
```

---

## 3. API Integration Tests

### Authentication & Authorization Tests

**JWT Authentication**:
```csharp
- Should_Reject_Request_Without_Token()
- Should_Accept_Valid_JWT_Token()
- Should_Reject_Expired_Token()
- Should_Reject_Invalid_Signature()
- Should_Extract_TenantId_From_Token()
- Should_Extract_UserId_From_Token()
```

**Role-Based Authorization**:
```csharp
- Should_Allow_SystemAdmin_Access_All_Tenants()
- Should_Allow_TenantOwner_Manage_Billing()
- Should_Allow_TenantAdmin_Manage_Users()
- Should_Allow_EventManager_Create_Events()
- Should_Restrict_User_To_Read_Only()
```

---

### API Endpoint Tests

#### **Event Management**
```csharp
POST /api/events
- Should_Create_Event_With_201_Status()
- Should_Return_400_For_Invalid_Data()
- Should_Return_401_For_Unauthenticated()
- Should_Return_403_For_Unauthorized_Role()

GET /api/events/{id}
- Should_Return_Event_With_200_Status()
- Should_Return_404_For_NonExistent_Event()
- Should_Not_Return_Other_Tenant_Event()

GET /api/events
- Should_Return_Paginated_Events()
- Should_Filter_By_Category()
- Should_Filter_By_Date_Range()
- Should_Sort_By_StartDate()

PUT /api/events/{id}
- Should_Update_Event_With_200_Status()
- Should_Return_404_For_NonExistent_Event()
- Should_Not_Allow_Update_Other_Tenant_Event()

DELETE /api/events/{id}
- Should_Delete_Event_With_204_Status()
- Should_Cascade_Delete_Registrations()
```

#### **Registration Management**
```csharp
POST /api/events/{eventId}/registrations
- Should_Register_For_Event_With_201()
- Should_Prevent_Duplicate_Registration()
- Should_Add_To_Waitlist_When_Full()
- Should_Deduct_Payment_If_Paid_Event()

GET /api/my-registrations
- Should_Return_User_Registrations()
- Should_Include_Event_Details()
```

#### **Credit & Payment Management**
```csharp
POST /api/credits/purchase
- Should_Create_Stripe_Payment_Intent()
- Should_Return_Client_Secret()
- Should_Validate_Credit_Package_Exists()

POST /api/webhooks/stripe
- Should_Process_Payment_Success()
- Should_Add_Credits_To_Tenant()
- Should_Create_CreditTransaction()
- Should_Verify_Webhook_Signature()

GET /api/credits/balance
- Should_Return_Available_Credits()
- Should_Return_Credits_Used_Today()
```

#### **AI Agent Management**
```csharp
POST /api/ai/planning/create-event
- Should_Generate_Event_From_Prompt()
- Should_Deduct_15_Credits()
- Should_Create_AIAgentUsage_Record()
- Should_Return_409_If_Insufficient_Credits()

POST /api/ai/invitation/generate
- Should_Generate_Personalized_Invitations()
- Should_Deduct_0_5_Credits_Per_Guest()
- Should_Track_Token_Usage()

POST /api/ai/analytics/predict-attendance
- Should_Return_Attendance_Prediction()
- Should_Deduct_5_Credits()
- Should_Provide_Confidence_Score()
```

---

## 4. End-to-End (E2E) Tests

### Critical User Journeys

#### **Journey 1: Tenant Onboarding**
```gherkin
Given a new company wants to use EventEase
When they sign up with company details
And they purchase the "Starter" credit package (€99)
Then their account should be created
And they should have 1,000 credits
And they should receive a welcome email
```

#### **Journey 2: AI-Powered Event Creation**
```gherkin
Given a TenantAdmin is logged in
And has sufficient credits (15+)
When they use the Planning Agent to create an event
And provide a natural language prompt
Then the AI should generate event details
And 15 credits should be deducted
And the event should be created in Draft status
```

#### **Journey 3: Smart Guest Invitation**
```gherkin
Given an Event Manager has created an event
And has imported 100 guests
When they use the Invitation Agent for smart guest selection
Then the AI should recommend top 50 guests based on engagement score
When they generate personalized invitations
Then 25 credits should be deducted (0.5 × 50)
And invitations should be queued with optimized send times
```

#### **Journey 4: Event Registration Flow**
```gherkin
Given a guest receives an invitation email
When they click the registration link
And fill out the registration form
Then they should be registered for the event
And the invitation status should be "Accepted"
And they should receive a confirmation email
```

#### **Journey 5: Credit Expiration & Renewal**
```gherkin
Given a tenant purchased "Starter" package 6 months ago
When the credits expire
Then unused credits should be marked as expired
And a CreditTransaction of type "Expiration" should be created
And the tenant should receive an expiration notice email
```

---

## 5. Performance Tests

### Load Testing

**Scenarios**:
```csharp
// Simulate 1000 concurrent users
- Should_Handle_1000_Concurrent_Event_Listings()
- Should_Handle_500_Concurrent_Registrations()
- Should_Process_100_AI_Requests_Per_Minute()
- Should_Handle_10000_Database_Queries_Per_Second()
```

**Metrics**:
- Response time < 200ms (p50)
- Response time < 500ms (p95)
- Response time < 1000ms (p99)
- 0% error rate under normal load

---

### Database Performance Tests

```csharp
// Tenant isolation performance
- Should_Query_10000_Events_In_Under_100ms()
- Should_Apply_Tenant_Filter_Without_Full_Scan()

// Index effectiveness
- Should_Use_Index_For_Email_Lookup()
- Should_Use_Index_For_Date_Range_Queries()
- Should_Use_Composite_Index_For_Tenant_Queries()
```

---

## 6. Security Tests

### Multi-Tenancy Security
```csharp
- Should_Prevent_Cross_Tenant_Data_Access()
- Should_Prevent_Tenant_Id_Manipulation_In_API()
- Should_Validate_Tenant_Context_On_Every_Request()
- Should_Log_Security_Violations()
```

### Authentication Security
```csharp
- Should_Hash_Passwords_With_BCrypt()
- Should_Enforce_Password_Strength_Policy()
- Should_Lock_Account_After_5_Failed_Attempts()
- Should_Require_Email_Confirmation_Before_Login()
```

### SQL Injection Tests
```csharp
- Should_Prevent_SQL_Injection_In_Event_Search()
- Should_Prevent_SQL_Injection_In_Guest_Filter()
- Should_Use_Parameterized_Queries()
```

### XSS Protection
```csharp
- Should_Sanitize_User_Input_In_Event_Description()
- Should_Encode_HTML_In_Invitation_Content()
- Should_Reject_Script_Tags_In_User_Input()
```

---

## 7. Testing Tools & Frameworks

### Recommended Stack

**Unit & Integration Tests**:
- **xUnit** - Test framework
- **FluentAssertions** - Readable assertions
- **Moq** - Mocking framework
- **AutoFixture** - Test data generation
- **Testcontainers** - Dockerized PostgreSQL for integration tests

**API Tests**:
- **WebApplicationFactory** - In-memory API testing
- **FluentValidation.TestHelper** - Validator testing
- **RestSharp** - HTTP client for E2E tests

**E2E Tests**:
- **Playwright** - Browser automation
- **SpecFlow** - BDD framework (Gherkin)

**Performance Tests**:
- **BenchmarkDotNet** - .NET benchmarking
- **k6** or **JMeter** - Load testing

**Code Coverage**:
- **Coverlet** - Coverage collection
- **ReportGenerator** - Coverage reports
- **Target**: >80% code coverage

---

## 8. Test Data Management

### Test Data Builders
```csharp
public class TenantBuilder
{
    public Tenant Build()
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            PrimaryContactEmail = "test@example.com",
            Status = TenantStatus.Active,
            AvailableCredits = 1000
        };
    }

    public TenantBuilder WithCredits(decimal credits)
    {
        // Fluent API
    }
}
```

### Database Seeding
```csharp
// Seed test database with:
- 3 test tenants
- 10 users per tenant (different roles)
- 20 events per tenant
- 50 guests per tenant
- Sample credit transactions
- Sample AI agent usages
```

---

## 9. CI/CD Integration

### GitHub Actions / Azure DevOps Pipeline

```yaml
name: Test Pipeline

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run Unit Tests
        run: dotnet test --filter Category=Unit
      - name: Upload Coverage
        run: codecov

  integration-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
    steps:
      - name: Run Integration Tests
        run: dotnet test --filter Category=Integration

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run E2E Tests
        run: dotnet test --filter Category=E2E
```

---

## 10. Testing Checklist

### Before MVP Release

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Multi-tenancy isolation verified
- [ ] Authentication & authorization tested
- [ ] Credit deduction logic verified
- [ ] AI agent integration tested
- [ ] Payment flow tested (Stripe test mode)
- [ ] Email delivery tested (SendGrid test mode)
- [ ] Row-Level Security policies validated
- [ ] API endpoints documented and tested
- [ ] Error handling tested
- [ ] Logging and monitoring verified

### Before Production Release

- [ ] All E2E tests pass
- [ ] Load testing completed
- [ ] Security audit completed
- [ ] Penetration testing completed
- [ ] Disaster recovery tested
- [ ] Database migration tested
- [ ] Monitoring and alerting configured
- [ ] Documentation complete

---

## Summary

This comprehensive testing strategy ensures:
- ✅ **Data Integrity**: Multi-tenant isolation, no cross-tenant leaks
- ✅ **Business Logic**: Credits, payments, AI agents work correctly
- ✅ **Security**: Authentication, authorization, input validation
- ✅ **Performance**: Fast response times under load
- ✅ **Reliability**: Error handling, retry logic, fallbacks

Tests should be written **after MVP completion** to validate the implementation against these requirements.
