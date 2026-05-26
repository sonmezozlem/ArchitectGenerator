using ArchitectGenerator.Core;

namespace ArchitectGenerator.Templates;

public static class BaseTemplates
{
    public static string BaseEntity() =>
"""
namespace Base.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedDate { get; set; }
    public long CreatedById { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public long? UpdatedById { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }
    public long? DeletedById { get; set; }
}
""";

    public static string UserRole() =>
"""
namespace Base.Domain.Enums;

public enum UserRole : byte
{
    Admin = 1,
    Employee = 2
}
""";

    public static string User() =>
"""
using Base.Domain.Entities;
using Base.Domain.Enums;
namespace Base.Domain.Identity;

public class User : BaseEntity
{
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
}
""";

    public static string RefreshToken() =>
"""
using Base.Domain.Entities;

namespace Base.Domain.Identity;

public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public string TokenSalt { get; set; } = null!;
    public DateTimeOffset Expiration { get; set; }
}
""";

    public static string IRepository() =>
"""
using Base.Domain.Entities;
using System.Linq.Expressions;

namespace Base.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task DeleteByIdAsync(long id);
    Task<T?> GetByIdAsync(long id, params Expression<Func<T, object>>[] includes);
    Task<T?> GetByIdAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, params Expression<Func<T, object>>[] includes);
    Task<List<T>> Where(Expression<Func<T, bool>> predicate, params string[] includes);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
""";

    public static string IUnitOfWork() =>
"""
using Base.Domain.Entities;

namespace Base.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> CommitAsync();
    ITransaction BeginTransaction();
}
""";

    public static string ITransaction() =>
"""
namespace Base.Domain.Interfaces;

public interface ITransaction : IDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
""";

    public static string ValidationBehavior() =>
"""
using FluentValidation;
using MediatR;

namespace Base.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
""";

    public static string IUserContextService() =>
"""
namespace Base.Application.Interfaces;

public interface IUserContextService
{
    long? GetCurrentUserId();
    string? GetCurrentUserRole();
    long? GetCurrentEmployeeId();
}
""";

    public static string IJwtService() =>
"""
using Base.Domain.Identity;

namespace Base.Application.Interfaces;

public interface IJwtService
{
    Task<string> GenerateToken(User user);
}
""";

    public static string IPasswordHasher() =>
"""
namespace Base.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}
""";

    public static string IRefreshTokenGenerator() =>
"""
namespace Base.Application.Interfaces;

public interface IRefreshTokenGenerator
{
    string GenerateToken();
    (string Hash, string Salt) HashToken(string token);
}
""";

    public static string IRedisService() =>
"""
namespace Base.Application.Interfaces;

public interface IRedisService
{
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetStringAsync(string key);
    Task DeleteKeyAsync(string key);
    Task<bool> ExistsAsync(string key);
}
""";

    public static string JwtSettings() =>
"""
namespace Base.Application.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpirationInMinutes { get; set; }
}
""";

    public static string BaseApplicationConfigureServices() =>
"""
using Base.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Base.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddBaseApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly);
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
""";

    public static string PasswordHasher() =>
"""
using Base.Application.Interfaces;

namespace Base.Infrastructure.Services.Auth;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
""";

    public static string UserContextService() =>
"""
using Base.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Base.Infrastructure.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long? GetCurrentUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }

    public string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
    }

    public long? GetCurrentEmployeeId()
    {
        var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("EmployeeId");
        return long.TryParse(value, out var id) ? id : null;
    }
}
""";

    public static string GlobalExceptionMiddleware() =>
"""
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Base.Infrastructure.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                ValidationException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
""";

    public static string ExceptionMiddlewareExtensions() =>
"""
using Microsoft.AspNetCore.Builder;

namespace Base.Infrastructure.Middlewares;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
""";

	public static string BaseInfrastructureConfigureServices() =>
	"""
using Base.Application.Interfaces;
using Base.Application.Settings;
using Base.Infrastructure.Services;
using Base.Infrastructure.Services.Auth;
using Base.Infrastructure.Services.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Base.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddBaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration.GetValue<string>("RedisConnection") ?? "localhost:6379"));
        services.AddScoped<IRedisService, RedisService>();

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 60;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 5;
                opt.QueueLimit = 0;
            });

            options.RejectionStatusCode = 429;
        });

        return services;
    }
}
""";

