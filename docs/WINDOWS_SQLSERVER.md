# Windows & SQL Server Compatibility Guide

This document covers known issues and solutions when running the Task Manager application on Windows and/or with SQL Server instead of the default PostgreSQL.

## Project Design

This application is designed for **Linux-first deployment**:
- Backend: .NET Core 8.0 with PostgreSQL
- Frontend: Vue.js 3 with Vite
- Containerization: Docker with docker-compose

However, you may encounter scenarios where Windows and/or SQL Server are required.

---

## Running on Windows

### Issue 1: Line Ending Problems (CRLF vs LF)

**Problem**: Windows uses CRLF (`\r\n`) line endings while Linux uses LF (`\n`). This causes shell scripts to fail with errors like:
```
/bin/sh^M: bad interpreter: No such file or directory
```

**Solution**: Configure Git to handle line endings properly:

```bash
# In the repository root, create or edit .gitattributes
* text=auto
*.sh text eol=lf
*.cs text eol=crlf
*.json text eol=crlf
*.ts text eol=crlf
*.vue text eol=crlf
*.md text eol=crlf
```

Then normalize the repository:
```bash
git add --renormalize .
git commit -m "Fix line endings"
```

### Issue 2: Docker Volume Mount Paths

**Problem**: Windows uses backslashes for paths, while Docker expects forward slashes.

**Solution**: Always use forward slashes in docker-compose.yml:
```yaml
# Correct
volumes:
  - ./backend:/app

# Incorrect on Windows
volumes:
  - .\backend:/app
```

### Issue 3: Docker Performance on Windows

**Problem**: Docker Desktop on Windows has slower file I/O due to filesystem translation between NTFS and Linux filesystem.

**Solution**: 
- Enable WSL 2 backend in Docker Desktop
- Store the project in WSL 2 filesystem (e.g., `\\wsl$\Ubuntu\home\user\project`)
- Use named volumes instead of bind mounts for better performance

### Issue 4: Port Conflicts on Windows

**Problem**: Windows may have services already using ports 80, 443, or 5000.

**Solutions**:
- Stop IIS: `net stop was /y`
- Stop other services using the ports
- Modify docker-compose.yml to use different ports:
```yaml
services:
  frontend:
    ports:
      - "8080:80"  # Use 8080 instead of 80
```

### Issue 5: Firewall Blocking Docker

**Problem**: Windows Firewall may block Docker network traffic.

**Solution**: Allow Docker through Windows Firewall:
1. Open Windows Defender Firewall
2. Click "Allow an app through firewall"
3. Find "Docker Desktop" and ensure both Private and Public are checked

### Issue 6: Environment Variables in PowerShell

**Problem**: Setting environment variables differs between PowerShell and bash.

**Bash**:
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=taskmanager;Username=postgres;Password=postgres"
```

**PowerShell**:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=taskmanager;Username=postgres;Password=postgres"
```

**Command Prompt (cmd)**:
```cmd
set ConnectionStrings__DefaultConnection=Host=localhost;Database=taskmanager;Username=postgres;Password=postgres
```

---

## Using SQL Server Instead of PostgreSQL

### Issue 1: Different EF Core Provider

**Problem**: The project uses Npgsql for PostgreSQL. SQL Server requires a different provider.

**Solution**: Modify `TaskManagerAPI.csproj`:

```xml
<!-- Remove PostgreSQL provider -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />

<!-- Add SQL Server provider -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

Then update `Program.cs`:
```csharp
// PostgreSQL (original)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// SQL Server (modified)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### Issue 2: Connection String Format

**Problem**: SQL Server connection strings differ from PostgreSQL.

**PostgreSQL**:
```
Host=localhost;Database=taskmanager;Username=postgres;Password=postgres
```

**SQL Server**:
```
Server=localhost,1433;Database=taskmanager;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True
```

### Issue 3: Database Creation Logic

**Problem**: The database auto-creation code uses Npgsql-specific commands.

