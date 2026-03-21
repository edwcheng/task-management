using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure port - always use 5000 for Railway private networking
// Ignore Railway's PORT variable to ensure consistent port for internal communication
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow string enums instead of integers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=taskmanager;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IReplyService, ReplyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

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
        logger.LogInformation("Starting database initialization...");
        
        // Get connection string and create database if it doesn't exist
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=taskmanager;Username=postgres;Password=postgres";
        
        // Parse connection string to get database name and server info
        var builder2 = new Npgsql.NpgsqlConnectionStringBuilder(dbConnectionString);
        var dbName = builder2.Database;
        builder2.Database = "postgres"; // Connect to default database first
        
        using (var connection = new Npgsql.NpgsqlConnection(builder2.ToString()))
        {
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
            var exists = await cmd.ExecuteScalarAsync();
            
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
        
        // Now create tables using EF Core
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("Database tables created/verified successfully");
        
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization error: {Message}", ex.Message);
        // Don't throw - let the app start anyway so we can see logs
    }
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
