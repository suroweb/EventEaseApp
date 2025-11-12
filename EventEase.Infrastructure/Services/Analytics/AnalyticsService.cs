using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.Analytics;

/// <summary>
/// Service for generating comprehensive analytics and insights
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<EventAnalyticsResponse> GetEventAnalyticsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .Include(e => e.Invitations)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        var registrations = eventEntity.Registrations.ToList();
        var totalRegistrations = registrations.Count;
        var confirmedAttendees = registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        var cancelledRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Cancelled);
        var checkedInAttendees = registrations.Count(r => r.Status == RegistrationStatus.CheckedIn);
        var noShowAttendees = confirmedAttendees - checkedInAttendees;

        // Calculate rates
        var attendanceRate = confirmedAttendees > 0 ? (decimal)checkedInAttendees / confirmedAttendees * 100 : 0;
        var cancellationRate = totalRegistrations > 0 ? (decimal)cancelledRegistrations / totalRegistrations * 100 : 0;
        var noShowRate = confirmedAttendees > 0 ? (decimal)noShowAttendees / confirmedAttendees * 100 : 0;

        // Capacity metrics
        decimal? capacityUtilization = null;
        if (eventEntity.MaxAttendees.HasValue && eventEntity.MaxAttendees > 0)
        {
            capacityUtilization = (decimal)confirmedAttendees / eventEntity.MaxAttendees.Value * 100;
        }

        // Revenue metrics
        var totalRevenue = registrations.Sum(r => r.TotalAmount);
        var averageRevenuePerAttendee = confirmedAttendees > 0 ? totalRevenue / confirmedAttendees : 0;

        // Invitation metrics
        var invitationsSent = eventEntity.Invitations.Count;
        var registrationsViaInvitation = registrations.Count(r => r.InvitationId.HasValue);
        var invitationConversionRate = invitationsSent > 0 ? (decimal)registrationsViaInvitation / invitationsSent * 100 : 0;
        var overallConversionRate = totalRegistrations > 0 ? (decimal)confirmedAttendees / totalRegistrations * 100 : 0;

        // Registrations by date
        var registrationsByDate = registrations
            .GroupBy(r => r.RegisteredAt.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString("yyyy-MM-dd"),
                g => g.Count());

        // Registrations by source
        var registrationsBySource = registrations
            .GroupBy(r => r.RegistrationSource ?? "unknown")
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        // Demographics
        var attendeesByCompany = registrations
            .Where(r => !string.IsNullOrEmpty(r.Company))
            .GroupBy(r => r.Company!)
            .OrderByDescending(g => g.Count())
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        var uniqueCompanies = attendeesByCompany.Keys.Count;
        var topCompanies = attendeesByCompany
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => kvp.Key)
            .ToList();

        // AI Usage
        var aiUsage = await _context.AIAgentUsages
            .Where(a => a.EventId == eventId)
            .ToListAsync(cancellationToken);

        var aiGenerationCost = aiUsage.Sum(a => a.CreditsCost);

        return new EventAnalyticsResponse
        {
            EventId = eventEntity.Id,
            EventName = eventEntity.Name,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Status = eventEntity.Status.ToString(),
            TotalRegistrations = totalRegistrations,
            ConfirmedAttendees = confirmedAttendees,
            CancelledRegistrations = cancelledRegistrations,
            CheckedInAttendees = checkedInAttendees,
            NoShowAttendees = noShowAttendees,
            AttendanceRate = Math.Round(attendanceRate, 2),
            CancellationRate = Math.Round(cancellationRate, 2),
            NoShowRate = Math.Round(noShowRate, 2),
            MaxAttendees = eventEntity.MaxAttendees,
            CapacityUtilization = capacityUtilization.HasValue ? Math.Round(capacityUtilization.Value, 2) : null,
            IsFree = eventEntity.IsFree,
            TicketPrice = eventEntity.Price,
            Currency = eventEntity.Currency,
            TotalRevenue = totalRevenue,
            AverageRevenuePerAttendee = Math.Round(averageRevenuePerAttendee, 2),
            InvitationsSent = invitationsSent,
            RegistrationsViaInvitation = registrationsViaInvitation,
            InvitationConversionRate = Math.Round(invitationConversionRate, 2),
            OverallConversionRate = Math.Round(overallConversionRate, 2),
            RegistrationsByDate = registrationsByDate,
            RegistrationsBySource = registrationsBySource,
            AttendeesByCompany = attendeesByCompany,
            UniqueCompanies = uniqueCompanies,
            TopCompanies = topCompanies,
            IsAIGenerated = eventEntity.IsAIGenerated,
            AIGenerationCost = aiGenerationCost > 0 ? aiGenerationCost : null
        };
    }

    public async Task<TenantAnalyticsResponse> GetTenantAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} not found");
        }

        // Get events in period
        var events = await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var totalEvents = events.Count;
        var publishedEvents = events.Count(e => e.Status == EventStatus.Published);
        var draftEvents = events.Count(e => e.Status == EventStatus.Draft);
        var completedEvents = events.Count(e => e.Status == EventStatus.Completed);
        var cancelledEvents = events.Count(e => e.Status == EventStatus.Cancelled);
        var upcomingEvents = events.Count(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow);

        // Attendee metrics
        var allRegistrations = events.SelectMany(e => e.Registrations).ToList();
        var totalRegistrations = allRegistrations.Count;
        var confirmedAttendees = allRegistrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        var totalCheckIns = allRegistrations.Count(r => r.Status == RegistrationStatus.CheckedIn);
        var uniqueAttendees = allRegistrations.Select(r => r.Email.ToLower()).Distinct().Count();
        var averageAttendeesPerEvent = totalEvents > 0 ? (decimal)confirmedAttendees / totalEvents : 0;

        // Revenue metrics
        var totalRevenue = allRegistrations.Sum(r => r.TotalAmount);
        var averageRevenuePerEvent = totalEvents > 0 ? totalRevenue / totalEvents : 0;
        var averageRevenuePerAttendee = confirmedAttendees > 0 ? totalRevenue / confirmedAttendees : 0;

        // Engagement metrics
        var totalInvitationsSent = await _context.Invitations
            .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate)
            .CountAsync(cancellationToken);

        var totalGuestsManaged = await _context.Guests
            .Where(g => g.CreatedAt >= startDate && g.CreatedAt <= endDate)
            .CountAsync(cancellationToken);

        var overallConversionRate = totalRegistrations > 0 ? (decimal)confirmedAttendees / totalRegistrations * 100 : 0;

        // Credit metrics
        var creditUsageRate = tenant.TotalCreditsPurchased > 0
            ? tenant.TotalCreditsUsed / tenant.TotalCreditsPurchased * 100
            : 0;

        // AI Usage metrics
        var aiUsages = await _context.AIAgentUsages
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var totalAIRequests = aiUsages.Count;
        var totalAICreditsCost = aiUsages.Sum(a => a.CreditsCost);

        var aiRequestsByAgent = aiUsages
            .GroupBy(a => a.AgentType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var aiCostByAgent = aiUsages
            .GroupBy(a => a.AgentType.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(a => a.CreditsCost));

        // Payment metrics
        var payments = await _context.PaymentTransactions
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var totalPayments = payments.Count;
        var totalPaymentAmount = payments.Sum(p => p.Amount);
        var successfulPayments = payments.Count(p => p.Status == PaymentStatus.Succeeded);
        var failedPayments = payments.Count(p => p.Status == PaymentStatus.Failed);
        var paymentSuccessRate = totalPayments > 0 ? (decimal)successfulPayments / totalPayments * 100 : 0;

        // Trends by month
        var eventsByMonth = events
            .GroupBy(e => e.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var attendeesByMonth = allRegistrations
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .GroupBy(r => r.RegisteredAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var revenueByMonth = allRegistrations
            .GroupBy(r => r.RegisteredAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmount));

        // Calculate tenant health score
        var healthScore = await CalculateTenantHealthScoreAsync(cancellationToken);

        return new TenantAnalyticsResponse
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalEvents = totalEvents,
            PublishedEvents = publishedEvents,
            DraftEvents = draftEvents,
            CompletedEvents = completedEvents,
            CancelledEvents = cancelledEvents,
            UpcomingEvents = upcomingEvents,
            TotalRegistrations = totalRegistrations,
            ConfirmedAttendees = confirmedAttendees,
            TotalCheckIns = totalCheckIns,
            UniqueAttendees = uniqueAttendees,
            AverageAttendeesPerEvent = Math.Round(averageAttendeesPerEvent, 2),
            TotalRevenue = totalRevenue,
            AverageRevenuePerEvent = Math.Round(averageRevenuePerEvent, 2),
            AverageRevenuePerAttendee = Math.Round(averageRevenuePerAttendee, 2),
            Currency = tenant.DefaultCurrency ?? "EUR",
            TotalInvitationsSent = totalInvitationsSent,
            TotalGuestsManaged = totalGuestsManaged,
            OverallConversionRate = Math.Round(overallConversionRate, 2),
            AvailableCredits = tenant.AvailableCredits,
            TotalCreditsPurchased = tenant.TotalCreditsPurchased,
            TotalCreditsUsed = tenant.TotalCreditsUsed,
            CreditUsageRate = Math.Round(creditUsageRate, 2),
            TotalAIRequests = totalAIRequests,
            TotalAICreditsCost = totalAICreditsCost,
            AIRequestsByAgent = aiRequestsByAgent,
            AICostByAgent = aiCostByAgent,
            TotalPayments = totalPayments,
            TotalPaymentAmount = totalPaymentAmount,
            SuccessfulPayments = successfulPayments,
            FailedPayments = failedPayments,
            PaymentSuccessRate = Math.Round(paymentSuccessRate, 2),
            EventsByMonth = eventsByMonth,
            AttendeesByMonth = attendeesByMonth,
            RevenueByMonth = revenueByMonth,
            TenantHealthScore = healthScore
        };
    }

    public async Task<RegistrationAnalyticsResponse> GetRegistrationAnalyticsAsync(
        Guid? eventId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EventRegistrations
            .Include(r => r.Event)
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(r => r.EventId == eventId.Value);
        }

        var registrations = await query
            .Where(r => r.RegisteredAt >= startDate && r.RegisteredAt <= endDate)
            .ToListAsync(cancellationToken);

        string? eventName = null;
        if (eventId.HasValue)
        {
            var eventEntity = await _context.Events.FindAsync(new object[] { eventId.Value }, cancellationToken);
            eventName = eventEntity?.Name;
        }

        var totalRegistrations = registrations.Count;
        var pendingRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Pending);
        var confirmedRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        var cancelledRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Cancelled);
        var waitlistedRegistrations = registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);

        // Conversion rates
        var pendingToConfirmedRate = pendingRegistrations > 0
            ? (decimal)confirmedRegistrations / (pendingRegistrations + confirmedRegistrations) * 100
            : 0;
        var cancellationRate = totalRegistrations > 0
            ? (decimal)cancelledRegistrations / totalRegistrations * 100
            : 0;
        var checkedInCount = registrations.Count(r => r.CheckedInAt.HasValue);
        var checkInRate = confirmedRegistrations > 0
            ? (decimal)checkedInCount / confirmedRegistrations * 100
            : 0;

        // Timeline analysis
        var registrationsByDate = registrations
            .GroupBy(r => r.RegisteredAt.Date.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var registrationsByDayOfWeek = registrations
            .GroupBy(r => r.RegisteredAt.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var registrationsByHour = registrations
            .GroupBy(r => r.RegisteredAt.Hour.ToString())
            .OrderBy(g => int.Parse(g.Key))
            .ToDictionary(g => $"{g.Key}:00", g => g.Count());

        // Source analysis
        var registrationsBySource = registrations
            .GroupBy(r => r.RegistrationSource ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var mostEffectiveSource = registrationsBySource
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault().Key ?? "N/A";

        // Performance metrics
        var confirmedRegs = registrations.Where(r => r.ConfirmedAt.HasValue).ToList();
        var avgTimeToConfirm = confirmedRegs.Any()
            ? TimeSpan.FromTicks((long)confirmedRegs.Average(r => (r.ConfirmedAt!.Value - r.RegisteredAt).Ticks))
            : TimeSpan.Zero;

        var checkedInRegs = registrations.Where(r => r.CheckedInAt.HasValue).ToList();
        var avgTimeToCheckIn = checkedInRegs.Any()
            ? TimeSpan.FromTicks((long)checkedInRegs.Average(r => (r.CheckedInAt!.Value - r.RegisteredAt).Ticks))
            : TimeSpan.Zero;

        // Demographics
        var registrationsByCompany = registrations
            .Where(r => !string.IsNullOrEmpty(r.Company))
            .GroupBy(r => r.Company!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        // Returning vs new attendees
        var uniqueEmails = registrations.Select(r => r.Email.ToLower()).Distinct().ToList();
        var historicalRegistrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt < startDate && uniqueEmails.Contains(r.Email.ToLower()))
            .Select(r => r.Email.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);

        var returningAttendees = historicalRegistrations.Count;
        var newAttendees = uniqueEmails.Count - returningAttendees;
        var returningAttendeeRate = uniqueEmails.Count > 0
            ? (decimal)returningAttendees / uniqueEmails.Count * 100
            : 0;

        return new RegistrationAnalyticsResponse
        {
            EventId = eventId,
            EventName = eventName,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalRegistrations = totalRegistrations,
            PendingRegistrations = pendingRegistrations,
            ConfirmedRegistrations = confirmedRegistrations,
            CancelledRegistrations = cancelledRegistrations,
            WaitlistedRegistrations = waitlistedRegistrations,
            PendingToConfirmedRate = Math.Round(pendingToConfirmedRate, 2),
            CancellationRate = Math.Round(cancellationRate, 2),
            CheckInRate = Math.Round(checkInRate, 2),
            RegistrationsByDate = registrationsByDate,
            RegistrationsByDayOfWeek = registrationsByDayOfWeek,
            RegistrationsByHour = registrationsByHour,
            RegistrationsBySource = registrationsBySource,
            MostEffectiveSource = mostEffectiveSource,
            AverageTimeToConfirm = avgTimeToConfirm,
            AverageTimeToCheckIn = avgTimeToCheckIn,
            RegistrationsByCompany = registrationsByCompany,
            ReturningAttendees = returningAttendees,
            NewAttendees = newAttendees,
            ReturningAttendeeRate = Math.Round(returningAttendeeRate, 2)
        };
    }

    public async Task<CreditAnalyticsResponse> GetCreditAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} not found");
        }

        // Get transactions in period
        var transactions = await _context.CreditTransactions
            .Include(t => t.AIAgentUsage)
            .Include(t => t.CreditPackage)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var purchaseTransactions = transactions.Where(t => t.Type == CreditTransactionType.Purchase).ToList();
        var usageTransactions = transactions.Where(t => t.Type == CreditTransactionType.Deduction).ToList();
        var refundTransactions = transactions.Where(t => t.Type == CreditTransactionType.Refund).ToList();

        var totalPurchased = purchaseTransactions.Sum(t => t.Amount);
        var totalUsed = Math.Abs(usageTransactions.Sum(t => t.Amount));
        var totalRefunded = refundTransactions.Sum(t => t.Amount);

        // Usage by agent
        var aiUsages = await _context.AIAgentUsages
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var usageByAgent = aiUsages
            .GroupBy(a => a.AgentType.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(a => a.CreditsCost));

        var requestsByAgent = aiUsages
            .GroupBy(a => a.AgentType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var usageByDate = usageTransactions
            .GroupBy(t => t.CreatedAt.Date.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));

        // Transaction metrics
        var totalPurchaseTransactions = purchaseTransactions.Count;
        var totalUsageTransactions = usageTransactions.Count;
        var averageTransactionAmount = transactions.Any()
            ? transactions.Average(t => Math.Abs(t.Amount))
            : 0;
        var averageCostPerRequest = aiUsages.Any()
            ? aiUsages.Average(a => a.CreditsCost)
            : 0;

        // Spending velocity
        var days = (endDate - startDate).Days;
        var dailyAverageSpend = days > 0 ? totalUsed / days : 0;
        var weeklyAverageSpend = dailyAverageSpend * 7;
        var monthlyAverageSpend = dailyAverageSpend * 30;
        var estimatedDaysUntilDepletion = dailyAverageSpend > 0
            ? (int)(tenant.AvailableCredits / dailyAverageSpend)
            : int.MaxValue;

        // Package analysis
        var purchasesByPackageType = purchaseTransactions
            .Where(t => t.CreditPackage != null)
            .GroupBy(t => t.CreditPackage!.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var mostCommonPackageSize = purchaseTransactions
            .Where(t => t.CreditPackage != null)
            .GroupBy(t => t.CreditPackage!.CreditAmount)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? 0;

        // Efficiency metrics
        var successfulRequests = aiUsages.Count(a => a.IsSuccess);
        var successfulRequestRate = aiUsages.Any()
            ? (decimal)successfulRequests / aiUsages.Count * 100
            : 0;
        var averageTokensPerRequest = aiUsages.Where(a => a.TotalTokens.HasValue).Any()
            ? (decimal)aiUsages.Where(a => a.TotalTokens.HasValue).Average(a => a.TotalTokens!.Value)
            : 0;
        var costPerToken = averageTokensPerRequest > 0
            ? averageCostPerRequest / averageTokensPerRequest
            : 0;

        // Monthly spend trend
        var monthlySpend = usageTransactions
            .GroupBy(t => t.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));

        // Calculate trend
        var spendTrend = 0m;
        if (monthlySpend.Count >= 2)
        {
            var lastMonth = monthlySpend.Values.Last();
            var previousMonth = monthlySpend.Values.SkipLast(1).Last();
            if (previousMonth > 0)
            {
                spendTrend = (lastMonth - previousMonth) / previousMonth * 100;
            }
        }

        return new CreditAnalyticsResponse
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            CurrentBalance = tenant.AvailableCredits,
            TotalPurchased = totalPurchased,
            TotalUsed = totalUsed,
            TotalRefunded = totalRefunded,
            UsageByAgent = usageByAgent,
            RequestsByAgent = requestsByAgent,
            UsageByDate = usageByDate,
            TotalPurchaseTransactions = totalPurchaseTransactions,
            TotalUsageTransactions = totalUsageTransactions,
            AverageTransactionAmount = Math.Round(averageTransactionAmount, 2),
            AverageCostPerRequest = Math.Round(averageCostPerRequest, 4),
            DailyAverageSpend = Math.Round(dailyAverageSpend, 2),
            WeeklyAverageSpend = Math.Round(weeklyAverageSpend, 2),
            MonthlyAverageSpend = Math.Round(monthlyAverageSpend, 2),
            EstimatedDaysUntilDepletion = Math.Min(estimatedDaysUntilDepletion, 999),
            PurchasesByPackageType = purchasesByPackageType,
            MostCommonPackageSize = mostCommonPackageSize,
            SuccessfulRequestRate = Math.Round(successfulRequestRate, 2),
            AverageTokensPerRequest = Math.Round(averageTokensPerRequest, 0),
            CostPerToken = Math.Round(costPerToken, 6),
            MonthlySpend = monthlySpend,
            SpendTrend = Math.Round(spendTrend, 2)
        };
    }

    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant {tenantId} not found");
        }

        // Calculate period length for previous period
        var periodLength = endDate - startDate;
        var previousStart = startDate - periodLength;
        var previousEnd = startDate.AddSeconds(-1);

        // Current period metrics
        var events = await _context.Events
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var previousEvents = await _context.Events
            .Where(e => e.CreatedAt >= previousStart && e.CreatedAt <= previousEnd)
            .CountAsync(cancellationToken);

        var registrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= startDate && r.RegisteredAt <= endDate &&
                       (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn))
            .ToListAsync(cancellationToken);

        var previousRegistrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= previousStart && r.RegisteredAt <= previousEnd &&
                       (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn))
            .CountAsync(cancellationToken);

        var revenue = registrations.Sum(r => r.TotalAmount);
        var previousRevenue = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= previousStart && r.RegisteredAt <= previousEnd)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        var activeUsers = await _context.Users
            .Where(u => u.LastLoginAt >= startDate && u.LastLoginAt <= endDate)
            .CountAsync(cancellationToken);

        var previousActiveUsers = await _context.Users
            .Where(u => u.LastLoginAt >= previousStart && u.LastLoginAt <= previousEnd)
            .CountAsync(cancellationToken);

        // Key metrics with comparisons
        var totalEventsMetric = CreateKeyMetric(events.Count, previousEvents, "Total Events", "");
        var totalAttendeesMetric = CreateKeyMetric(registrations.Count, previousRegistrations, "Total Attendees", "");
        var totalRevenueMetric = CreateKeyMetric(revenue, previousRevenue, "Total Revenue", tenant.DefaultCurrency ?? "EUR");
        var activeUsersMetric = CreateKeyMetric(activeUsers, previousActiveUsers, "Active Users", "");

        // Quick stats
        var upcomingEvents = await _context.Events
            .CountAsync(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow, cancellationToken);

        var ongoingEvents = await _context.Events
            .CountAsync(e => e.Status == EventStatus.Published &&
                           e.StartDate <= DateTime.UtcNow &&
                           e.EndDate >= DateTime.UtcNow, cancellationToken);

        var completedEventsThisMonth = await _context.Events
            .CountAsync(e => e.Status == EventStatus.Completed &&
                           e.EndDate >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                       cancellationToken);

        var avgCapacity = await CalculateAverageCapacityUtilizationAsync(cancellationToken);

        // Recent activities
        var recentActivities = await GetRecentActivitiesAsync(10, cancellationToken);

        // Trend data (last 12 months)
        var eventsTrend = await GetEventsTrendAsync(12, cancellationToken);
        var attendeesTrend = await GetAttendeesTrendAsync(12, cancellationToken);
        var revenueTrend = await GetRevenueTrendAsync(12, cancellationToken);

        // Top performers
        var topByAttendance = await GetTopEventsByAttendanceAsync(5, cancellationToken);
        var topByRevenue = await GetTopEventsByRevenueAsync(5, cancellationToken);

        return new DashboardMetricsResponse
        {
            TenantId = tenantId,
            GeneratedAt = DateTime.UtcNow,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalEvents = totalEventsMetric,
            TotalAttendees = totalAttendeesMetric,
            TotalRevenue = totalRevenueMetric,
            ActiveUsers = activeUsersMetric,
            UpcomingEvents = upcomingEvents,
            OngoingEvents = ongoingEvents,
            CompletedEventsThisMonth = completedEventsThisMonth,
            AvailableCredits = tenant.AvailableCredits,
            AverageEventCapacityUtilization = avgCapacity,
            RecentActivities = recentActivities,
            EventsTrend = eventsTrend,
            AttendeesTrend = attendeesTrend,
            RevenueTrend = revenueTrend,
            TopEventsByAttendance = topByAttendance,
            TopEventsByRevenue = topByRevenue
        };
    }

    public async Task<ComparisonAnalyticsResponse> GetComparisonAnalyticsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        string comparisonType = "month-over-month",
        CancellationToken cancellationToken = default)
    {
        // Calculate previous period based on comparison type
        var periodLength = currentEnd - currentStart;
        var previousStart = currentStart - periodLength;
        var previousEnd = currentStart.AddSeconds(-1);

        var currentMetrics = await GetPeriodMetricsAsync(currentStart, currentEnd, cancellationToken);
        var previousMetrics = await GetPeriodMetricsAsync(previousStart, previousEnd, cancellationToken);

        currentMetrics.Label = GetPeriodLabel(currentStart, currentEnd);
        previousMetrics.Label = GetPeriodLabel(previousStart, previousEnd);

        var comparison = CalculateComparison(currentMetrics, previousMetrics);

        return new ComparisonAnalyticsResponse
        {
            ComparisonType = comparisonType,
            CurrentPeriod = currentMetrics,
            PreviousPeriod = previousMetrics,
            Comparison = comparison
        };
    }

    public async Task<PredictiveAnalyticsResponse> GetPredictiveAnalyticsAsync(
        int forecastDays = 30,
        CancellationToken cancellationToken = default)
    {
        // Get historical data (last 90 days)
        var historicalStart = DateTime.UtcNow.AddDays(-90);
        var historicalEnd = DateTime.UtcNow;

        var historicalEvents = await _context.Events
            .Where(e => e.CreatedAt >= historicalStart && e.CreatedAt <= historicalEnd)
            .CountAsync(cancellationToken);

        var historicalAttendees = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= historicalStart && r.RegisteredAt <= historicalEnd &&
                       (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn))
            .CountAsync(cancellationToken);

        var historicalRevenue = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= historicalStart && r.RegisteredAt <= historicalEnd)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        var historicalCredits = await _context.CreditTransactions
            .Where(t => t.CreatedAt >= historicalStart && t.CreatedAt <= historicalEnd &&
                       t.Type == CreditTransactionType.Deduction)
            .SumAsync(t => Math.Abs(t.Amount), cancellationToken);

        // Simple linear forecasting (can be enhanced with ML models)
        var dailyEventRate = historicalEvents / 90m;
        var dailyAttendeeRate = historicalAttendees / 90m;
        var dailyRevenueRate = historicalRevenue / 90m;
        var dailyCreditRate = historicalCredits / 90m;

        var expectedEvents = new ForecastMetric
        {
            PredictedValue = Math.Round(dailyEventRate * forecastDays, 0),
            LowerBound = Math.Round(dailyEventRate * forecastDays * 0.8m, 0),
            UpperBound = Math.Round(dailyEventRate * forecastDays * 1.2m, 0),
            ConfidenceLevel = 70,
            Unit = "events"
        };

        var expectedAttendees = new ForecastMetric
        {
            PredictedValue = Math.Round(dailyAttendeeRate * forecastDays, 0),
            LowerBound = Math.Round(dailyAttendeeRate * forecastDays * 0.75m, 0),
            UpperBound = Math.Round(dailyAttendeeRate * forecastDays * 1.25m, 0),
            ConfidenceLevel = 75,
            Unit = "attendees"
        };

        var expectedRevenue = new ForecastMetric
        {
            PredictedValue = Math.Round(dailyRevenueRate * forecastDays, 2),
            LowerBound = Math.Round(dailyRevenueRate * forecastDays * 0.7m, 2),
            UpperBound = Math.Round(dailyRevenueRate * forecastDays * 1.3m, 2),
            ConfidenceLevel = 65,
            Unit = "EUR"
        };

        var expectedCreditUsage = new ForecastMetric
        {
            PredictedValue = Math.Round(dailyCreditRate * forecastDays, 2),
            LowerBound = Math.Round(dailyCreditRate * forecastDays * 0.8m, 2),
            UpperBound = Math.Round(dailyCreditRate * forecastDays * 1.2m, 2),
            ConfidenceLevel = 80,
            Unit = "credits"
        };

        // Generate forecast time series
        var eventsForecast = GenerateForecastTimeSeries(dailyEventRate, forecastDays);
        var attendeesForecast = GenerateForecastTimeSeries(dailyAttendeeRate, forecastDays);
        var revenueForecast = GenerateForecastTimeSeries(dailyRevenueRate, forecastDays);

        // Generate recommendations
        var recommendations = await GenerateRecommendationsAsync(
            expectedEvents.PredictedValue,
            expectedAttendees.PredictedValue,
            expectedRevenue.PredictedValue,
            cancellationToken);

        // Generate risk indicators
        var riskIndicators = await GenerateRiskIndicatorsAsync(
            expectedCreditUsage.PredictedValue,
            cancellationToken);

        return new PredictiveAnalyticsResponse
        {
            GeneratedAt = DateTime.UtcNow,
            ForecastPeriod = $"Next {forecastDays} days",
            ExpectedEvents = expectedEvents,
            ExpectedAttendees = expectedAttendees,
            ExpectedRevenue = expectedRevenue,
            ExpectedCreditUsage = expectedCreditUsage,
            EventsForecast = eventsForecast,
            AttendeesForecast = attendeesForecast,
            RevenueForecast = revenueForecast,
            Recommendations = recommendations,
            RiskIndicators = riskIndicators,
            ConfidenceScore = 70,
            ModelAccuracy = "Based on 90-day historical average"
        };
    }

    public async Task<List<EventAnalyticsResponse>> GetBulkEventAnalyticsAsync(
        List<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        var analytics = new List<EventAnalyticsResponse>();

        foreach (var eventId in eventIds)
        {
            try
            {
                var eventAnalytics = await GetEventAnalyticsAsync(eventId, cancellationToken);
                analytics.Add(eventAnalytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get analytics for event {EventId}", eventId);
            }
        }

        return analytics;
    }

    public async Task<decimal> CalculateTenantHealthScoreAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenantService.TenantId;
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);

        if (tenant == null)
        {
            return 0;
        }

        decimal score = 0;

        // Factor 1: Event activity (30 points)
        var recentEvents = await _context.Events
            .CountAsync(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30), cancellationToken);
        score += Math.Min(recentEvents * 5, 30);

        // Factor 2: Credit health (20 points)
        if (tenant.TotalCreditsPurchased > 0)
        {
            var creditBalance = tenant.AvailableCredits / tenant.TotalCreditsPurchased;
            score += creditBalance * 20;
        }

        // Factor 3: Engagement (20 points)
        var activeUsers = await _context.Users
            .CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7), cancellationToken);
        score += Math.Min(activeUsers * 4, 20);

        // Factor 4: Revenue (15 points)
        var recentRevenue = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(r => r.TotalAmount, cancellationToken);
        score += Math.Min((decimal)(recentRevenue / 1000), 15);

        // Factor 5: Success rate (15 points)
        var totalRegistrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync(cancellationToken);
        var confirmedRegistrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= DateTime.UtcNow.AddDays(-30) &&
                       r.Status == RegistrationStatus.Confirmed)
            .CountAsync(cancellationToken);

        if (totalRegistrations > 0)
        {
            score += ((decimal)confirmedRegistrations / totalRegistrations) * 15;
        }

        return Math.Min(Math.Round(score, 2), 100);
    }

    public async Task<Dictionary<string, decimal>> GetRealTimeMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, decimal>();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        metrics["events_today"] = await _context.Events
            .CountAsync(e => e.CreatedAt >= todayStart, cancellationToken);

        metrics["registrations_today"] = await _context.EventRegistrations
            .CountAsync(r => r.RegisteredAt >= todayStart, cancellationToken);

        metrics["revenue_today"] = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= todayStart)
            .SumAsync(r => r.TotalAmount, cancellationToken);

        metrics["active_users_now"] = await _context.Users
            .CountAsync(u => u.LastLoginAt >= now.AddHours(-1), cancellationToken);

        metrics["ongoing_events"] = await _context.Events
            .CountAsync(e => e.Status == EventStatus.Published &&
                           e.StartDate <= now &&
                           e.EndDate >= now, cancellationToken);

        metrics["credits_used_today"] = await _context.CreditTransactions
            .Where(t => t.CreatedAt >= todayStart && t.Type == CreditTransactionType.Deduction)
            .SumAsync(t => Math.Abs(t.Amount), cancellationToken);

        return metrics;
    }

    // Helper methods
    private KeyMetric CreateKeyMetric(decimal current, decimal previous, string label, string unit)
    {
        var change = previous > 0 ? ((current - previous) / previous) * 100 : 0;
        var trend = Math.Abs(change) < 5 ? "stable" : (change > 0 ? "up" : "down");

        return new KeyMetric
        {
            Value = current,
            PreviousPeriodValue = previous,
            ChangePercentage = Math.Round(change, 2),
            Trend = trend,
            Label = label,
            Unit = unit
        };
    }

    private async Task<decimal> CalculateAverageCapacityUtilizationAsync(CancellationToken cancellationToken)
    {
        var events = await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.MaxAttendees.HasValue && e.MaxAttendees > 0)
            .ToListAsync(cancellationToken);

        if (!events.Any())
        {
            return 0;
        }

        var utilizationRates = events
            .Select(e =>
            {
                var confirmed = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
                return e.MaxAttendees.HasValue && e.MaxAttendees > 0
                    ? (decimal)confirmed / e.MaxAttendees.Value * 100
                    : 0;
            })
            .ToList();

        return Math.Round(utilizationRates.Average(), 2);
    }

    private async Task<List<RecentActivity>> GetRecentActivitiesAsync(int limit, CancellationToken cancellationToken)
    {
        var activities = new List<RecentActivity>();

        // Get recent events
        var recentEvents = await _context.Events
            .Include(e => e.CreatedByUser)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit / 2)
            .ToListAsync(cancellationToken);

        activities.AddRange(recentEvents.Select(e => new RecentActivity
        {
            Timestamp = e.CreatedAt,
            Type = "event_created",
            Description = $"Event '{e.Name}' was created",
            UserName = $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}",
            EntityName = e.Name
        }));

        // Get recent registrations
        var recentRegistrations = await _context.EventRegistrations
            .Include(r => r.Event)
            .OrderByDescending(r => r.RegisteredAt)
            .Take(limit / 2)
            .ToListAsync(cancellationToken);

        activities.AddRange(recentRegistrations.Select(r => new RecentActivity
        {
            Timestamp = r.RegisteredAt,
            Type = "registration",
            Description = $"New registration for '{r.Event.Name}'",
            UserName = $"{r.FirstName} {r.LastName}",
            EntityName = r.Event.Name
        }));

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToList();
    }

    private async Task<List<TrendDataPoint>> GetEventsTrendAsync(int months, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var events = await _context.Events
            .Where(e => e.CreatedAt >= startDate)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return events.Select(e => new TrendDataPoint
        {
            Date = new DateTime(e.Year, e.Month, 1),
            Label = new DateTime(e.Year, e.Month, 1).ToString("MMM yyyy"),
            Value = e.Count
        }).OrderBy(t => t.Date).ToList();
    }

    private async Task<List<TrendDataPoint>> GetAttendeesTrendAsync(int months, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var registrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= startDate &&
                       (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn))
            .GroupBy(r => new { r.RegisteredAt.Year, r.RegisteredAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return registrations.Select(r => new TrendDataPoint
        {
            Date = new DateTime(r.Year, r.Month, 1),
            Label = new DateTime(r.Year, r.Month, 1).ToString("MMM yyyy"),
            Value = r.Count
        }).OrderBy(t => t.Date).ToList();
    }

    private async Task<List<TrendDataPoint>> GetRevenueTrendAsync(int months, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var revenue = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= startDate)
            .GroupBy(r => new { r.RegisteredAt.Year, r.RegisteredAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(r => r.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        return revenue.Select(r => new TrendDataPoint
        {
            Date = new DateTime(r.Year, r.Month, 1),
            Label = new DateTime(r.Year, r.Month, 1).ToString("MMM yyyy"),
            Value = r.Revenue
        }).OrderBy(t => t.Date).ToList();
    }

    private async Task<List<TopEvent>> GetTopEventsByAttendanceAsync(int limit, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.Registrations.Count(r =>
                r.Status == RegistrationStatus.Confirmed ||
                r.Status == RegistrationStatus.CheckedIn))
            .Take(limit)
            .Select(e => new TopEvent
            {
                EventId = e.Id,
                EventName = e.Name,
                StartDate = e.StartDate,
                AttendeeCount = e.Registrations.Count(r =>
                    r.Status == RegistrationStatus.Confirmed ||
                    r.Status == RegistrationStatus.CheckedIn),
                Revenue = e.Registrations.Sum(r => r.TotalAmount),
                Currency = e.Currency
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<TopEvent>> GetTopEventsByRevenueAsync(int limit, CancellationToken cancellationToken)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.Registrations.Sum(r => r.TotalAmount))
            .Take(limit)
            .Select(e => new TopEvent
            {
                EventId = e.Id,
                EventName = e.Name,
                StartDate = e.StartDate,
                AttendeeCount = e.Registrations.Count(r =>
                    r.Status == RegistrationStatus.Confirmed ||
                    r.Status == RegistrationStatus.CheckedIn),
                Revenue = e.Registrations.Sum(r => r.TotalAmount),
                Currency = e.Currency
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<PeriodMetrics> GetPeriodMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var events = await _context.Events
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var registrations = await _context.EventRegistrations
            .Where(r => r.RegisteredAt >= startDate && r.RegisteredAt <= endDate)
            .ToListAsync(cancellationToken);

        var eventsCreated = events.Count;
        var eventsPublished = events.Count(e => e.Status == EventStatus.Published);
        var totalRegistrations = registrations.Count;
        var confirmedAttendees = registrations.Count(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.CheckedIn);
        var totalRevenue = registrations.Sum(r => r.TotalAmount);

        var creditUsage = await _context.CreditTransactions
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate &&
                       t.Type == CreditTransactionType.Deduction)
            .SumAsync(t => Math.Abs(t.Amount), cancellationToken);

        var aiRequests = await _context.AIAgentUsages
            .CountAsync(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate, cancellationToken);

        var paymentVolume = await _context.PaymentTransactions
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .SumAsync(p => p.Amount, cancellationToken);

        var avgAttendeesPerEvent = eventsCreated > 0 ? (decimal)confirmedAttendees / eventsCreated : 0;
        var conversionRate = totalRegistrations > 0 ? (decimal)confirmedAttendees / totalRegistrations * 100 : 0;

        var checkedIn = registrations.Count(r => r.CheckedInAt.HasValue);
        var attendanceRate = confirmedAttendees > 0 ? (decimal)checkedIn / confirmedAttendees * 100 : 0;

        return new PeriodMetrics
        {
            StartDate = startDate,
            EndDate = endDate,
            EventsCreated = eventsCreated,
            EventsPublished = eventsPublished,
            TotalRegistrations = totalRegistrations,
            ConfirmedAttendees = confirmedAttendees,
            TotalRevenue = totalRevenue,
            CreditUsage = creditUsage,
            AIRequests = aiRequests,
            PaymentVolume = paymentVolume,
            AverageAttendeesPerEvent = Math.Round(avgAttendeesPerEvent, 2),
            ConversionRate = Math.Round(conversionRate, 2),
            AttendanceRate = Math.Round(attendanceRate, 2)
        };
    }

    private string GetPeriodLabel(DateTime start, DateTime end)
    {
        if (start.Year == end.Year && start.Month == end.Month)
        {
            return start.ToString("MMMM yyyy");
        }
        return $"{start.ToString("MMM yyyy")} - {end.ToString("MMM yyyy")}";
    }

    private ComparisonMetrics CalculateComparison(PeriodMetrics current, PeriodMetrics previous)
    {
        return new ComparisonMetrics
        {
            EventsCreated = CreateChangeMetric(current.EventsCreated, previous.EventsCreated, true),
            EventsPublished = CreateChangeMetric(current.EventsPublished, previous.EventsPublished, true),
            TotalRegistrations = CreateChangeMetric(current.TotalRegistrations, previous.TotalRegistrations, true),
            ConfirmedAttendees = CreateChangeMetric(current.ConfirmedAttendees, previous.ConfirmedAttendees, true),
            TotalRevenue = CreateChangeMetric(current.TotalRevenue, previous.TotalRevenue, true),
            CreditUsage = CreateChangeMetric(current.CreditUsage, previous.CreditUsage, false),
            AIRequests = CreateChangeMetric(current.AIRequests, previous.AIRequests, true),
            PaymentVolume = CreateChangeMetric(current.PaymentVolume, previous.PaymentVolume, true),
            AverageAttendeesPerEvent = CreateChangeMetric(current.AverageAttendeesPerEvent, previous.AverageAttendeesPerEvent, true),
            ConversionRate = CreateChangeMetric(current.ConversionRate, previous.ConversionRate, true),
            AttendanceRate = CreateChangeMetric(current.AttendanceRate, previous.AttendanceRate, true),
            OverallTrend = "stable",
            OverallChangePercentage = 0
        };
    }

    private ChangeMetric CreateChangeMetric(decimal current, decimal previous, bool higherIsBetter)
    {
        var absoluteChange = current - previous;
        var percentageChange = previous > 0 ? (absoluteChange / previous) * 100 : 0;
        var direction = Math.Abs(percentageChange) < 5 ? "stable" : (percentageChange > 0 ? "up" : "down");
        var isImprovement = higherIsBetter ? percentageChange > 0 : percentageChange < 0;

        return new ChangeMetric
        {
            AbsoluteChange = Math.Round(absoluteChange, 2),
            PercentageChange = Math.Round(percentageChange, 2),
            Direction = direction,
            IsImprovement = isImprovement
        };
    }

    private List<ForecastDataPoint> GenerateForecastTimeSeries(decimal dailyRate, int forecastDays)
    {
        var dataPoints = new List<ForecastDataPoint>();
        var today = DateTime.UtcNow.Date;

        for (int i = 1; i <= forecastDays; i++)
        {
            var date = today.AddDays(i);
            var predictedValue = dailyRate * i;

            dataPoints.Add(new ForecastDataPoint
            {
                Date = date,
                Label = date.ToString("MMM dd"),
                PredictedValue = Math.Round(predictedValue, 2),
                ActualValue = null,
                LowerBound = Math.Round(predictedValue * 0.8m, 2),
                UpperBound = Math.Round(predictedValue * 1.2m, 2)
            });
        }

        return dataPoints;
    }

    private async Task<List<Recommendation>> GenerateRecommendationsAsync(
        decimal expectedEvents,
        decimal expectedAttendees,
        decimal expectedRevenue,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<Recommendation>();

        // Recommendation based on event activity
        if (expectedEvents < 5)
        {
            recommendations.Add(new Recommendation
            {
                Category = "events",
                Title = "Increase Event Creation",
                Description = "Your forecast shows low event activity. Consider creating more events to engage your audience.",
                Priority = "medium",
                ImpactScore = 70,
                Actions = new List<string>
                {
                    "Use AI agents to quickly generate event ideas",
                    "Schedule recurring events",
                    "Create event templates for faster setup"
                }
            });
        }

        // Recommendation based on revenue
        if (expectedRevenue < 1000)
        {
            recommendations.Add(new Recommendation
            {
                Category = "pricing",
                Title = "Optimize Pricing Strategy",
                Description = "Revenue forecast is below target. Consider reviewing your pricing strategy.",
                Priority = "high",
                ImpactScore = 85,
                Actions = new List<string>
                {
                    "Analyze competitor pricing",
                    "Consider tiered ticket options",
                    "Offer early bird discounts"
                }
            });
        }

        return recommendations;
    }

    private async Task<List<RiskIndicator>> GenerateRiskIndicatorsAsync(
        decimal expectedCreditUsage,
        CancellationToken cancellationToken)
    {
        var risks = new List<RiskIndicator>();
        var tenant = await _context.Tenants.FindAsync(new object[] { _currentTenantService.TenantId }, cancellationToken);

        if (tenant != null && expectedCreditUsage > tenant.AvailableCredits)
        {
            risks.Add(new RiskIndicator
            {
                Type = "credits",
                Description = "Credit balance may be insufficient for forecasted usage",
                Severity = "high",
                Probability = 80,
                EstimatedOccurrence = DateTime.UtcNow.AddDays(15),
                MitigationStrategies = new List<string>
                {
                    "Purchase additional credit packages",
                    "Optimize AI agent usage",
                    "Enable automatic credit top-up"
                }
            });
        }

        return risks;
    }
}
