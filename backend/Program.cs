using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services;

Console.WriteLine("=== Application Starting ===");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== Building configuration ===");

// Configure port - always use 5000 for Railway private networking
// Ignore Railway's PORT variable to ensure consistent port for internal communication
builder.WebHost.ConfigureKestrel(options =>
{
    Console.WriteLine("=== Configuring Kestrel on port 5000 ===");
    options.ListenAnyIP(5000);
});

Console.WriteLine("=== Adding controllers ===");

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow string enums instead of integers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

Console.WriteLine("=== Configuring PostgreSQL ===");

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=taskmanager;Username=postgres;Password=postgres";

Console.WriteLine($"=== Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}... ===");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

Console.WriteLine("=== Registering services ===");

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IReplyService, ReplyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

Console.WriteLine("=== Configuring JWT ===");

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "DefaultSecretKeyForDevelopment12345678!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskManagerAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TaskManagerAPI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Manager API",
        Version = "v1",
        Description = "A task management web API with forum-like functionality"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Health check endpoint for Railway
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Auto-create database and tables on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    try
    {
        logger.LogInformation("=== STEP 1: Starting database initialization ===");
        Console.WriteLine("=== STEP 1: Starting database initialization ===");
        
        // Get connection string and create database if it doesn't exist
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=taskmanager;Username=postgres;Password=postgres";
        
        logger.LogInformation("=== STEP 2: Got connection string ===");
        Console.WriteLine("=== STEP 2: Got connection string ===");
        
        // Parse connection string to get database name and server info
        var builder2 = new Npgsql.NpgsqlConnectionStringBuilder(dbConnectionString);
        var dbName = builder2.Database;
        builder2.Database = "postgres"; // Connect to default database first
        
        logger.LogInformation("=== STEP 3: Connecting to postgres database ===");
        Console.WriteLine("=== STEP 3: Connecting to postgres database ===");
        
        using (var connection = new Npgsql.NpgsqlConnection(builder2.ToString()))
        {
            await connection.OpenAsync();
            logger.LogInformation("=== STEP 4: Connection opened ===");
            Console.WriteLine("=== STEP 4: Connection opened ===");
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
            var exists = await cmd.ExecuteScalarAsync();
            
            logger.LogInformation("=== STEP 5: Database existence checked ===");
            Console.WriteLine($"=== STEP 5: Database existence checked, exists={exists} ===");
            
            if (exists == null)
            {
                logger.LogInformation("Creating database '{DbName}'...", dbName);
                cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
                await cmd.ExecuteNonQueryAsync();
                logger.LogInformation("Database '{DbName}' created successfully", dbName);
            }
            else
            {
                logger.LogInformation("Database '{DbName}' already exists", dbName);
            }
        }
        
        logger.LogInformation("=== STEP 6: Calling EnsureCreatedAsync ===");
        Console.WriteLine("=== STEP 6: Calling EnsureCreatedAsync ===");
        
        // Now create tables using EF Core
        await dbContext.Database.EnsureCreatedAsync();
        
        logger.LogInformation("=== STEP 7: EnsureCreatedAsync completed ===");
        Console.WriteLine("=== STEP 7: EnsureCreatedAsync completed ===");
        
        // Seed admin user if not exists
        if (!await dbContext.Users.AnyAsync(u => u.Username == "admin"))
        {
            logger.LogInformation("Seeding admin user...");
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Email = "admin@taskmanager.com",
                DisplayName = "Administrator",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Admin user seeded successfully (username: admin, password: Admin@123)");
        }
        else
        {
            logger.LogInformation("Admin user already exists, skipping seed");
        }
        
        logger.LogInformation("=== STEP 8: Database initialization complete ===");
        Console.WriteLine("=== STEP 8: Database initialization complete ===");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization error: {Message}", ex.Message);
        Console.WriteLine($"=== DATABASE ERROR: {ex.Message} ===");
        Console.WriteLine($"=== STACK TRACE: {ex.StackTrace} ===");
        // Don't throw - let the app start anyway so we can see logs
    }
}

Console.WriteLine("=== STEP 9: Configuring middleware ===");

app.UseCors("AllowAll");

Console.WriteLine("=== STEP 10: CORS configured ===");

app.UseAuthentication();
app.UseAuthorization();

Console.WriteLine("=== STEP 11: Auth configured ===");

app.MapControllers();

Console.WriteLine("=== STEP 12: Controllers mapped ===");
Console.WriteLine("=== Application starting - listening on port 5000 ===");

app.Run();
