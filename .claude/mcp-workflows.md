# EventEase MCP Development Workflows

This guide provides practical workflows using MCP servers for common EventEaseApp development tasks.

---

## 🔍 Workflow 1: Verify Multi-Tenancy Isolation

**Goal**: Ensure tenants cannot access each other's data

### Steps:

1. **Create Test Data** (PostgreSQL MCP)
```sql
-- Ask Claude: "Create two test tenants with sample events"

INSERT INTO tenants (id, name, status, available_credits) VALUES
('11111111-1111-1111-1111-111111111111', 'TenantA', 1, 100),
('22222222-2222-2222-2222-222222222222', 'TenantB', 1, 100);

INSERT INTO events (id, tenant_id, name, start_date, end_date) VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'TenantA Event', NOW(), NOW() + INTERVAL '1 day'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222', 'TenantB Event', NOW(), NOW() + INTERVAL '1 day');
```

2. **Verify Isolation** (PostgreSQL MCP)
```sql
-- Ask Claude: "Check if TenantA can see TenantB's events"

-- This should return ONLY TenantA's event (if global query filters work)
SELECT * FROM events WHERE tenant_id = '11111111-1111-1111-1111-111111111111';
```

3. **Run Tests** (Filesystem MCP + Git MCP)
```bash
# Ask Claude: "Run the multi-tenancy isolation tests"
# Claude will use Filesystem MCP to find the test file
# and execute: dotnet test --filter "MultiTenancyIsolationTests"
```

4. **Document Results** (Memory MCP)
```
# Ask Claude: "Remember that multi-tenancy isolation tests passed on 2025-11-13"
```

---

## 💳 Workflow 2: Test Payment Processing

**Goal**: Verify Stripe webhook processing and credit granting

### Steps:

1. **Create Test Tenant** (PostgreSQL MCP)
```sql
-- Ask Claude: "Create a trial tenant ready for upgrade"

INSERT INTO tenants (id, name, status, available_credits, trial_ends_at) VALUES
('test-tenant-id', 'Test Tenant', 0, 100, NOW() + INTERVAL '14 days');
```

2. **Simulate Payment Intent** (Fetch MCP)
```bash
# Ask Claude: "Test Stripe payment intent creation for Pro package (€399)"

# Claude uses Fetch MCP to call Stripe API:
curl https://api.stripe.com/v1/payment_intents \
  -u sk_test_your_key: \
  -d amount=39900 \
  -d currency=eur \
  -d "metadata[tenantId]=test-tenant-id" \
  -d "metadata[packageId]=pro-package-id"
```

3. **Simulate Webhook** (Fetch MCP)
```bash
# Ask Claude: "Send a test payment_intent.succeeded webhook"

# Claude sends webhook to local endpoint
POST http://localhost:5000/api/webhooks/stripe
Content-Type: application/json

{
  "type": "payment_intent.succeeded",
  "data": {
    "object": {
      "id": "pi_test_123",
      "amount": 39900,
      "metadata": {
        "tenantId": "test-tenant-id",
        "packageId": "pro-package-id"
      }
    }
  }
}
```

4. **Verify Credits Granted** (PostgreSQL MCP)
```sql
-- Ask Claude: "Check if credits were added for test-tenant-id"

SELECT * FROM tenants WHERE id = 'test-tenant-id';
-- Should show: available_credits = 6100 (100 trial + 6000 Pro package)

SELECT * FROM credit_transactions
WHERE tenant_id = 'test-tenant-id'
ORDER BY created_at DESC LIMIT 5;
-- Should show Purchase transaction for 6000 credits
```

5. **Check Test Results** (Sequential Thinking MCP)
```
# Ask Claude: "Did the payment processing work correctly? Analyze the results."

# Claude uses sequential thinking to:
# 1. Compare expected vs actual credit balance
# 2. Verify transaction was created
# 3. Check tenant status upgraded from Trial to Active
# 4. Identify any discrepancies
```

---

## 🧪 Workflow 3: Debug Failing Tests

**Goal**: Investigate and fix a failing test

### Steps:

1. **Find Failing Test** (Filesystem MCP)
```bash
# Ask Claude: "Find test files with authentication in the name"

# Claude searches: tests/**/*Authentication*.cs
```

2. **Read Test Code** (Filesystem MCP)
```bash
# Ask Claude: "Show me the AuthenticationServiceTests.cs file"
```

3. **Analyze Database State** (PostgreSQL MCP)
```sql
-- Ask Claude: "Check what test data exists in the users table"

SELECT id, email, failed_login_attempts, locked_until
FROM users
WHERE email LIKE '%test%';
```