**Original (PostgreSQL)**:
```csharp
var builder2 = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
var dbName = builder2.Database;
builder2.Database = "postgres"; // Connect to default database

using var connection = new Npgsql.NpgsqlConnection(builder2.ToString());
await connection.OpenAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
```

**Modified (SQL Server)**:
```csharp
var builder2 = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
var dbName = builder2.InitialCatalog;
builder2.InitialCatalog = "master"; // Connect to master database

using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder2.ToString());
await connection.OpenAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = $"SELECT 1 FROM sys.databases WHERE name = '{dbName}'";
```

**Note**: You also need to add the `Microsoft.Data.SqlClient` NuGet package.

### Issue 4: JSON/JSONB Data Types

**Problem**: PostgreSQL has native JSONB support. SQL Server uses `NVARCHAR(MAX)` with `JSON_*` functions.

**Solution**: If your models use JSON columns, modify the DbContext:
```csharp
// PostgreSQL
modelBuilder.Entity<TaskItem>()
    .Property(t => t.Metadata)
    .HasColumnType("jsonb");

// SQL Server
modelBuilder.Entity<TaskItem>()
    .Property(t => t.Metadata)
    .HasColumnType("nvarchar(max)");
```

### Issue 5: Case Sensitivity

**Problem**: PostgreSQL is case-sensitive for strings by default. SQL Server's default collation is case-insensitive.

**Impact**: Queries that rely on case-sensitive comparisons will behave differently.

**Solution**: Be explicit in your queries or use case-insensitive comparisons:
```csharp
// Case-insensitive comparison (works on both)
var user = await dbContext.Users
    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
```

### Issue 6: String Length Limits

**Problem**: SQL Server has an 8000-byte limit for indexed NVARCHAR columns. PostgreSQL's `text` type has no such limit.

**Solution**: Specify explicit lengths for indexed string columns:
```csharp
modelBuilder.Entity<User>()
    .Property(u => u.Username)
    .HasMaxLength(256)
    .IsRequired();
```

### Issue 7: Docker SQL Server Setup

**Problem**: Running SQL Server in Docker requires different configuration than PostgreSQL.

**PostgreSQL docker-compose.yml**:
```yaml
postgres:
  image: postgres:16-alpine
  environment:
    POSTGRES_DB: taskmanager
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
  ports:
    - "5432:5432"
```

**SQL Server docker-compose.yml**:
```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    ACCEPT_EULA: "Y"
    SA_PASSWORD: "YourStrong@Password123"
  ports:
    - "1433:1433"
```

**Important**: SQL Server container requires:
- Accept EULA by setting `ACCEPT_EULA=Y`
- Strong password (minimum 8 characters, uppercase, lowercase, digits, special characters)
- At least 2GB RAM allocated to Docker

---

## Quick Reference: Platform Differences

| Feature | PostgreSQL | SQL Server |
|---------|------------|------------|
| Default Port | 5432 | 1433 |
| Default User | postgres | sa |
| Connection String Key | Host | Server |
| Database Property | Database | Initial Catalog |
| JSON Type | jsonb | nvarchar(max) |
| Case Sensitivity | Yes (default) | No (default) |
| Auto-increment | SERIAL | IDENTITY |
| Boolean Type | boolean | bit |
| String No Limit | text | nvarchar(max) |

---

## Troubleshooting Checklist

### Windows Issues
- [ ] Git line endings configured (.gitattributes)
- [ ] Docker Desktop running with WSL 2
- [ ] Ports not blocked by Windows services
- [ ] Firewall allows Docker
- [ ] Environment variables set correctly for PowerShell/cmd

### SQL Server Issues
- [ ] NuGet packages updated (Microsoft.EntityFrameworkCore.SqlServer)
- [ ] Connection string format correct
- [ ] Database creation code updated
- [ ] JSON columns changed to nvarchar(max)
- [ ] Docker container has enough memory (2GB+)
- [ ] SA_PASSWORD meets complexity requirements
