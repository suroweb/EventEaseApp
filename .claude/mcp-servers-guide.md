# EventEase MCP Servers Configuration

This file configures Model Context Protocol (MCP) servers for enhanced development experience with EventEaseApp in Claude Code.

## 🚀 Enabled MCP Servers

### 1. **PostgreSQL MCP** 🗄️
**Purpose**: Direct database access for queries, inspection, and data validation

**Capabilities**:
- Query all tables (events, users, registrations, credits, payments)
- Inspect multi-tenancy isolation
- Validate test data
- Check database schema
- Verify migrations

**Example Use Cases**:
```sql
-- Check how many events exist per tenant
SELECT tenant_id, COUNT(*) as event_count
FROM events
GROUP BY tenant_id;

-- Verify trial credits were created
SELECT * FROM credit_transactions
WHERE type = 'Bonus'
ORDER BY created_at DESC;

-- Inspect payment webhook processing
SELECT * FROM payment_transactions
WHERE stripe_payment_intent_id = 'pi_xxx';
```

**Connection**: `postgresql://postgres:your_password_here@localhost:5432/eventease_saas`

**To use**: Ask Claude to query the database
- "Show me all active tenants"
- "What events are scheduled for next week?"
- "Check the credit balance for tenant X"

---

### 2. **Filesystem MCP** 📁
**Purpose**: Advanced file operations beyond basic read/write

**Capabilities**:
- Search across entire codebase
- Pattern matching (glob patterns)
- Bulk file operations
- Directory traversal
- File metadata inspection

**Example Use Cases**:
- "Find all files that reference IEmailService"
- "Show me all test files with 'Authentication' in the name"
- "List all migration files"
- "Find TODOs in controller files"

**Root Directory**: `/home/user/EventEaseApp`

---

### 3. **Git MCP** 🌿
**Purpose**: Enhanced Git operations for version control

**Capabilities**:
- View commit history
- Create branches
- Stage and commit changes
- View diffs
- Manage tags
- Check status

**Example Use Cases**:
- "Show me commits in the last week"
- "Create a new branch for adding CreditPurchase entity"
- "What files changed since last commit?"
- "Show me the diff for Program.cs"

**Repository**: `/home/user/EventEaseApp`

---

### 4. **Sequential Thinking MCP** 🧠
**Purpose**: Enhanced reasoning for complex architectural decisions

**Capabilities**:
- Step-by-step problem solving
- Architecture decision analysis
- Debugging complex issues
- Performance optimization strategies

**Example Use Cases**:
- "Help me decide: database-per-tenant vs shared database?"
- "Debug why webhook idempotency might fail"
- "Optimize the credit deduction concurrent access logic"

---

### 5. **Memory MCP** 💾
**Purpose**: Persistent memory across Claude Code sessions

**Capabilities**:
- Remember project context
- Store architectural decisions
- Track TODOs and action items
- Remember user preferences

**Example Use Cases**:
- "Remember that we use DeepSeek for cost optimization"
- "What were the critical issues from the architecture review?"
- "What's the status of the CreditPurchase entity issue?"

---

### 6. **Fetch MCP** 🌐
**Purpose**: HTTP requests for testing external APIs

**Capabilities**:
- Test Stripe API calls
- Verify SendGrid email sending
- Test webhook endpoints
- Check OpenAI/Anthropic connectivity

**Example Use Cases**:
- "Test the Stripe payment intent creation"
- "Send a test email via SendGrid"
- "Verify the /health endpoint is responding"
- "Test OpenAI API with our key"

---

## 🔒 Optional MCP Servers (Disabled by Default)

### 7. **Brave Search MCP** 🔍
**Status**: Disabled (requires API key)

**Setup**:
1. Get API key from https://brave.com/search/api/
2. Update `mcp.json` with your key
3. Set `"disabled": false`

**Use Cases**:
- Search for ASP.NET Core 9.0 best practices
- Find solutions to EF Core migration errors
- Research PostgreSQL performance optimization

---

### 8. **GitHub MCP** 🐙
**Status**: Disabled (requires Personal Access Token)

**Setup**:
1. Create token at https://github.com/settings/tokens
2. Required scopes: `repo`, `workflow`, `write:packages`
3. Update `mcp.json` with your token
4. Set `"disabled": false`