4. **Check Recent Commits** (Git MCP)
```bash
# Ask Claude: "What changed in AuthenticationService.cs recently?"

# Claude uses Git MCP to show:
git log --oneline -10 --follow EventEase.Infrastructure/Services/AuthenticationService.cs
git diff HEAD~5 EventEase.Infrastructure/Services/AuthenticationService.cs
```

5. **Propose Fix** (Sequential Thinking MCP)
```
# Ask Claude: "The login test is failing. Help me debug why."

# Claude uses sequential thinking:
# 1. Analyze test expectations
# 2. Check implementation logic
# 3. Review database state
# 4. Identify root cause
# 5. Suggest fix
```

6. **Implement Fix** (Filesystem MCP)
```bash
# Ask Claude: "Fix the account lockout logic in AuthenticationService.cs"

# Claude edits the file using Filesystem MCP
```

7. **Verify Fix** (Git MCP)
```bash
# Ask Claude: "Show me what I changed and create a commit"

git diff
git add EventEase.Infrastructure/Services/AuthenticationService.cs
git commit -m "fix: correct account lockout duration to 30 minutes"
```

---

## 📊 Workflow 4: Performance Analysis

**Goal**: Identify slow queries and optimize database performance

### Steps:

1. **Enable Query Logging** (PostgreSQL MCP)
```sql
-- Ask Claude: "Show me the slowest queries in the last hour"

SELECT query, calls, total_exec_time, mean_exec_time
FROM pg_stat_statements
WHERE query LIKE '%events%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

2. **Analyze Query Plan** (PostgreSQL MCP)
```sql
-- Ask Claude: "Explain the query plan for fetching events with registrations"

EXPLAIN ANALYZE
SELECT e.*, r.*
FROM events e
LEFT JOIN event_registrations r ON e.id = r.event_id
WHERE e.tenant_id = '11111111-1111-1111-1111-111111111111'
AND e.status = 1;
```

3. **Check Missing Indexes** (PostgreSQL MCP)
```sql
-- Ask Claude: "What indexes should I add for better performance?"

SELECT schemaname, tablename, attname, n_distinct, correlation
FROM pg_stats
WHERE schemaname = 'public'
AND tablename IN ('events', 'event_registrations')
ORDER BY abs(correlation) DESC;
```

4. **Search for N+1 Queries** (Filesystem MCP)
```bash
# Ask Claude: "Find all controller methods that might have N+1 query problems"

# Claude searches for patterns like:
# - foreach loops with database calls
# - Missing .Include() statements
```

5. **Propose Optimization** (Sequential Thinking MCP + Memory MCP)
```
# Ask Claude: "Analyze performance issues and suggest optimizations"

# Claude:
# 1. Identifies missing indexes
# 2. Finds N+1 query patterns
# 3. Suggests query improvements
# 4. Recommends caching strategies
# 5. Stores recommendations in Memory MCP
```

---

## 🔐 Workflow 5: Security Audit

**Goal**: Verify security best practices are followed

### Steps:

1. **Check Authentication Logic** (Filesystem MCP)
```bash
# Ask Claude: "Review all authentication-related files for security issues"

# Claude searches for:
# - Password hashing (should use BCrypt)
# - JWT token generation
# - Refresh token handling
# - Account lockout logic
```

2. **Verify Multi-Tenancy Filters** (PostgreSQL MCP + Filesystem MCP)
```sql
-- Ask Claude: "List all entities and check if they have tenant isolation"

SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
AND table_type = 'BASE TABLE';
```

```csharp
// Then check each entity for ITenantEntity interface
```

3. **Check for SQL Injection** (Filesystem MCP)
```bash
# Ask Claude: "Find any raw SQL queries that might be vulnerable to injection"

# Claude searches for patterns:
# - FromSqlRaw with string concatenation
# - String interpolation in queries
```

4. **Review Authorization Policies** (Filesystem MCP)
```bash
# Ask Claude: "Show me all authorization policies and which endpoints use them"

# Claude reads Program.cs and all controllers
```

5. **Test Rate Limiting** (Fetch MCP)
```bash
# Ask Claude: "Send 10 rapid requests to test rate limiting"

# Claude uses Fetch MCP to send requests:
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/authentication/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}'
done

# Should return 429 Too Many Requests after 5 attempts
```

6. **Document Findings** (Memory MCP + Filesystem MCP)
```
# Ask Claude: "Create a security audit report"

# Claude:
# 1. Summarizes findings
# 2. Lists vulnerabilities
# 3. Provides recommendations
# 4. Stores in Memory MCP
# 5. Creates docs/SECURITY_AUDIT.md
```

---

## 🚀 Workflow 6: Deployment Preparation

**Goal**: Prepare for production deployment

### Steps:

1. **Run All Tests** (Git MCP + Filesystem MCP)
```bash
# Ask Claude: "Run all tests and show me the results"

