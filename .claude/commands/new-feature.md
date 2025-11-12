# Create New Feature

Generate a complete feature with entity, migration, service, controller, DTOs, and tests.

## Usage

```
/new-feature <FeatureName>
```

## What This Command Does

1. Creates domain entity in `EventEase.Domain/Entities/`
2. Creates EF Core migration
3. Creates service interface in `EventEase.Application/Interfaces/`
4. Creates service implementation in `EventEase.Infrastructure/Services/`
5. Creates request/response DTOs in `EventEase.API/DTOs/`
6. Creates API controller in `EventEase.API/Controllers/`
7. Registers services in `Program.cs`
8. Creates unit tests
9. Creates integration tests
10. Updates documentation

## Example

```
/new-feature Speaker
```

This would create a complete speaker management feature with all necessary files and tests.
