# 🤝 Contributing to EventEaseApp

Thank you for your interest in contributing to EventEaseApp! This document provides guidelines and instructions for contributing to the project.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

---

## 🌟 Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in your interactions.

### Expected Behavior

- ✅ Use welcoming and inclusive language
- ✅ Be respectful of differing viewpoints
- ✅ Accept constructive criticism gracefully
- ✅ Focus on what is best for the community
- ✅ Show empathy towards other community members

### Unacceptable Behavior

- ❌ Trolling, insulting/derogatory comments
- ❌ Public or private harassment
- ❌ Publishing others' private information
- ❌ Unprofessional conduct

---

## 🚀 Getting Started

### 1. Fork and Clone

```bash
# Fork the repository on GitHub
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/EventEaseApp.git
cd EventEaseApp

# Add upstream remote
git remote add upstream https://github.com/suroweb/EventEaseApp.git
```

### 2. Set Up Development Environment

Follow the [Development Setup Guide](DEV_SETUP.md) to configure your local environment.

```bash
# Install dependencies
dotnet restore

# Start infrastructure
docker-compose up -d

# Run the application
dotnet run --project EventEaseApp
```

### 3. Create a Branch

```bash
# Update your main branch
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
```

---

## 🔄 Development Workflow

### Branch Naming Convention

Use descriptive branch names with the following prefixes:

- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Adding or updating tests
- `chore/` - Maintenance tasks

**Examples:**
```
feature/qr-code-generation
fix/registration-email-not-sent
docs/api-documentation
refactor/event-service
test/registration-service
chore/update-dependencies
```

### Development Process

1. **Write Code**
   ```bash
   # Make your changes
   # Test locally
   dotnet run
   ```

2. **Write Tests**
   ```bash
   # Create tests for your changes
   # Run tests
   dotnet test
   ```

3. **Run Linters and Formatters**
   ```bash
   # Format code
   dotnet format

   # Build to check for warnings
   dotnet build --configuration Release
   ```

4. **Commit Changes**
   ```bash
   git add .
   git commit -m "feat: add QR code generation for tickets"
   ```

5. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create Pull Request**
   - Go to GitHub
   - Click "New Pull Request"
   - Fill out the PR template
   - Submit for review

---

## 📝 Coding Standards

### C# Coding Conventions

Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions):

#### Naming Conventions

```csharp
// PascalCase for classes, methods, properties
public class EventService { }
public void RegisterUser() { }
public string FirstName { get; set; }

// camelCase for private fields
private readonly IEventRepository _eventRepository;

// camelCase for parameters
public void AddEvent(Event newEvent, int userId) { }

// UPPERCASE for constants
private const int MAX_ATTENDEES = 500;

// Interfaces start with 'I'
public interface IEventService { }
```

#### Code Organization

```csharp
// 1. Using statements
using System;
using System.Collections.Generic;
using EventEaseApp.Core.Entities;

// 2. Namespace
namespace EventEaseApp.Core.Services
{
    // 3. Class documentation
    /// <summary>
    /// Manages event operations including CRUD and business logic
    /// </summary>
    public class EventService : IEventService
    {
        // 4. Fields
        private readonly IEventRepository _repository;
        private readonly ILogger<EventService> _logger;

        // 5. Constructor
        public EventService(IEventRepository repository, ILogger<EventService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 6. Public methods
        public async Task<Event> GetEventByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving event with ID {EventId}", id);
            return await _repository.GetByIdAsync(id);
        }

        // 7. Private methods
        private bool ValidateEvent(Event eventEntity)
        {
            return eventEntity != null && !string.IsNullOrEmpty(eventEntity.Name);
        }
    }
}
```

#### Best Practices

```csharp
// ✅ DO: Use meaningful variable names
var activeEvents = await GetActiveEventsAsync();

// ❌ DON'T: Use cryptic abbreviations
var ae = await GetAEAsync();

// ✅ DO: Use nullable reference types
public Event? GetEventById(int id)

// ✅ DO: Use async/await for I/O operations
public async Task<List<Event>> GetAllEventsAsync()

// ✅ DO: Validate input
public void RegisterUser(User user)
{
    if (user == null) throw new ArgumentNullException(nameof(user));
    if (string.IsNullOrEmpty(user.Email)) throw new ArgumentException("Email is required");
}

// ✅ DO: Use LINQ effectively
var upcomingEvents = events.Where(e => e.Date > DateTime.Now)
                           .OrderBy(e => e.Date)
                           .Take(10);

// ✅ DO: Handle exceptions properly
try
{
    await _repository.SaveAsync(entity);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save entity");
    throw new ApplicationException("Unable to save changes", ex);
}
```

### Blazor Component Standards

```razor
@* Component: EventCard.razor *@
@inject IEventService EventService

<div class="event-card">
    <h3>@Event.Name</h3>
    <p>@Event.Description</p>
    <button @onclick="RegisterForEvent">Register</button>
</div>

@code {
    [Parameter]
    public Event Event { get; set; } = null!;

    [Parameter]
    public EventCallback OnRegistered { get; set; }

    private async Task RegisterForEvent()
    {
        await EventService.RegisterAsync(Event.Id);
        await OnRegistered.InvokeAsync();
    }
}
```

---

## 💬 Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) specification.

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, semicolons, etc.)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks
- `perf:` - Performance improvements
- `ci:` - CI/CD changes

### Examples