**Use Cases**:
- Create pull requests automatically
- Manage GitHub issues
- Review pull request comments
- Update repository settings

---

## 📋 Configuration File Location

**File**: `/home/user/EventEaseApp/.claude/mcp.json`

**Format**: JSON configuration with server definitions

**Structure**:
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-name", "...args"],
      "description": "What this server does",
      "disabled": false,
      "env": {
        "API_KEY": "optional-environment-variables"
      }
    }
  }
}
```

---

## 🔧 Customization

### PostgreSQL Connection String

Update the connection string in `mcp.json` to match your database:

```json
"args": [
  "-y",
  "@modelcontextprotocol/server-postgres",
  "postgresql://USERNAME:PASSWORD@HOST:PORT/DATABASE"
]
```

**Current**: `postgresql://postgres:your_password_here@localhost:5432/eventease_saas`

**For Production** (read-only replica):
```json
"postgresql://readonly_user:password@prod-replica.example.com:5432/eventease_prod"
```

---

## 🚀 Quick Start Examples

### Database Queries
**Ask Claude**:
- "How many tenants are in trial status?"
- "Show me the latest 10 credit transactions"
- "Which events have the most registrations?"

### File Operations
**Ask Claude**:
- "Find all controllers that use IStripePaymentService"
- "Show me test files with failing tests"
- "List all entities that implement ITenantEntity"

### Git Operations
**Ask Claude**:
- "Show me uncommitted changes"
- "Create a branch called feature/credit-purchase-entity"
- "What commits did I make today?"

### Testing External Services
**Ask Claude**:
- "Test if the Stripe API key is valid"
- "Check if SendGrid can send emails"
- "Verify the health check endpoint is working"

---

## 📊 MCP Server Benefits for EventEase

### 1. **Faster Development** ⚡
- Direct database inspection without leaving Claude Code
- Quick file searches across entire codebase
- Instant Git operations

### 2. **Better Debugging** 🐛
- Query production-like data
- Verify multi-tenancy isolation
- Test payment webhook flow

### 3. **Enhanced Testing** 🧪
- Validate test data in database
- Check credit transaction accuracy
- Verify payment intent creation

### 4. **Architecture Decisions** 🏗️
- Use sequential thinking for complex decisions
- Remember past architectural choices
- Document decisions for future reference

### 5. **External Service Testing** 🌐
- Test Stripe without Postman
- Verify SendGrid without dashboard
- Check AI provider connectivity

---

## ⚠️ Security Considerations

### 1. **Database Access**
- MCP has **full database access**
- Can read/write all data
- Use **read-only credentials** for production databases
- **Never** point to production DB with write access

### 2. **API Keys**
- Store in environment variables (not in `mcp.json` directly)
- Use `.env` files (add to `.gitignore`)
- Rotate keys regularly

### 3. **Git Operations**
- MCP can commit and push changes
- Review commits before pushing
- Use branch protection rules

---

## 🔄 Restart Claude Code

After editing `mcp.json`, restart Claude Code to load the new configuration:

1. Close Claude Code window
2. Reopen project
3. Verify MCP servers loaded: "List available MCP servers"

---

## 📚 Additional Resources

- **MCP Documentation**: https://modelcontextprotocol.io/
- **PostgreSQL MCP**: https://github.com/modelcontextprotocol/servers/tree/main/src/postgres
- **Filesystem MCP**: https://github.com/modelcontextprotocol/servers/tree/main/src/filesystem
- **Git MCP**: https://github.com/modelcontextprotocol/servers/tree/main/src/git

---

## 🐛 Troubleshooting

### "MCP server failed to start"
1. Check `npx` is installed: `npx --version`
2. Verify Node.js version: `node --version` (should be 18+)
3. Check server logs in Claude Code

### "Database connection failed"
1. Verify PostgreSQL is running: `pg_isready`
2. Check connection string in `mcp.json`
3. Test connection: `psql postgresql://...`

### "Permission denied"
1. Check file permissions: `ls -la .claude/mcp.json`
2. Ensure EventEaseApp directory is accessible

---

**Last Updated**: 2025-11-13
**EventEase Version**: Phase 2.0
**MCP Servers**: 6 enabled, 2 optional