	public static string IModelConfigurator() =>
"""
using Microsoft.EntityFrameworkCore;

namespace Base.Persistence.Interfaces;

public interface IModelConfigurator
{
    void Configure(ModelBuilder modelBuilder);
}
""";

    public static string AppDbContext() =>
"""
using Base.Domain.Identity;
using Base.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Base.Persistence.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly IEnumerable<IModelConfigurator> _configurators;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEnumerable<IModelConfigurator> configurators)
        : base(options)
    {
        _configurators = configurators;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var configurator in _configurators)
        {
            configurator.Configure(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }
}
""";

    public static string BaseEntityConfiguration() =>
"""
using Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.Persistence.Configurations.Base;

public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();
        builder.Property(x => x.CreatedById).IsRequired();
        builder.Property(x => x.UpdatedDate).IsRequired(false);
        builder.Property(x => x.UpdatedById).IsRequired(false);
        builder.Property(x => x.DeletedDate).IsRequired(false);
        builder.Property(x => x.DeletedById).IsRequired(false);
    }
}
""";

	public static string UserConfiguration() =>
	"""
using Base.Domain.Identity;
using Base.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.Persistence.Configurations.Identity;

public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "Base");
        base.Configure(builder);

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<byte>();
    }
}
""";

	public static string RefreshTokenConfiguration() =>
"""
using Base.Domain.Identity;
using Base.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "Base");
        base.Configure(builder);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TokenSalt)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Expiration)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
""";

    public static string GenericRepository() =>
"""
using Base.Application.Interfaces;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Base.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;
    private readonly IUserContextService _userContextService;

    public GenericRepository(AppDbContext dbContext, IUserContextService userContextService)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
        _userContextService = userContextService;
    }

    public async Task<T> AddAsync(T entity)
    {
        entity.CreatedDate = DateTimeOffset.UtcNow;
        entity.CreatedById = _userContextService.GetCurrentUserId() ?? 0;
        entity.IsActive = true;

        await DbSet.AddAsync(entity);
        return entity;
    }

    public void Update(T entity)
    {
        if (entity.Id <= 0)
            throw new ArgumentException("ID bulunamadı.");

        if (!entity.IsActive)
            throw new InvalidOperationException("Pasif kayıt güncellenemez.");

        entity.UpdatedDate = DateTimeOffset.UtcNow;
        entity.UpdatedById = _userContextService.GetCurrentUserId();

        DbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        if (!entity.IsActive)
            return;

        entity.IsActive = false;
        entity.DeletedDate = DateTimeOffset.UtcNow;
        entity.DeletedById = _userContextService.GetCurrentUserId();

        DbSet.Update(entity);
    }

    public async Task DeleteByIdAsync(long id)
    {
        if (id <= 0)
            throw new ArgumentException("ID bulunamadı.");

        var entity = await DbSet.FindAsync(id);

        if (entity == null)
            throw new KeyNotFoundException("Silinecek kayıt bulunamadı.");

        Delete(entity);
    }

    public async Task<T?> GetByIdAsync(long id, params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<T?> GetByIdAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter, params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(filter);
    }

    public async Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter = null, params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public async Task<List<T>> Where(System.Linq.Expressions.Expression<Func<T, bool>> predicate, params string[] includes)
    {
        IQueryable<T> query = DbSet.Where(predicate);

        query = includes.Aggregate(query, (current, include) => current.Include(include));

        return await query.ToListAsync();
    }

    public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }
}
""";

    public static string EfTransaction() =>
"""
using Base.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Base.Persistence.UnitOfWork;

public class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }
}
""";

    public static string UnitOfWork() =>
"""
using Base.Application.Interfaces;
using Base.Domain.Entities;
using Base.Domain.Interfaces;
using Base.Persistence.DbContext;
using Base.Persistence.Repositories;

namespace Base.Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);

        if (!_repositories.ContainsKey(type))
        {
            var repository = new GenericRepository<T>(_context, _userContextService);
            _repositories[type] = repository;
        }

        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public ITransaction BeginTransaction()
    {
        var transaction = _context.Database.BeginTransaction();
        return new EfTransaction(transaction);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
""";