```bash
# Feature
feat(events): add QR code generation for tickets

# Bug fix
fix(registration): resolve duplicate email validation issue

# Documentation
docs(readme): update installation instructions

# Refactoring
refactor(services): extract payment logic to separate service

# Test
test(events): add unit tests for event service

# Multiple lines
feat(waitlist): implement automatic waitlist promotion

- Add background job for waitlist processing
- Send email notifications when spots open
- Update registration status automatically

Closes #123
```

### Commit Best Practices

- ✅ Write in present tense ("add feature" not "added feature")
- ✅ Use imperative mood ("move cursor to..." not "moves cursor to...")
- ✅ Limit first line to 72 characters
- ✅ Reference issues and PRs in the footer
- ✅ Keep commits atomic (one logical change per commit)

---

## 🔍 Pull Request Process

### Before Creating a PR

1. ✅ Ensure your code follows the coding standards
2. ✅ Write/update tests for your changes
3. ✅ Run all tests locally (`dotnet test`)
4. ✅ Update documentation if needed
5. ✅ Rebase on the latest main branch
6. ✅ Ensure CI/CD checks pass

### PR Template

When creating a pull request, include:

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Closes #123

## Changes Made
- Change 1
- Change 2
- Change 3

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Screenshots (if applicable)
[Add screenshots here]

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests pass locally
- [ ] Dependent changes merged
```

### PR Review Process

1. **Automated Checks**
   - CI/CD pipeline must pass
   - Code coverage must not decrease
   - No security vulnerabilities

2. **Peer Review**
   - At least one approval required
   - Address all review comments
   - Update code based on feedback

3. **Merge**
   - Squash and merge (preferred)
   - Rebase and merge
   - Merge commit (for major features)

---

## 🧪 Testing Guidelines

### Test Structure

```
EventEaseApp.Tests/
├── Unit/
│   ├── Services/
│   │   ├── EventServiceTests.cs
│   │   ├── RegistrationServiceTests.cs
│   │   └── PaymentServiceTests.cs
│   └── Helpers/
├── Integration/
│   ├── API/
│   │   ├── EventsControllerTests.cs
│   │   └── RegistrationsControllerTests.cs
│   └── Database/
└── E2E/
    └── RegistrationFlowTests.cs
```

### Unit Test Example

```csharp
using Xunit;
using Moq;
using FluentAssertions;

public class EventServiceTests
{
    [Fact]
    public async Task GetEventById_ValidId_ReturnsEvent()
    {
        // Arrange
        var mockRepo = new Mock<IEventRepository>();
        var expectedEvent = new Event { Id = 1, Name = "Test Event" };
        mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(expectedEvent);
        var service = new EventService(mockRepo.Object);

        // Act
        var result = await service.GetEventByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetEventById_InvalidId_ThrowsException()
    {
        // Arrange
        var mockRepo = new Mock<IEventRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new NotFoundException("Event not found"));
        var service = new EventService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetEventByIdAsync(999));
    }
}
```

### Test Coverage

- **Minimum**: 70% code coverage
- **Target**: 80%+ code coverage
- **Critical paths**: 100% coverage (authentication, payment, registration)

---

## 📚 Documentation

### Code Documentation

```csharp
/// <summary>
/// Registers a user for a specific event
/// </summary>
/// <param name="eventId">The unique identifier of the event</param>
/// <param name="userId">The unique identifier of the user</param>
/// <param name="ticketCount">Number of tickets to register (1-10)</param>
/// <returns>Registration confirmation with QR code</returns>
/// <exception cref="ArgumentException">Thrown when ticket count is invalid</exception>
/// <exception cref="EventFullException">Thrown when event has reached capacity</exception>
public async Task<Registration> RegisterForEventAsync(
    int eventId,
    int userId,
    int ticketCount)
{
    // Implementation
}
```

### Documentation Files

Update these files when applicable:

- **README.md** - Project overview and quick start
- **DEV_SETUP.md** - Development environment setup
- **IMPLEMENTATION_PLAN.md** - Architecture and roadmap
- **API.md** - API documentation (when API is created)
- **CHANGELOG.md** - Version history and changes

---

## 🎯 Feature Development Checklist

When implementing a new feature:

- [ ] Create feature branch from main
- [ ] Review implementation plan for feature requirements
- [ ] Implement core functionality
- [ ] Write unit tests (70%+ coverage)
- [ ] Write integration tests
- [ ] Update/add documentation
- [ ] Test manually in browser
- [ ] Create pull request
- [ ] Address review comments
- [ ] Merge to main

---

## 🐛 Bug Fix Checklist

When fixing a bug:

- [ ] Create fix branch from main
- [ ] Write failing test that reproduces the bug
- [ ] Implement fix
- [ ] Verify test now passes
- [ ] Check for related issues
- [ ] Update CHANGELOG.md
- [ ] Create pull request with "Closes #issue-number"
- [ ] Get review approval
- [ ] Merge to main

---

## ❓ Questions?

If you have questions about contributing:

1. Check existing [documentation](README.md)
2. Search [GitHub Issues](https://github.com/suroweb/EventEaseApp/issues)
3. Create a new issue with the "question" label
4. Reach out on team chat/Slack

---

## 🙏 Thank You!

Your contributions make EventEaseApp better for everyone. We appreciate your time and effort!

---

**Happy Contributing! 🚀**

**Last Updated**: 2025-11-09
**Maintained By**: EventEase Development Team
