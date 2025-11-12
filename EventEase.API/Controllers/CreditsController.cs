using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.Controllers;

/// <summary>
/// Credit management and transaction endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CreditsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<CreditsController> _logger;

    public CreditsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<CreditsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get available credit packages
    /// </summary>
    /// <response code="200">Returns available credit packages</response>
    [HttpGet("packages")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CreditPackageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CreditPackageResponse>>> GetCreditPackages()
    {
        var packages = await _context.CreditPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new CreditPackageResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                BaseCredits = p.BaseCredits,
                BonusCredits = p.BonusCredits,
                ValidityDays = p.ValidityDays,
                IsActive = p.IsActive,
                IsPopular = p.IsPopular,
                DisplayOrder = p.DisplayOrder
            })
            .ToListAsync();

        return Ok(packages);
    }

    /// <summary>
    /// Get current credit balance and summary
    /// </summary>
    /// <response code="200">Returns credit balance and summary</response>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(CreditBalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreditBalanceResponse>> GetCreditBalance()
    {
        var tenantId = _currentTenantService.TenantId;

        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null)
        {
            return NotFound(new { Error = "Tenant not found" });
        }

        // Get transaction summary
        var transactions = await _context.CreditTransactions
            .Where(t => t.TenantId == tenantId)
            .ToListAsync();

        var totalEarned = transactions
            .Where(t => t.Type == CreditTransactionType.Purchase || t.Type == CreditTransactionType.Bonus)
            .Sum(t => t.Amount);

        var totalSpent = transactions
            .Where(t => t.Type == CreditTransactionType.Deduction)
            .Sum(t => Math.Abs(t.Amount));

        var totalPurchased = transactions
            .Where(t => t.Type == CreditTransactionType.Purchase)
            .Sum(t => t.Amount);

        // Get active purchases
        var activePurchases = await _context.CreditPurchases
            .Include(p => p.Package)
            .Where(p =>
                p.TenantId == tenantId &&
                p.PaymentStatus == PaymentStatus.Completed &&
                p.ExpiresAt > DateTime.UtcNow)
            .OrderBy(p => p.ExpiresAt)
            .ToListAsync();

        var nextExpiryDate = activePurchases.FirstOrDefault()?.ExpiresAt;

        // Calculate credits expiring in 30 days
        var creditsExpiringIn30Days = activePurchases
            .Where(p => p.ExpiresAt <= DateTime.UtcNow.AddDays(30))
            .Sum(p => p.CreditsGranted);

        // Get recent transactions (last 10)
        var recentTransactions = await _context.CreditTransactions
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentTransactionResponses = new List<CreditTransactionResponse>();

        foreach (var transaction in recentTransactions)
        {
            User? user = null;
            if (transaction.UserId.HasValue)
            {
                user = await _context.Users.FindAsync(transaction.UserId.Value);
            }

            recentTransactionResponses.Add(new CreditTransactionResponse
            {
                Id = transaction.Id,
                TenantId = transaction.TenantId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                RelatedEntityId = transaction.RelatedEntityId,
                RelatedEntityType = transaction.RelatedEntityType,
                PurchaseId = transaction.PurchaseId,
                UserId = transaction.UserId,
                UserEmail = user?.Email,
                CreatedAt = transaction.CreatedAt
            });
        }

        var activePurchaseResponses = activePurchases.Select(p => new CreditPurchaseResponse
        {
            Id = p.Id,
            TenantId = p.TenantId,
            PackageId = p.PackageId,
            PackageName = p.Package?.Name ?? "Unknown",
            Amount = p.Amount,
            Currency = p.Currency,
            CreditsGranted = p.CreditsGranted,
            PaymentStatus = p.PaymentStatus,
            StripePaymentIntentId = p.StripePaymentIntentId,
            StripeInvoiceId = p.StripeInvoiceId,
            PaidAt = p.PaidAt,
            ExpiresAt = p.ExpiresAt,
            CreatedAt = p.CreatedAt
        }).ToList();

        var isTrialAccount = tenant.Status == TenantStatus.Trial;
        var daysLeftInTrial = isTrialAccount && tenant.TrialEndsAt.HasValue
            ? Math.Max(0, (tenant.TrialEndsAt.Value - DateTime.UtcNow).Days)
            : (int?)null;

        return Ok(new CreditBalanceResponse
        {
            TenantId = tenantId,
            AvailableCredits = tenant.AvailableCredits,
            TotalCreditsEarned = totalEarned,
            TotalCreditsSpent = totalSpent,
            TotalCreditsPurchased = totalPurchased,
            LastPurchaseDate = activePurchases.LastOrDefault()?.PaidAt,
            NextExpiryDate = nextExpiryDate,
            CreditsExpiringIn30Days = creditsExpiringIn30Days,
            IsTrialAccount = isTrialAccount,
            TrialEndsAt = tenant.TrialEndsAt,
            DaysLeftInTrial = daysLeftInTrial,
            RecentTransactions = recentTransactionResponses,
            ActivePurchases = activePurchaseResponses
        });
    }

    /// <summary>
    /// Purchase a credit package
    /// </summary>
    /// <param name="request">Purchase request with package ID</param>
    /// <response code="200">Returns purchase details and payment information</response>
    /// <response code="404">Package not found</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("purchase")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PurchaseCreditPackage([FromBody] PurchaseCreditPackageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var package = await _context.CreditPackages.FindAsync(request.PackageId);

        if (package == null)
        {
            return NotFound(new { Error = "Credit package not found" });
        }

        if (!package.IsActive)
        {
            return BadRequest(new { Error = "This credit package is no longer available" });
        }

        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null)
        {
            return NotFound(new { Error = "Tenant not found" });
        }

        // TODO: Phase 1.5 - Implement actual Stripe payment processing
        // For now, create a pending purchase record

        var totalCredits = package.BaseCredits + (package.BonusCredits ?? 0);
        var expiresAt = DateTime.UtcNow.AddDays(package.ValidityDays);

        var purchase = new CreditPurchase
        {
            TenantId = tenantId,
            PackageId = package.Id,
            Amount = package.Price,
            Currency = package.Currency,
            CreditsGranted = totalCredits,
            PaymentStatus = PaymentStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.CreditPurchases.Add(purchase);

        _logger.LogInformation("Credit purchase initiated: {TenantId} purchasing package {PackageName} ({Credits} credits)",
            tenantId, package.Name, totalCredits);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            PurchaseId = purchase.Id,
            PackageName = package.Name,
            Amount = package.Price,
            Currency = package.Currency,
            Credits = totalCredits,
            ExpiresAt = expiresAt,
            PaymentStatus = "Pending",
            Message = "Purchase created. In Phase 1.5, this will integrate with Stripe for payment processing.",
            NextSteps = new[]
            {
                "Complete payment through Stripe",
                "Credits will be added to your account upon successful payment",
                "You will receive an invoice via email"
            }
        });
    }

    /// <summary>
    /// Get purchase history
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <response code="200">Returns purchase history</response>
    [HttpGet("purchases")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(PagedResult<CreditPurchaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CreditPurchaseResponse>>> GetPurchaseHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = _currentTenantService.TenantId;

        var query = _context.CreditPurchases
            .Include(p => p.Package)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();

        var purchases = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new CreditPurchaseResponse
            {
                Id = p.Id,
                TenantId = p.TenantId,
                PackageId = p.PackageId,
                PackageName = p.Package!.Name,
                Amount = p.Amount,
                Currency = p.Currency,
                CreditsGranted = p.CreditsGranted,
                PaymentStatus = p.PaymentStatus,
                StripePaymentIntentId = p.StripePaymentIntentId,
                StripeInvoiceId = p.StripeInvoiceId,
                PaidAt = p.PaidAt,
                ExpiresAt = p.ExpiresAt,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<CreditPurchaseResponse>
        {
            Items = purchases,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get transaction history
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="type">Filter by transaction type</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <response code="200">Returns transaction history</response>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PagedResult<CreditTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CreditTransactionResponse>>> GetTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CreditTransactionType? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = _currentTenantService.TenantId;

        var query = _context.CreditTransactions
            .Where(t => t.TenantId == tenantId);

        // Type filter
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        // Date range filter
        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= endDate.Value);
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var transactionResponses = new List<CreditTransactionResponse>();

        foreach (var transaction in transactions)
        {
            User? user = null;
            if (transaction.UserId.HasValue)
            {
                user = await _context.Users.FindAsync(transaction.UserId.Value);
            }

            transactionResponses.Add(new CreditTransactionResponse
            {
                Id = transaction.Id,
                TenantId = transaction.TenantId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                RelatedEntityId = transaction.RelatedEntityId,
                RelatedEntityType = transaction.RelatedEntityType,
                PurchaseId = transaction.PurchaseId,
                UserId = transaction.UserId,
                UserEmail = user?.Email,
                CreatedAt = transaction.CreatedAt
            });
        }

        return Ok(new PagedResult<CreditTransactionResponse>
        {
            Items = transactionResponses,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get credit usage statistics
    /// </summary>
    /// <param name="days">Number of days to include (default: 30)</param>
    /// <response code="200">Returns usage statistics</response>
    [HttpGet("usage-stats")]
    [ProducesResponseType(typeof(CreditUsageStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreditUsageStatsResponse>> GetUsageStats([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);

        var tenantId = _currentTenantService.TenantId;
        var startDate = DateTime.UtcNow.AddDays(-days);

        var aiUsages = await _context.AIAgentUsages
            .Where(u => u.TenantId == tenantId && u.ExecutedAt >= startDate)
            .ToListAsync();

        var totalCreditsSpent = aiUsages.Sum(u => u.CreditsCost);
        var totalOperations = aiUsages.Count;
        var averageCostPerOperation = totalOperations > 0 ? totalCreditsSpent / totalOperations : 0;

        // Usage by agent type
        var usageByAgent = aiUsages
            .GroupBy(u => u.AgentType)
            .Select(g => new AgentUsageStats
            {
                AgentType = g.Key.ToString(),
                OperationCount = g.Count(),
                TotalCredits = g.Sum(u => u.CreditsCost),
                AverageCostPerOperation = g.Average(u => u.CreditsCost),
                TotalInputTokens = g.Sum(u => u.InputTokens ?? 0),
                TotalOutputTokens = g.Sum(u => u.OutputTokens ?? 0)
            })
            .OrderByDescending(s => s.TotalCredits)
            .ToList();

        // Usage by provider
        var usageByProvider = aiUsages
            .GroupBy(u => u.Provider)
            .Select(g => new ProviderUsageStats
            {
                Provider = g.Key.ToString(),
                OperationCount = g.Count(),
                TotalCredits = g.Sum(u => u.CreditsCost),
                AverageCostPerOperation = g.Average(u => u.CreditsCost)
            })
            .OrderByDescending(s => s.TotalCredits)
            .ToList();

        // Daily usage
        var dailyUsage = aiUsages
            .GroupBy(u => u.ExecutedAt.Date)
            .Select(g => new DailyUsageStats
            {
                Date = g.Key,
                CreditsSpent = g.Sum(u => u.CreditsCost),
                OperationCount = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToList();

        return Ok(new CreditUsageStatsResponse
        {
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = DateTime.UtcNow,
            TotalCreditsSpent = totalCreditsSpent,
            TotalOperations = totalOperations,
            AverageCostPerOperation = averageCostPerOperation,
            UsageByAgent = usageByAgent,
            UsageByProvider = usageByProvider,
            DailyUsage = dailyUsage
        });
    }

    /// <summary>
    /// Deduct credits from tenant balance (internal use for AI operations)
    /// </summary>
    /// <param name="amount">Amount of credits to deduct</param>
    /// <param name="description">Description of the operation</param>
    /// <param name="relatedEntityId">Related entity ID (e.g., event ID, agent usage ID)</param>
    /// <param name="relatedEntityType">Related entity type</param>
    /// <response code="200">Credits deducted successfully</response>
    /// <response code="400">Insufficient credits</response>
    [HttpPost("deduct")]
    [Authorize(Policy = "SystemAdminOnly")]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger - internal use only
    public async Task<ActionResult> DeductCredits(
        [FromQuery] decimal amount,
        [FromQuery] string description,
        [FromQuery] Guid? relatedEntityId = null,
        [FromQuery] string? relatedEntityType = null)
    {
        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null)
        {
            return NotFound(new { Error = "Tenant not found" });
        }

        if (tenant.AvailableCredits < amount)
        {
            return BadRequest(new
            {
                Error = "Insufficient credits",
                Available = tenant.AvailableCredits,
                Required = amount,
                Shortfall = amount - tenant.AvailableCredits
            });
        }

        tenant.AvailableCredits -= amount;

        var transaction = new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.Deduction,
            Amount = -amount,
            BalanceAfter = tenant.AvailableCredits,
            Description = description,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            UserId = _currentUserService.UserId
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Credits deducted: {Amount} credits from tenant {TenantId} - {Description}",
            amount, tenantId, description);

        return Ok(new
        {
            Success = true,
            AmountDeducted = amount,
            BalanceAfter = tenant.AvailableCredits,
            TransactionId = transaction.Id
        });
    }
}
