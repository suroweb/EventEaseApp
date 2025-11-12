using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.Payment;

/// <summary>
/// Stripe payment service implementation
/// NOTE: This implementation uses Stripe .NET SDK (Stripe.net package)
/// Add package: dotnet add package Stripe.net
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _stripeSecretKey;

    public StripePaymentService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        _stripeSecretKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe Secret Key not configured");

        // Set Stripe API key globally (from Stripe.net SDK)
        // Stripe.StripeConfiguration.ApiKey = _stripeSecretKey;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid tenantId,
        Guid packageId,
        string? paymentMethodId = null,
        bool savePaymentMethod = false)
    {
        try
        {
            // Get tenant
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = "Tenant not found"
                };
            }

            // Get credit package
            var package = await _context.CreditPackages.FindAsync(packageId);
            if (package == null || !package.IsActive)
            {
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = "Credit package not found or inactive"
                };
            }

            // Get or create Stripe customer
            var customerId = await GetOrCreateCustomerAsync(tenantId);

            // Calculate amount in cents (Stripe uses smallest currency unit)
            var amountInCents = (long)(package.Price * 100);

            // Create payment intent metadata
            var metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId.ToString() },
                { "package_id", packageId.ToString() },
                { "package_name", package.Name },
                { "credits", (package.BaseCredits + (package.BonusCredits ?? 0)).ToString() }
            };

            // NOTE: Actual Stripe SDK usage would be:
            /*
            var options = new Stripe.PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = package.Currency.ToLower(),
                Customer = customerId,
                PaymentMethod = paymentMethodId,
                SetupFutureUsage = savePaymentMethod ? "off_session" : null,
                ConfirmationMethod = "automatic",
                Confirm = !string.IsNullOrEmpty(paymentMethodId),
                Metadata = metadata,
                Description = $"EventEase Credits: {package.Name}"
            };

            var service = new Stripe.PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            */

            // For demonstration, return simulated response
            var simulatedPaymentIntentId = $"pi_sim_{Guid.NewGuid():N}";
            var simulatedClientSecret = $"{simulatedPaymentIntentId}_secret_{Guid.NewGuid():N}";

            // Create purchase record in database
            var purchase = new CreditPurchase
            {
                TenantId = tenantId,
                PackageId = packageId,
                Amount = package.Price,
                Currency = package.Currency,
                CreditsGranted = package.BaseCredits + (package.BonusCredits ?? 0),
                PaymentStatus = PaymentStatus.Pending,
                StripePaymentIntentId = simulatedPaymentIntentId,
                StripeCustomerId = customerId,
                ExpiresAt = DateTime.UtcNow.AddDays(package.ValidityDays),
                CreatedByUserId = null // Will be set by controller
            };

            _context.CreditPurchases.Add(purchase);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment intent created: {PaymentIntentId} for tenant {TenantId}, package {PackageName}",
                simulatedPaymentIntentId, tenantId, package.Name);

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = simulatedPaymentIntentId,
                ClientSecret = simulatedClientSecret,
                Status = "requires_payment_method",
                Amount = package.Price,
                Currency = package.Currency,
                CustomerId = customerId,
                PaymentMethodId = paymentMethodId,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for tenant {TenantId}", tenantId);

            return new PaymentIntentResult
            {
                Success = false,
                ErrorMessage = $"Payment intent creation failed: {ex.Message}"
            };
        }
    }

    public async Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var service = new Stripe.PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId);

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount / 100m,
                Currency = paymentIntent.Currency.ToUpper(),
                CustomerId = paymentIntent.CustomerId,
                PaymentMethodId = paymentIntent.PaymentMethodId
            };
            */

            // Simulated response
            _logger.LogInformation("Payment intent confirmed: {PaymentIntentId}", paymentIntentId);

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = paymentIntentId,
                Status = "succeeded",
                Amount = 0, // Would come from Stripe
                Currency = "EUR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment intent {PaymentIntentId}", paymentIntentId);

            return new PaymentIntentResult
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = $"Payment confirmation failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> CancelPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var service = new Stripe.PaymentIntentService();
            await service.CancelAsync(paymentIntentId);
            */

            // Update purchase record
            var purchase = await _context.CreditPurchases
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (purchase != null)
            {
                purchase.PaymentStatus = PaymentStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Payment intent cancelled: {PaymentIntentId}", paymentIntentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<RefundResult> CreateRefundAsync(
        string paymentIntentId,
        decimal? amount = null,
        string? reason = null)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var options = new Stripe.RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amount.HasValue ? (long)(amount.Value * 100) : null, // null = full refund
                Reason = reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    "requested_by_customer" => "requested_by_customer",
                    _ => null
                }
            };

            var service = new Stripe.RefundService();
            var refund = await service.CreateAsync(options);

            return new RefundResult
            {
                Success = true,
                RefundId = refund.Id,
                Status = refund.Status,
                Amount = refund.Amount / 100m,
                Currency = refund.Currency.ToUpper(),
                Reason = refund.Reason
            };
            */

            // Update purchase record
            var purchase = await _context.CreditPurchases
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (purchase != null)
            {
                purchase.PaymentStatus = PaymentStatus.Refunded;
                await _context.SaveChangesAsync();

                // TODO: Deduct credits from tenant if already granted
            }

            var simulatedRefundId = $"re_sim_{Guid.NewGuid():N}";

            _logger.LogInformation("Refund created: {RefundId} for payment {PaymentIntentId}",
                simulatedRefundId, paymentIntentId);

            return new RefundResult
            {
                Success = true,
                RefundId = simulatedRefundId,
                Status = "succeeded",
                Amount = amount ?? 0,
                Currency = "EUR",
                Reason = reason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for payment {PaymentIntentId}", paymentIntentId);

            return new RefundResult
            {
                Success = false,
                ErrorMessage = $"Refund creation failed: {ex.Message}"
            };
        }
    }

    public async Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var service = new Stripe.PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount / 100m,
                Currency = paymentIntent.Currency.ToUpper(),
                CustomerId = paymentIntent.CustomerId,
                PaymentMethodId = paymentIntent.PaymentMethodId,
                Metadata = paymentIntent.Metadata
            };
            */

            // Get from database
            var purchase = await _context.CreditPurchases
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (purchase == null)
            {
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = "Payment intent not found"
                };
            }

            return new PaymentIntentResult
            {
                Success = true,
                PaymentIntentId = paymentIntentId,
                Status = purchase.PaymentStatus.ToString().ToLower(),
                Amount = purchase.Amount,
                Currency = purchase.Currency,
                CustomerId = purchase.StripeCustomerId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment intent {PaymentIntentId}", paymentIntentId);

            return new PaymentIntentResult
            {
                Success = false,
                ErrorMessage = $"Failed to retrieve payment intent: {ex.Message}"
            };
        }
    }

    public async Task<bool> AttachPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var service = new Stripe.PaymentMethodService();
            var options = new Stripe.PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            await service.AttachAsync(paymentMethodId, options);
            */

            _logger.LogInformation("Payment method {PaymentMethodId} attached to customer {CustomerId}",
                paymentMethodId, customerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);
            return false;
        }
    }

    public async Task<List<PaymentMethodInfo>> GetCustomerPaymentMethodsAsync(string customerId)
    {
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var service = new Stripe.PaymentMethodService();
            var options = new Stripe.PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await service.ListAsync(options);

            return paymentMethods.Data.Select(pm => new PaymentMethodInfo
            {
                Id = pm.Id,
                Type = pm.Type,
                Brand = pm.Card?.Brand,
                Last4 = pm.Card?.Last4,
                ExpMonth = pm.Card?.ExpMonth,
                ExpYear = pm.Card?.ExpYear,
                IsDefault = false // Would need to check customer.InvoiceSettings.DefaultPaymentMethod
            }).ToList();
            */

            // Simulated response
            _logger.LogInformation("Retrieved payment methods for customer {CustomerId}", customerId);

            return new List<PaymentMethodInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods for customer {CustomerId}", customerId);
            return new List<PaymentMethodInfo>();
        }
    }

    public async Task<string> GetOrCreateCustomerAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null)
        {
            throw new InvalidOperationException("Tenant not found");
        }

        // If customer already exists, return it
        if (!string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            return tenant.StripeCustomerId;
        }

        // Create new Stripe customer
        try
        {
            // NOTE: Actual Stripe SDK usage:
            /*
            var options = new Stripe.CustomerCreateOptions
            {
                Email = tenant.Email, // Would need to add Email to Tenant entity
                Name = tenant.Name,
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", tenantId.ToString() }
                }
            };

            var service = new Stripe.CustomerService();
            var customer = await service.CreateAsync(options);

            tenant.StripeCustomerId = customer.Id;
            await _context.SaveChangesAsync();

            return customer.Id;
            */

            // Simulated customer creation
            var simulatedCustomerId = $"cus_sim_{Guid.NewGuid():N}";

            tenant.StripeCustomerId = simulatedCustomerId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created Stripe customer {CustomerId} for tenant {TenantId}",
                simulatedCustomerId, tenantId);

            return simulatedCustomerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