dotnet test --logger "console;verbosity=detailed"
```

2. **Generate Migration Script** (Filesystem MCP)
```bash
# Ask Claude: "Generate an idempotent SQL migration script"

dotnet ef migrations script --idempotent --output migrations.sql
```

3. **Check Configuration** (Filesystem MCP)
```bash
# Ask Claude: "Review appsettings.json for production readiness"

# Claude checks:
# - JWT secret strength
# - Connection strings
# - API keys (should be environment variables)
# - CORS settings
```

4. **Verify Health Checks** (Fetch MCP)
```bash
# Ask Claude: "Test all health check endpoints"

curl http://localhost:5000/health
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/live
```

5. **Check Dependencies** (Filesystem MCP)
```bash
# Ask Claude: "List all NuGet packages and check for updates"

# Claude reads all .csproj files
```

6. **Create Deployment Checklist** (Memory MCP + Filesystem MCP)
```
# Ask Claude: "Create a production deployment checklist"

# Claude:
# 1. Reviews all configuration
# 2. Checks test coverage
# 3. Verifies migrations
# 4. Creates checklist in docs/DEPLOYMENT_CHECKLIST.md
# 5. Stores in Memory MCP for future reference
```

7. **Tag Release** (Git MCP)
```bash
# Ask Claude: "Create a git tag for version 1.0.0"

git tag -a v1.0.0 -m "Production release 1.0.0 - 95% ready"
git push origin v1.0.0
```

---

## 🧩 Workflow 7: Add Missing CreditPurchase Entity

**Goal**: Implement the missing CreditPurchase entity identified in tests

### Steps:

1. **Review Issue** (Memory MCP)
```
# Ask Claude: "What was the CreditPurchase entity issue from the architecture review?"

# Claude retrieves from memory:
# - Missing entity breaks payment processing
# - Referenced in StripePaymentService.cs
# - Needs to link payments to credit grants
```

2. **Create Entity** (Filesystem MCP)
```csharp
# Ask Claude: "Create the CreditPurchase entity based on the recommendation"

// Claude creates:
// EventEase.Domain/Entities/CreditPurchase.cs
```

3. **Update DbContext** (Filesystem MCP)
```csharp
# Ask Claude: "Add CreditPurchase DbSet to ApplicationDbContext"

// Claude edits ApplicationDbContext.cs
```

4. **Generate Migration** (Git MCP)
```bash
# Ask Claude: "Create a migration for the new CreditPurchase entity"

# Note: Requires dotnet CLI (document the command)
```

5. **Update Services** (Filesystem MCP)
```csharp
# Ask Claude: "Update StripePaymentService and PaymentWebhookService to use CreditPurchase"

// Claude edits both service files
```

6. **Add Tests** (Filesystem MCP)
```csharp
# Ask Claude: "Create tests for CreditPurchase creation and retrieval"

// Claude creates:
// tests/EventEase.Application.Tests/Services/CreditPurchaseServiceTests.cs
```

7. **Verify Changes** (Git MCP)
```bash
# Ask Claude: "Show me all changes and create a commit"

git status
git diff
git add .
git commit -m "feat: add CreditPurchase entity to fix payment processing"
```

---

## 📝 Best Practices with MCP Servers

### 1. **Use Descriptive Requests**
❌ "Check the database"
✅ "Show me all tenants with trial status expiring in the next 7 days"

### 2. **Combine Multiple MCP Servers**
```
# Ask Claude: "Find all controllers (Filesystem MCP), check which ones have
# corresponding tests (Filesystem MCP), and verify test data in database (PostgreSQL MCP)"
```

### 3. **Store Important Decisions**
```
# Ask Claude: "Remember that we decided to use DeepSeek for cost optimization
# and store this architectural decision"
# (Uses Memory MCP)
```

### 4. **Chain Operations**
```
# Ask Claude: "Query the database for failing payments (PostgreSQL MCP),
# find the webhook handler code (Filesystem MCP), and show recent commits
# to that file (Git MCP)"
```

### 5. **Use Sequential Thinking for Complex Problems**
```
# Ask Claude: "Should we implement database-per-tenant for large customers?
# Use sequential thinking to analyze pros/cons."
```

---

## 🎯 Quick Reference

| Task | MCP Server | Example |
|------|------------|---------|
| Query database | PostgreSQL | "How many active tenants exist?" |
| Search code | Filesystem | "Find all uses of IStripePaymentService" |
| View commits | Git | "Show commits from last week" |
| Test API | Fetch | "Test the health check endpoint" |
| Complex decision | Sequential Thinking | "Should we add caching to events API?" |
| Remember context | Memory | "Remember the CreditPurchase issue" |

---

**Last Updated**: 2025-11-13
**EventEase Version**: Phase 2.0 Complete
**MCP Servers**: 6 enabled
