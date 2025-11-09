# 🚀 EventEase - Local Development Setup Guide

Complete guide to get EventEaseApp running on your local machine in under 10 minutes.

---

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

| Software | Version | Download Link |
|----------|---------|---------------|
| **.NET SDK** | 10.0 or later | [Download](https://dotnet.microsoft.com/download) |
| **Docker Desktop** | Latest | [Download](https://www.docker.com/products/docker-desktop) |
| **Git** | Latest | [Download](https://git-scm.com/downloads) |

### Optional (Recommended)

| Software | Purpose |
|----------|---------|
| **Visual Studio 2022** | Full IDE experience |
| **VS Code** | Lightweight editor |
| **JetBrains Rider** | Alternative IDE |
| **SQL Server Management Studio** | Database management |
| **Azure Data Studio** | Cross-platform DB tool |
| **Postman** | API testing |

### Verify Installations

```bash
# Check .NET version
dotnet --version
# Expected: 10.0.x or higher

# Check Docker version
docker --version
# Expected: Docker version 20.x or higher

# Check Docker Compose
docker-compose --version
# Expected: Docker Compose version 2.x or higher

# Check Git version
git --version
# Expected: git version 2.x or higher
```

---

## 🎯 Quick Start (5 Minutes)

### Step 1: Clone the Repository

```bash
# Clone the repository
git clone https://github.com/suroweb/EventEaseApp.git

# Navigate to project directory
cd EventEaseApp
```

### Step 2: Configure Environment Variables

```bash
# Copy the environment template
cp .env.example .env

# (Optional) Edit .env file with your preferred editor
# For development, the default values should work fine
```

### Step 3: Start Infrastructure Services

```bash
# Start SQL Server, Redis, Seq, and MailHog
docker-compose up -d

# Verify all services are running
docker-compose ps

# Expected output:
# NAME                  STATUS    PORTS
# eventease-sqlserver   Up        0.0.0.0:1433->1433/tcp
# eventease-redis       Up        0.0.0.0:6379->6379/tcp
# eventease-seq         Up        0.0.0.0:5341->80/tcp
# eventease-mailhog     Up        0.0.0.0:1025->1025/tcp, 0.0.0.0:8025->8025/tcp
```

### Step 4: Run the Application

```bash
# Restore NuGet packages
dotnet restore

# Run the Blazor web application
cd EventEaseApp
dotnet watch run

# Alternative: Run without hot reload
# dotnet run
```

### Step 5: Open in Browser

The application will automatically open in your browser at:
- **Web App**: http://localhost:5040
- **Seq Logs**: http://localhost:5341
- **MailHog**: http://localhost:8025

---

## 🗄️ Database Setup

### Initial Database Creation

The application uses **SQL Server** running in Docker. On first run, the database will be created automatically.

### Manual Database Operations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version

# When API project is ready with migrations:
# Create initial migration
dotnet ef migrations add InitialCreate --project EventEaseApp.Infrastructure --startup-project EventEaseApp.API

# Update database
dotnet ef database update --project EventEaseApp.Infrastructure --startup-project EventEaseApp.API

# Seed sample data
dotnet run --project EventEaseApp.API -- seed
```

### Connect to SQL Server

**Using Azure Data Studio / SSMS:**
- **Server**: localhost,1433
- **Authentication**: SQL Server Authentication
- **Username**: sa
- **Password**: EventEase2025!
- **Trust Server Certificate**: Yes

**Using Command Line (sqlcmd):**
```bash
# Connect to SQL Server (Docker)
docker exec -it eventease-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P EventEase2025!

# Run a test query
> SELECT @@VERSION;
> GO
```

---

## 🔧 Development Workflow

### Running in Development Mode

```bash
# Run with hot reload (recommended for development)
cd EventEaseApp
dotnet watch run

# This will:
# ✅ Auto-reload on file changes
# ✅ Show detailed error pages
# ✅ Enable browser link
# ✅ Use Development appsettings
```

### Project Structure Navigation

```bash
EventEaseApp/
├── EventEaseApp/              # 👉 START HERE - Blazor Server UI
│   ├── Components/Pages/      # Page components
│   ├── Components/Layout/     # Layout components
│   ├── Services/              # Current services (will move to .Core)
│   ├── Models/                # Current models (will move to .Core)
│   └── Program.cs             # Application entry point
│
├── EventEaseApp.API/          # 🚧 FUTURE - REST API (not yet created)
├── EventEaseApp.Core/         # 🚧 FUTURE - Business logic (not yet created)
├── EventEaseApp.Infrastructure/ # 🚧 FUTURE - Data access (not yet created)
├── EventEaseApp.Tests/        # 🚧 FUTURE - Unit tests (not yet created)
│
├── docker-compose.yml         # ✅ Docker services
├── .env.example               # ✅ Environment template
└── DEV_SETUP.md              # ✅ This file
```

### Common Development Tasks

#### 1. **Viewing Logs**

```bash
# View application logs in Seq
# Open: http://localhost:5341

# View Docker container logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f sqlserver
docker-compose logs -f redis
```

#### 2. **Testing Emails**

All emails sent by the application in development are captured by MailHog:
- **SMTP Server**: localhost:1025
- **Web UI**: http://localhost:8025

No emails will actually be sent to real addresses.

#### 3. **Redis Cache Management**

```bash
# Connect to Redis CLI
docker exec -it eventease-redis redis-cli

# View all keys
> KEYS *

# Get a specific key
> GET eventease_registrations

# Clear all cache
> FLUSHALL

# Exit
> EXIT
```

#### 4. **Stopping Services**

```bash
# Stop all Docker services
docker-compose down

# Stop and remove volumes (deletes all data)
docker-compose down -v

# Stop but keep containers for faster restart
docker-compose stop
```

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test EventEaseApp.Tests/

# Run tests in watch mode
dotnet watch test
```

### Writing Tests

```bash
# Create a new test project (example)
dotnet new xunit -n EventEaseApp.Tests
dotnet add EventEaseApp.Tests reference EventEaseApp.Core

# Install testing packages
dotnet add EventEaseApp.Tests package Moq
dotnet add EventEaseApp.Tests package FluentAssertions
```

---

## 🐳 Docker Commands Reference

### Service Management

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Restart a specific service
docker-compose restart sqlserver

# View service status
docker-compose ps

# View resource usage
docker stats
```

### Troubleshooting

```bash
# View logs for debugging
docker-compose logs -f [service-name]

# Rebuild a service
docker-compose up -d --build [service-name]

# Remove all stopped containers
docker-compose rm

# Prune unused Docker resources
docker system prune -a
```

---

## 🔐 Security Notes for Development

### Default Credentials (CHANGE IN PRODUCTION)

| Service | Username | Password |
|---------|----------|----------|
| **SQL Server** | sa | EventEase2025! |
| **Seq** | N/A | No password (open access) |
| **MailHog** | N/A | No authentication |
| **Redis** | N/A | No password |

⚠️ **IMPORTANT**: These credentials are for **local development only**. Never use these in production!

### .env File Security

```bash
# Make sure .env is in .gitignore
echo ".env" >> .gitignore

# Never commit .env to version control
git rm --cached .env  # If accidentally committed
```

---

## 🚨 Troubleshooting

### Problem: Port Already in Use

**Error**: `port 1433 is already allocated`

**Solution**:
```bash
# Option 1: Stop the conflicting service
# For SQL Server on Windows:
net stop MSSQLSERVER

# Option 2: Change the port in docker-compose.yml
# Edit the ports section:
ports:
  - "1434:1433"  # Use 1434 instead
```

### Problem: Docker Services Won't Start

**Error**: `Cannot connect to the Docker daemon`

**Solution**:
```bash
# Make sure Docker Desktop is running
# On Windows: Check system tray
# On Mac: Check menu bar

# Restart Docker Desktop

# Verify Docker is running
docker ps
```

### Problem: Database Connection Failed

**Error**: `A network-related or instance-specific error occurred while establishing a connection to SQL Server`

**Solution**:
```bash
# 1. Check if SQL Server container is running
docker ps | grep sqlserver

# 2. Check container health
docker inspect eventease-sqlserver | grep Health

# 3. Check container logs
docker logs eventease-sqlserver

# 4. Restart the container
docker-compose restart sqlserver

# 5. Wait for SQL Server to be ready (10-20 seconds)
```

### Problem: Application Won't Start

**Error**: Various startup errors

**Solution**:
```bash
# 1. Clean the solution
dotnet clean

# 2. Restore packages
dotnet restore

# 3. Rebuild
dotnet build

# 4. Check for port conflicts
netstat -an | findstr :5040  # Windows
lsof -i :5040                # Mac/Linux

# 5. Check environment variables
cat .env

# 6. View detailed error messages
dotnet run --verbosity detailed
```

### Problem: Hot Reload Not Working

**Solution**:
```bash
# Make sure you're using watch
dotnet watch run

# If still not working, restart with clean build
dotnet clean
dotnet watch run
```

---

## 📚 Additional Resources

### Official Documentation
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Docker Documentation](https://docs.docker.com/)

### Project Documentation
- [Implementation Plan](IMPLEMENTATION_PLAN.md) - Full feature roadmap
- [README](README.md) - Project overview
- [AI Development Summary](AI_DEVELOPMENT_SUMMARY.md) - Development history

### Community
- [.NET Discord](https://discord.gg/dotnet)
- [Stack Overflow - .NET](https://stackoverflow.com/questions/tagged/.net)
- [GitHub Discussions](https://github.com/suroweb/EventEaseApp/discussions)

---

## 🎓 Learning Path for New Developers

### Week 1: Understanding the Basics
1. ✅ Get the project running locally
2. ✅ Explore the UI and register for an event
3. ✅ Browse through the codebase structure
4. ✅ Read the existing README and documentation

### Week 2: Making Your First Change
1. ✅ Make a small UI change (e.g., button color)
2. ✅ Add a new property to an existing model
3. ✅ Create a simple new page
4. ✅ Write your first unit test

### Week 3: Understanding the Architecture
1. ✅ Study the service layer
2. ✅ Understand LocalStorage persistence
3. ✅ Learn about Blazor component lifecycle
4. ✅ Review the implementation plan for future features

### Week 4: Contributing
1. ✅ Pick a small feature from the implementation plan
2. ✅ Create a feature branch
3. ✅ Implement the feature
4. ✅ Write tests
5. ✅ Submit a pull request

---

## 🤝 Getting Help

### Stuck? Here's How to Get Assistance

1. **Check Documentation First**
   - Review this setup guide
   - Check the troubleshooting section
   - Read the implementation plan

2. **Search Existing Issues**
   - GitHub Issues: [Project Issues](https://github.com/suroweb/EventEaseApp/issues)

3. **Ask for Help**
   - Create a new GitHub issue with:
     - Descriptive title
     - Steps to reproduce
     - Expected vs actual behavior
     - Environment details (OS, .NET version, etc.)
     - Relevant logs or error messages

4. **Connect with the Team**
   - Team chat/Slack
   - Email: [Your Team Email]
   - Weekly office hours: [Schedule]

---

## ✅ Development Checklist

Before starting development, ensure:

- [ ] Docker Desktop is running
- [ ] All containers are healthy (`docker-compose ps`)
- [ ] .NET SDK 10+ is installed (`dotnet --version`)
- [ ] Application starts without errors (`dotnet run`)
- [ ] Can access the web app (http://localhost:5040)
- [ ] Can access Seq logs (http://localhost:5341)
- [ ] Can access MailHog (http://localhost:8025)
- [ ] Database connection works
- [ ] Redis connection works
- [ ] Code editor is configured (VS/VSCode/Rider)
- [ ] Git is configured with your credentials

---

## 🎉 You're Ready!

If you've completed all the steps above, you're ready to start developing EventEase!

### Next Steps

1. ✅ Explore the application UI
2. ✅ Review the [Implementation Plan](IMPLEMENTATION_PLAN.md)
3. ✅ Pick a feature to work on
4. ✅ Start coding!

**Happy Coding! 🚀**

---

**Last Updated**: 2025-11-09
**Maintained By**: EventEase Development Team
**Questions?**: Create an issue on GitHub
