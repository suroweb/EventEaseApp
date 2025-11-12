using EventEase.API.GraphQL.DataLoaders;
using EventEase.Domain.Entities;

namespace EventEase.API.GraphQL.Types;

/// <summary>
/// GraphQL ObjectType for User entity
/// </summary>
public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("Represents a user in the system");

        descriptor
            .Field(u => u.Id)
            .Description("Unique identifier for the user");

        descriptor
            .Field(u => u.TenantId)
            .Description("Tenant that owns this user");

        descriptor
            .Field(u => u.Email)
            .Description("User email address");

        descriptor
            .Field(u => u.FirstName)
            .Description("User first name");

        descriptor
            .Field(u => u.LastName)
            .Description("User last name");

        descriptor
            .Field(u => u.PhoneNumber)
            .Description("User phone number");

        descriptor
            .Field(u => u.AvatarUrl)
            .Description("User avatar URL");

        descriptor
            .Field(u => u.JobTitle)
            .Description("User job title");

        descriptor
            .Field(u => u.Department)
            .Description("User department");

        descriptor
            .Field(u => u.Role)
            .Description("User role");

        descriptor
            .Field(u => u.IsActive)
            .Description("Whether the user is active");

        descriptor
            .Field(u => u.EmailConfirmed)
            .Description("Whether email is confirmed");

        descriptor
            .Field(u => u.LastLoginAt)
            .Description("When user last logged in");

        descriptor
            .Field(u => u.PreferredLanguage)
            .Description("User's preferred language");

        descriptor
            .Field(u => u.TimeZone)
            .Description("User's timezone");

        descriptor
            .Field(u => u.CreatedAt)
            .Description("When the user was created");

        descriptor
            .Field(u => u.UpdatedAt)
            .Description("When the user was last updated");

        // Navigation properties
        descriptor
            .Field(u => u.Tenant)
            .ResolveWith<UserResolvers>(r => r.GetTenantAsync(default!, default!, default))
            .Description("Tenant that owns this user");

        // Computed field
        descriptor
            .Field("fullName")
            .Type<StringType>()
            .ResolveWith<UserResolvers>(r => r.GetFullName(default!))
            .Description("Full name of the user");

        // Sensitive fields - exclude from GraphQL
        descriptor.Ignore(u => u.PasswordHash);
        descriptor.Ignore(u => u.PasswordSalt);
        descriptor.Ignore(u => u.RefreshToken);
        descriptor.Ignore(u => u.RefreshTokenExpiresAt);
        descriptor.Ignore(u => u.PasswordResetToken);
        descriptor.Ignore(u => u.PasswordResetTokenExpiresAt);
        descriptor.Ignore(u => u.FailedLoginAttempts);
        descriptor.Ignore(u => u.LockedUntil);
    }
}

/// <summary>
/// Resolvers for User GraphQL type
/// </summary>
public class UserResolvers
{
    public async Task<Tenant> GetTenantAsync(
        [Parent] User user,
        TenantByIdDataLoader tenantLoader,
        CancellationToken cancellationToken)
    {
        return await tenantLoader.LoadAsync(user.TenantId, cancellationToken);
    }

    public string GetFullName([Parent] User user)
    {
        return $"{user.FirstName} {user.LastName}";
    }
}