	public static string BasePersistenceConfigureServices(DatabaseProvider provider) =>
$$"""
using Base.Application.Interfaces;
using Base.Domain.Interfaces;
using Base.Persistence.DbContext;
using Base.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Base.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection AddBasePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            {{DatabaseProviderInfo.UseDbCall(provider)}});

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork>(sp =>
            new Base.Persistence.UnitOfWork.UnitOfWork(
                sp.GetRequiredService<AppDbContext>(),
                sp.GetRequiredService<IUserContextService>()));

        return services;
    }
}
""";

	public static string ProgramCs() =>
	"""
using Base.Application;
using Base.Infrastructure;
using Base.Infrastructure.Middlewares;
using Base.Persistence;
using Microsoft.AspNetCore.RateLimiting;
// <ARCHITECT_GEN_MODULES_USING_START>
// <ARCHITECT_GEN_MODULES_USING_END>

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy
            .WithOrigins(builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddBaseInfrastructure(builder.Configuration);
builder.Services.AddBaseApplication();
builder.Services.AddBasePersistence(builder.Configuration);
// <ARCHITECT_GEN_MODULES_DI_START>
// <ARCHITECT_GEN_MODULES_DI_END>

var app = builder.Build();

app.UseGlobalExceptionMiddleware();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<JwtBlacklistMiddleware>();

app.UseCors("DefaultPolicy");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
""";

	public static string AppSettings(string solutionName, DatabaseProvider provider) =>
    $$"""
{
  "ConnectionStrings": {
    "DefaultConnection": "{{DatabaseProviderInfo.ConnectionString(provider, solutionName)}}"
  },
  "JwtSettings": {
    "SecretKey": "YOUR_SUPER_SECRET_KEY_HERE",
    "Issuer": "{{solutionName}}",
    "Audience": "{{solutionName}}Users",
    "ExpirationInMinutes": 60
  },
  "RedisConnection": "localhost:6379",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
""";

	public static string JwtService() =>
"""
using Base.Application.Interfaces;
using Base.Application.Settings;
using Base.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Base.Infrastructure.Services.Auth;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public Task<string> GenerateToken(User user)
    {
        var jti = Guid.NewGuid().ToString();
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }
}
""";

	public static string RefreshTokenGenerator() =>
	"""
using Base.Application.Interfaces;
using System.Security.Cryptography;

namespace Base.Infrastructure.Services.Auth;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string GenerateToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public (string Hash, string Salt) HashToken(string token)
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            token,
            salt,
            100_000,
            HashAlgorithmName.SHA256);

        var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
        var saltStr = Convert.ToBase64String(salt);

        return (hash, saltStr);
    }
}
""";

	public static string RedisService() =>
	"""
using Base.Application.Interfaces;
using StackExchange.Redis;

namespace Base.Infrastructure.Services.Redis;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
{
    if (expiry.HasValue)
        await _db.StringSetAsync(key, value, expiry.Value);
    else
        await _db.StringSetAsync(key, value);
}

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteKeyAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }
}
""";

	public static string SecurityHeadersMiddleware() =>
	"""
using Microsoft.AspNetCore.Http;

namespace Base.Infrastructure.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}
""";

	public static string RequestLoggingMiddleware() =>
	"""
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Base.Infrastructure.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} {StatusCode} {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
""";

	public static string JwtBlacklistMiddleware() =>
	"""
using Base.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace Base.Infrastructure.Middlewares;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRedisService redisService)
    {
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            var jti = GetJti(token);
            if (jti != null && await redisService.ExistsAsync($"blacklist:{jti}"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token geçersiz kılınmış.");
                return;
            }
        }

        await _next(context);
    }

    private static string? GetJti(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Id;
        }
        catch
        {
            return null;
        }
    }
}
""";

	public static string ApiControllerBase() =>
	"""
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Controllers.Base;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
""";
}