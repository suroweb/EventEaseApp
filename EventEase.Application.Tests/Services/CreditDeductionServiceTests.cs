using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using EventEase.Infrastructure.Services.AI;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventEase.Application.Tests.Services;

/// <summary>
/// Comprehensive tests for CreditDeductionService - CRITICAL for financial accuracy
/// Tests credit transactions, balance calculations, and concurrency handling
/// </summary>
public class CreditDeductionServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Mock<ILogger<CreditDeductionService>> _loggerMock;

    public CreditDeductionServiceTests()
    {
        // Create unique in-memory database for each test run
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditDeductionTestDb_{Guid.NewGuid()}")
            .Options;

        _loggerMock = new Mock<ILogger<CreditDeductionService>>();

        // Seed initial test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var context = new ApplicationDbContext(_options);

        // Create test tenant with initial credits
        var tenant = new Tenant
        {
            Id = _testTenantId,
            Name = "Test Tenant",
            PrimaryContactEmail = "test@tenant.com",
            Status = TenantStatus.Trial,
            AvailableCredits = 100, // Start with 100 trial credits
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Tenants.Add(tenant);
        context.SaveChanges();
    }

    private ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(_options);
    }

    private CreditDeductionService CreateService()
    {
        var context = CreateContext();
        return new CreditDeductionService(context, _loggerMock.Object);
    }

    private async Task<Tenant> GetTenant()
    {
        using var context = CreateContext();
        var tenant = await context.Tenants.FindAsync(_testTenantId);
        return tenant ?? throw new InvalidOperationException("Test tenant not found");
    }

    private async Task<CreditTransaction?> GetLatestTransaction()
    {
        using var context = CreateContext();
        return await context.CreditTransactions
            .Where(t => t.TenantId == _testTenantId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<AIAgentUsage?> GetLatestUsageRecord()
    {
        using var context = CreateContext();
        return await context.AIAgentUsages
            .Where(u => u.TenantId == _testTenantId)
            .OrderByDescending(u => u.ExecutedAt)
            .FirstOrDefaultAsync();
    }

    #region Test 1-2: Sufficient Balance - Deduction and Transaction Creation

    [Fact]
    public async Task DeductCredits_SufficientBalance_DeductsCorrectAmount()
    {
        // Arrange
        var service = CreateService();
        const decimal deductionAmount = 50;

        // Act
        var result = await service.DeductCreditsAsync(
            tenantId: _testTenantId,
            amount: deductionAmount,
            agentType: AIAgentType.EmailGenerator,
            provider: AIProvider.OpenAI,
            description: "Email generation test");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AmountDeducted.Should().Be(deductionAmount);
        result.BalanceAfter.Should().Be(50, "100 - 50 = 50");

        var tenant = await GetTenant();
        tenant.AvailableCredits.Should().Be(50, "balance should be updated in database");
    }

    [Fact]
    public async Task DeductCredits_SufficientBalance_CreatesTransaction()
    {
        // Arrange
        var service = CreateService();
        const decimal deductionAmount = 30;

        // Act
        var result = await service.DeductCreditsAsync(
            tenantId: _testTenantId,
            amount: deductionAmount,
            agentType: AIAgentType.EventDescriptionGenerator,
            provider: AIProvider.Anthropic,
            description: "Event description generation");

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionId.Should().NotBeNull();

        var transaction = await GetLatestTransaction();
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(CreditTransactionType.Deduction);
        transaction.Amount.Should().Be(-deductionAmount, "deduction should be negative");
        transaction.BalanceAfter.Should().Be(70, "100 - 30 = 70");
        transaction.Description.Should().Be("Event description generation");
    }

    #endregion

    #region Test 3-4: Insufficient Balance

    [Fact]
    public async Task DeductCredits_InsufficientBalance_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        const decimal deductionAmount = 150; // More than available (100)

        // Act
        var result = await service.DeductCreditsAsync(
            tenantId: _testTenantId,
            amount: deductionAmount,
            agentType: AIAgentType.GuestListOptimizer,
            provider: AIProvider.OpenAI,
            description: "Guest list optimization");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient credits");
        result.ErrorMessage.Should().Contain("Available: 100");
        result.ErrorMessage.Should().Contain("Required: 150");
        result.BalanceAfter.Should().Be(100, "balance should remain unchanged");
    }

    [Fact]
    public async Task DeductCredits_InsufficientBalance_DoesNotCreateTransaction()
    {
        // Arrange
        var service = CreateService();
        const decimal deductionAmount = 200;

        using var context = CreateContext();
        var transactionCountBefore = await context.CreditTransactions
            .Where(t => t.TenantId == _testTenantId)
            .CountAsync();

        // Act
        var result = await service.DeductCreditsAsync(
            tenantId: _testTenantId,
            amount: deductionAmount,
            agentType: AIAgentType.BudgetPlanner,
            provider: AIProvider.DeepSeek,
            description: "Budget planning");

        // Assert
        result.Success.Should().BeFalse();

        using var verifyContext = CreateContext();
        var transactionCountAfter = await verifyContext.CreditTransactions
            .Where(t => t.TenantId == _testTenantId)
            .CountAsync();

        transactionCountAfter.Should().Be(transactionCountBefore,
            "no transaction should be created on insufficient balance");

        var tenant = await GetTenant();
        tenant.AvailableCredits.Should().Be(100, "balance should remain unchanged");
    }

    #endregion

    #region Test 5-6: Balance Updates and Audit Trail

    [Fact]
    public async Task DeductCredits_UpdatesBalanceCorrectly()
    {
        // Arrange
        var service = CreateService();

        // Act - Make multiple deductions
        await service.DeductCreditsAsync(_testTenantId, 10, AIAgentType.EmailGenerator,
            AIProvider.OpenAI, "Deduction 1");
        await service.DeductCreditsAsync(_testTenantId, 20, AIAgentType.EventDescriptionGenerator,
            AIProvider.Anthropic, "Deduction 2");
        await service.DeductCreditsAsync(_testTenantId, 15, AIAgentType.GuestListOptimizer,
            AIProvider.OpenAI, "Deduction 3");

        // Assert
        var tenant = await GetTenant();
        tenant.AvailableCredits.Should().Be(55, "100 - 10 - 20 - 15 = 55");
    }

    [Fact]
    public async Task DeductCredits_RecordsBalanceBeforeAndAfter()
    {
        // Arrange
        var service = CreateService();

        // First deduction
        await service.DeductCreditsAsync(_testTenantId, 25, AIAgentType.EmailGenerator,
            AIProvider.OpenAI, "First deduction");

        // Act - Second deduction
        var result = await service.DeductCreditsAsync(_testTenantId, 30, AIAgentType.BudgetPlanner,
            AIProvider.Anthropic, "Second deduction");

        // Assert
        result.Success.Should().BeTrue();

        var transaction = await GetLatestTransaction();
        transaction.Should().NotBeNull();
        transaction!.BalanceBefore.Should().Be(75, "balance before this deduction");
        transaction.BalanceAfter.Should().Be(45, "75 - 30 = 45");
        transaction.Amount.Should().Be(-30);
    }

    #endregion

    #region Test 7: Trial Credits Deduction

    [Fact]
    public async Task DeductCredits_TrialCredits_DeductsFromAvailableCredits()
    {
        // Arrange
        var service = CreateService();
        var tenant = await GetTenant();
        tenant.Status.Should().Be(TenantStatus.Trial, "tenant should be in trial status");

        // Act
        var result = await service.DeductCreditsAsync(
            _testTenantId,
            40,
            AIAgentType.EventDescriptionGenerator,
            AIProvider.DeepSeek,
            description: "Using trial credits");

        // Assert
        result.Success.Should().BeTrue();
        result.BalanceAfter.Should().Be(60, "trial credits can be used normally");

        var updatedTenant = await GetTenant();
        updatedTenant.AvailableCredits.Should().Be(60);
    }

    #endregion

    #region Test 8-10: Credit Addition Tests

    [Fact]
    public async Task AddCredits_Purchase_IncreasesBalance()
    {
        // Arrange
        using var context = CreateContext();
        var tenant = await context.Tenants.FindAsync(_testTenantId);
        var balanceBefore = tenant!.AvailableCredits;

        // Act - Simulate credit purchase
        tenant.AvailableCredits += 500;

        var transaction = new CreditTransaction
        {
            TenantId = _testTenantId,
            Type = CreditTransactionType.Purchase,
            Amount = 500,
            BalanceBefore = balanceBefore,
            BalanceAfter = tenant.AvailableCredits,
            Description = "Credit package purchase"
        };

        context.CreditTransactions.Add(transaction);
        await context.SaveChangesAsync();

        // Assert
        var updatedTenant = await GetTenant();
        updatedTenant.AvailableCredits.Should().Be(600, "100 + 500 = 600");

        var latestTransaction = await GetLatestTransaction();
        latestTransaction!.Type.Should().Be(CreditTransactionType.Purchase);
        latestTransaction.Amount.Should().Be(500, "purchase amount should be positive");
    }

    [Fact]
    public async Task AddCredits_Bonus_RecordsCorrectType()
    {
        // Arrange
        using var context = CreateContext();
        var tenant = await context.Tenants.FindAsync(_testTenantId);
        var balanceBefore = tenant!.AvailableCredits;

        // Act - Simulate bonus credits
        const decimal bonusAmount = 50;
        tenant.AvailableCredits += bonusAmount;

        var transaction = new CreditTransaction
        {
            TenantId = _testTenantId,
            Type = CreditTransactionType.Bonus,
            Amount = bonusAmount,
            BalanceBefore = balanceBefore,
            BalanceAfter = tenant.AvailableCredits,
            Description = "Referral bonus credits"
        };

        context.CreditTransactions.Add(transaction);
        await context.SaveChangesAsync();

        // Assert
        var latestTransaction = await GetLatestTransaction();
        latestTransaction!.Type.Should().Be(CreditTransactionType.Bonus);
        latestTransaction.Amount.Should().Be(50);
        latestTransaction.Description.Should().Contain("bonus");

        var updatedTenant = await GetTenant();
        updatedTenant.AvailableCredits.Should().Be(150, "100 + 50 bonus = 150");
    }

    [Fact]
    public async Task GetBalance_ReturnsCorrectAvailableCredits()
    {
        // Arrange
        var service = CreateService();

        // Make some deductions first
        await service.DeductCreditsAsync(_testTenantId, 35, AIAgentType.EmailGenerator,
            AIProvider.OpenAI, "Test deduction");

        // Act
        var balance = await service.GetAvailableCreditsAsync(_testTenantId);

        // Assert
        balance.Should().Be(65, "100 - 35 = 65");
    }

    #endregion

    #region Test 11: Audit Trail Verification

    [Fact]
    public async Task CreditTransaction_HasCorrectAuditTrail()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeductCreditsAsync(
            tenantId: _testTenantId,
            amount: 45,
            agentType: AIAgentType.BudgetPlanner,
            provider: AIProvider.Anthropic,
            description: "Budget planning audit test",
            inputTokens: 1500,
            outputTokens: 800,
            promptInput: "Create budget for corporate event",
            agentResponse: "Budget plan created successfully");

        // Assert
        result.Success.Should().BeTrue();

        // Verify transaction record
        var transaction = await GetLatestTransaction();
        transaction.Should().NotBeNull();
        transaction!.TenantId.Should().Be(_testTenantId);
        transaction.Type.Should().Be(CreditTransactionType.Deduction);
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify AI usage record
        var usageRecord = await GetLatestUsageRecord();
        usageRecord.Should().NotBeNull();
        usageRecord!.TenantId.Should().Be(_testTenantId);
        usageRecord.AgentType.Should().Be(AIAgentType.BudgetPlanner);
        usageRecord.Provider.Should().Be(AIProvider.Anthropic);
        usageRecord.CreditsCost.Should().Be(45);
        usageRecord.InputTokens.Should().Be(1500);
        usageRecord.OutputTokens.Should().Be(800);
        usageRecord.PromptInput.Should().Be("Create budget for corporate event");
        usageRecord.AgentResponse.Should().Be("Budget plan created successfully");
    }

    #endregion

    #region Test 12: Concurrency Test (CRITICAL)

    [Fact]
    public async Task ConcurrentDeductions_HandledCorrectly()
    {
        // Arrange
        // Create a fresh tenant with exactly 100 credits for this test
        var concurrentTestTenantId = Guid.NewGuid();
        using (var setupContext = CreateContext())
        {
            setupContext.Tenants.Add(new Tenant
            {
                Id = concurrentTestTenantId,
                Name = "Concurrent Test Tenant",
                PrimaryContactEmail = "concurrent@test.com",
                Status = TenantStatus.Active,
                AvailableCredits = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await setupContext.SaveChangesAsync();
        }

        // Act - Simulate 5 concurrent deductions of 20 credits each
        var tasks = new List<Task<CreditDeductionResult>>();
        for (int i = 0; i < 5; i++)
        {
            var taskService = CreateService();
            var task = taskService.DeductCreditsAsync(
                concurrentTestTenantId,
                20,
                AIAgentType.EmailGenerator,
                AIProvider.OpenAI,
                $"Concurrent deduction {i + 1}");
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.Success);
        successCount.Should().Be(5, "all 5 deductions should succeed as they total exactly 100");

        // Verify final balance
        using var verifyContext = CreateContext();
        var finalTenant = await verifyContext.Tenants.FindAsync(concurrentTestTenantId);
        finalTenant!.AvailableCredits.Should().Be(0, "100 - (5 * 20) = 0");

        // Verify all transactions were recorded
        var transactions = await verifyContext.CreditTransactions
            .Where(t => t.TenantId == concurrentTestTenantId)
            .ToListAsync();
        transactions.Should().HaveCount(5, "all 5 deductions should create transactions");

        // Verify total amount deducted
        var totalDeducted = transactions.Sum(t => Math.Abs(t.Amount));
        totalDeducted.Should().Be(100, "total deductions should equal 100");
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task DeductCredits_NonExistentTenant_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var nonExistentTenantId = Guid.NewGuid();

        // Act
        var result = await service.DeductCreditsAsync(
            nonExistentTenantId,
            10,
            AIAgentType.EmailGenerator,
            AIProvider.OpenAI,
            "Test");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tenant not found");
    }

    [Fact]
    public async Task DeductCredits_ZeroAmount_SucceedsWithNoChange()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeductCreditsAsync(
            _testTenantId,
            0,
            AIAgentType.EmailGenerator,
            AIProvider.OpenAI,
            "Zero amount test");

        // Assert
        result.Success.Should().BeTrue();
        result.BalanceAfter.Should().Be(100, "balance should remain unchanged");

        var tenant = await GetTenant();
        tenant.AvailableCredits.Should().Be(100);
    }

    [Fact]
    public async Task HasSufficientCredits_SufficientBalance_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var hasSufficient = await service.HasSufficientCreditsAsync(_testTenantId, 50);

        // Assert
        hasSufficient.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientCredits_InsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var hasSufficient = await service.HasSufficientCreditsAsync(_testTenantId, 150);

        // Assert
        hasSufficient.Should().BeFalse();
    }

    [Fact]
    public async Task HasSufficientCredits_ExactBalance_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var hasSufficient = await service.HasSufficientCreditsAsync(_testTenantId, 100);

        // Assert
        hasSufficient.Should().BeTrue("exact balance should be sufficient");
    }

    [Fact]
    public async Task GetAvailableCredits_NonExistentTenant_ReturnsZero()
    {
        // Arrange
        var service = CreateService();
        var nonExistentTenantId = Guid.NewGuid();

        // Act
        var balance = await service.GetAvailableCreditsAsync(nonExistentTenantId);

        // Assert
        balance.Should().Be(0);
    }

    #endregion

    public void Dispose()
    {
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureDeleted();
    }
}
