using ArchitectGenerator.Core;

namespace ArchitectGenerator.Templates;

public static partial class BaseTemplates
{
	// ======================= Auth / Login =======================

	public static string LoginRequest() =>
"""
namespace Base.Application.Features.Auth.Logins.Models;

public class LoginRequest
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}
""";

	public static string LoginResponse() =>
"""
namespace Base.Application.Features.Auth.Logins.Models;

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public DateTime Expiration { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTimeOffset RefreshTokenExpiration { get; set; }
}
""";

	public static string LoginCommand() =>
"""
using Base.Application.Features.Auth.Logins.Models;
using MediatR;

namespace Base.Application.Features.Auth.Logins.Commands;

public class LoginCommand : IRequest<LoginResponse>
{
    public LoginRequest Request { get; }

    public LoginCommand(LoginRequest request)
    {
        Request = request;
    }
}
""";

	public static string LoginCommandHandler() =>
"""
using Base.Application.Common.Exceptions;
using Base.Application.Features.Auth.Logins.Models;
using Base.Application.Interfaces;
using Base.Domain.Identity;
using Base.Domain.Interfaces;
using MediatR;

namespace Base.Application.Features.Auth.Logins.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var user = (await _unitOfWork.Repository<User>()
            .Where(u => u.UserName == dto.UserName && u.IsActive))
            .FirstOrDefault();

        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var (accessToken, expiresAt) = await _jwtService.GenerateToken(user);

        // Mevcut aktif refresh token'ları iptal et (rotation)
        var activeTokens = await _unitOfWork.Repository<RefreshToken>()
            .GetAllAsync(x => x.UserId == user.Id && x.IsActive);

        foreach (var old in activeTokens)
            _unitOfWork.Repository<RefreshToken>().Delete(old);

        var (token, selector, verifierHash) = _refreshTokenGenerator.Generate();
        var refreshExpiration = DateTimeOffset.UtcNow.AddDays(7);

        await _unitOfWork.Repository<RefreshToken>().AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Selector = selector,
            VerifierHash = verifierHash,
            Expiration = refreshExpiration
        });

        await _unitOfWork.CommitAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            Expiration = expiresAt,
            RefreshToken = token,
            RefreshTokenExpiration = refreshExpiration
        };
    }
}
""";

	public static string LoginCommandValidator() =>
"""
using Base.Application.Features.Auth.Logins.Commands;
using FluentValidation;

namespace Base.Application.Features.Auth.Logins.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı boş olamaz.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.");
    }
}
""";

	// ======================= Auth / Logout =======================

	public static string LogoutCommand() =>
"""
using MediatR;

namespace Base.Application.Features.Auth.Logouts.Commands;

public class LogoutCommand : IRequest<Unit>
{
}
""";

	public static string LogoutCommandHandler() =>
"""
using Base.Application.Interfaces;
using Base.Domain.Identity;
using Base.Domain.Interfaces;
using MediatR;

namespace Base.Application.Features.Auth.Logouts.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRedisService _redisService;
    private readonly IUserContextService _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRedisService redisService,
        IUserContextService userContext,
        IUnitOfWork unitOfWork)
    {
        _redisService = redisService;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var jti = _userContext.GetCurrentJti();
        var expiration = _userContext.GetCurrentTokenExpiration();
        var userId = _userContext.GetCurrentUserId();

        if (string.IsNullOrEmpty(jti))
            throw new UnauthorizedAccessException("Geçerli bir oturum bulunamadı.");

        // Access token'ı kalan ömrü kadar blacklist'e ekle (JwtBlacklistMiddleware kontrol eder)
        var ttl = (expiration ?? DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero)
            await _redisService.SetStringAsync($"blacklist:{jti}", "1", ttl);

        // Kullanıcının refresh token'larını iptal et
        if (userId.HasValue)
        {
            var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                .GetAllAsync(x => x.UserId == userId.Value && x.IsActive);

            foreach (var token in activeTokens)
                _unitOfWork.Repository<RefreshToken>().Delete(token);

            if (activeTokens.Count > 0)
                await _unitOfWork.CommitAsync();
        }

        return Unit.Value;
    }
}
""";

	// ======================= Auth / RefreshToken =======================

	public static string RefreshTokenRequest() =>
"""
namespace Base.Application.Features.Auth.RefreshTokens.Models;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}
""";

	public static string RefreshTokenResponse() =>
"""
namespace Base.Application.Features.Auth.RefreshTokens.Models;

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = null!;
    public DateTime Expiration { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTimeOffset RefreshTokenExpiration { get; set; }
}
""";

	public static string RefreshTokenCommand() =>
"""
using Base.Application.Features.Auth.RefreshTokens.Models;
using MediatR;

namespace Base.Application.Features.Auth.RefreshTokens.Commands;

public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public RefreshTokenRequest Request { get; }

    public RefreshTokenCommand(RefreshTokenRequest request)
    {
        Request = request;
    }
}
""";

	public static string RefreshTokenCommandHandler() =>
"""
using Base.Application.Features.Auth.RefreshTokens.Models;
using Base.Application.Interfaces;
using Base.Domain.Identity;
using Base.Domain.Interfaces;
using MediatR;

namespace Base.Application.Features.Auth.RefreshTokens.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (!_refreshTokenGenerator.TryParse(request.Request.RefreshToken, out var selector, out var verifier))
            throw new UnauthorizedAccessException("Geçersiz refresh token.");

        // Selector indexli olduğu için arama hızlı; sır (verifier) sabit zamanlı doğrulanır.
        var stored = (await _unitOfWork.Repository<RefreshToken>()
            .Where(x => x.Selector == selector && x.IsActive))
            .FirstOrDefault();

        if (stored is null
            || stored.Expiration <= DateTimeOffset.UtcNow
            || !_refreshTokenGenerator.Verify(verifier, stored.VerifierHash))
        {
            throw new UnauthorizedAccessException("Geçersiz refresh token.");
        }

        // Eski token'ı iptal et (rotation)
        _unitOfWork.Repository<RefreshToken>().Delete(stored);

        var (newToken, newSelector, newVerifierHash) = _refreshTokenGenerator.Generate();
        var newExpiration = DateTimeOffset.UtcNow.AddDays(7);

        await _unitOfWork.Repository<RefreshToken>().AddAsync(new RefreshToken
        {
            UserId = stored.UserId,
            Selector = newSelector,
            VerifierHash = newVerifierHash,
            Expiration = newExpiration
        });

        await _unitOfWork.CommitAsync();

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

        var (accessToken, expiresAt) = await _jwtService.GenerateToken(user);

        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            Expiration = expiresAt,
            RefreshToken = newToken,
            RefreshTokenExpiration = newExpiration
        };
    }
}
""";

	public static string RefreshTokenCommandValidator() =>
"""
using Base.Application.Features.Auth.RefreshTokens.Commands;
using FluentValidation;

namespace Base.Application.Features.Auth.RefreshTokens.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Request.RefreshToken)
            .NotEmpty().WithMessage("Refresh token boş olamaz.");
    }
}
""";

	// ======================= Auth / CreateUser (admin-only) =======================

	public static string CreateUserRequest() =>
"""
using Base.Domain.Enums;

namespace Base.Application.Features.Auth.Users.Models;

public class CreateUserRequest
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserRole Role { get; set; }
}
""";

	public static string CreateUserResponse() =>
"""
using Base.Domain.Enums;

namespace Base.Application.Features.Auth.Users.Models;

public class CreateUserResponse
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public UserRole Role { get; set; }
}
""";

	public static string CreateUserCommand() =>
"""
using Base.Application.Features.Auth.Users.Models;
using MediatR;

namespace Base.Application.Features.Auth.Users.Commands;

public class CreateUserCommand : IRequest<CreateUserResponse>
{
    public CreateUserRequest Request { get; }

    public CreateUserCommand(CreateUserRequest request)
    {
        Request = request;
    }
}
""";

	public static string CreateUserCommandHandler() =>
"""
using Base.Application.Common.Exceptions;
using Base.Application.Features.Auth.Users.Models;
using Base.Application.Interfaces;
using Base.Domain.Identity;
using Base.Domain.Interfaces;
using MediatR;

namespace Base.Application.Features.Auth.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var exists = await _unitOfWork.Repository<User>()
            .AnyAsync(x => x.UserName == dto.UserName);

        if (exists)
            throw new ConflictException("Bu kullanıcı adı zaten alınmış.");

        var user = new User
        {
            UserName = dto.UserName,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = dto.Role
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.CommitAsync();

        return new CreateUserResponse
        {
            UserId = user.Id,
            UserName = user.UserName,
            Role = user.Role
        };
    }
}
""";

	public static string CreateUserCommandValidator() =>
"""
using Base.Application.Features.Auth.Users.Commands;
using FluentValidation;

namespace Base.Application.Features.Auth.Users.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Request.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı boş olamaz.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");

        RuleFor(x => x.Request.Role)
            .IsInEnum().WithMessage("Geçersiz rol.");
    }
}
""";

	// ======================= Auth / Register (public, optional) =======================

	public static string RegisterRequest() =>
"""
namespace Base.Application.Features.Auth.Registers.Models;

public class RegisterRequest
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}
""";

	public static string RegisterResponse() =>
"""
namespace Base.Application.Features.Auth.Registers.Models;

public class RegisterResponse
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
}
""";

	public static string RegisterCommand() =>
"""
using Base.Application.Features.Auth.Registers.Models;
using MediatR;

namespace Base.Application.Features.Auth.Registers.Commands;

public class RegisterCommand : IRequest<RegisterResponse>
{
    public RegisterRequest Request { get; }

    public RegisterCommand(RegisterRequest request)
    {
        Request = request;
    }
}
""";

	public static string RegisterCommandHandler(string selfRegisterRole) =>
$$"""
using Base.Application.Common.Exceptions;
using Base.Application.Features.Auth.Registers.Models;
using Base.Application.Interfaces;
using Base.Domain.Enums;
using Base.Domain.Identity;
using Base.Domain.Interfaces;
using MediatR;

namespace Base.Application.Features.Auth.Registers.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var exists = await _unitOfWork.Repository<User>()
            .AnyAsync(x => x.UserName == dto.UserName);

        if (exists)
            throw new ConflictException("Bu kullanıcı adı zaten alınmış.");

        var user = new User
        {
            UserName = dto.UserName,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            // GÜVENLİK: public kayıtta rol sunucuda sabittir; client seçemez.
            Role = UserRole.{{selfRegisterRole}}
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.CommitAsync();

        return new RegisterResponse
        {
            UserId = user.Id,
            UserName = user.UserName
        };
    }
}
""";

	public static string RegisterCommandValidator() =>
"""
using Base.Application.Features.Auth.Registers.Commands;
using FluentValidation;

namespace Base.Application.Features.Auth.Registers.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı boş olamaz.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");
    }
}
""";

	// ======================= AuthController =======================

	public static string AuthController(string adminRole, bool publicRegister)
	{
		var sb = new System.Text.StringBuilder();
		sb.AppendLine("using Base.Application.Features.Auth.Logins.Commands;");
		sb.AppendLine("using Base.Application.Features.Auth.Logins.Models;");
		sb.AppendLine("using Base.Application.Features.Auth.Logouts.Commands;");
		sb.AppendLine("using Base.Application.Features.Auth.RefreshTokens.Commands;");
		sb.AppendLine("using Base.Application.Features.Auth.RefreshTokens.Models;");
		sb.AppendLine("using Base.Application.Features.Auth.Users.Commands;");
		sb.AppendLine("using Base.Application.Features.Auth.Users.Models;");
		if (publicRegister)
		{
			sb.AppendLine("using Base.Application.Features.Auth.Registers.Commands;");
			sb.AppendLine("using Base.Application.Features.Auth.Registers.Models;");
		}
		sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
		sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
		sb.AppendLine();
		sb.AppendLine("namespace Presentation.Controllers.Base;");
		sb.AppendLine();
		sb.AppendLine("[ApiController]");
		sb.AppendLine("[Route(\"api/auth\")]");
		sb.AppendLine("public class AuthController : ApiControllerBase");
		sb.AppendLine("{");
		sb.AppendLine("    /// <summary>Giriş yapar, access + refresh token döner.</summary>");
		sb.AppendLine("    [HttpPost(\"login\")]");
		sb.AppendLine("    public async Task<IActionResult> Login([FromBody] LoginRequest request)");
		sb.AppendLine("    {");
		sb.AppendLine("        var result = await Mediator.Send(new LoginCommand(request));");
		sb.AppendLine("        return Ok(result);");
		sb.AppendLine("    }");
		sb.AppendLine();
		sb.AppendLine("    /// <summary>Refresh token ile yeni access token alır.</summary>");
		sb.AppendLine("    [HttpPost(\"refresh-token\")]");
		sb.AppendLine("    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)");
		sb.AppendLine("    {");
		sb.AppendLine("        var result = await Mediator.Send(new RefreshTokenCommand(request));");
		sb.AppendLine("        return Ok(result);");
		sb.AppendLine("    }");
		sb.AppendLine();
		sb.AppendLine("    /// <summary>Oturumu kapatır; access token blacklist'e eklenir.</summary>");
		sb.AppendLine("    [HttpPost(\"logout\")]");
		sb.AppendLine("    [Authorize]");
		sb.AppendLine("    public async Task<IActionResult> Logout()");
		sb.AppendLine("    {");
		sb.AppendLine("        await Mediator.Send(new LogoutCommand());");
		sb.AppendLine("        return Ok(new { message = \"Çıkış yapıldı.\" });");
		sb.AppendLine("    }");
		sb.AppendLine();
		sb.AppendLine("    /// <summary>Yeni kullanıcı oluşturur (yalnızca yetkili rol; rol atayabilir).</summary>");
		sb.AppendLine("    [HttpPost(\"users\")]");
		sb.AppendLine($"    [Authorize(Roles = \"{adminRole}\")]");
		sb.AppendLine("    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)");
		sb.AppendLine("    {");
		sb.AppendLine("        var result = await Mediator.Send(new CreateUserCommand(request));");
		sb.AppendLine("        return Ok(result);");
		sb.AppendLine("    }");
		if (publicRegister)
		{
			sb.AppendLine();
			sb.AppendLine("    /// <summary>Herkese açık kayıt; rol sunucuda sabit atanır.</summary>");
			sb.AppendLine("    [HttpPost(\"register\")]");
			sb.AppendLine("    public async Task<IActionResult> Register([FromBody] RegisterRequest request)");
			sb.AppendLine("    {");
			sb.AppendLine("        var result = await Mediator.Send(new RegisterCommand(request));");
			sb.AppendLine("        return Ok(result);");
			sb.AppendLine("    }");
		}
		sb.AppendLine("}");
		return sb.ToString();
	}

	// ======================= Identity Seeder =======================

	public static string IdentityDataSeeder(string adminRole) =>
$$"""
using Base.Application.Interfaces;
using Base.Domain.Enums;
using Base.Domain.Identity;
using Base.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Base.Persistence.Seeding;

public static class IdentityDataSeeder
{
    /// <summary>
    /// appsettings > SeedAdmin > Enabled=true ise ve hiç admin yoksa,
    /// config'teki kullanıcı adı/şifre ile bir admin oluşturur. Şifre koda gömülü DEĞİLDİR.
    /// </summary>
    public static async Task SeedAdminAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var config = sp.GetRequiredService<IConfiguration>();
        var section = config.GetSection("SeedAdmin");

        var enabled = bool.TryParse(section["Enabled"], out var e) && e;
        if (!enabled)
            return;

        var userName = section["UserName"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return;

        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.{{adminRole}});
        if (adminExists)
            return;

        db.Users.Add(new User
        {
            UserName = userName,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.{{adminRole}},
            IsActive = true,
            CreatedDate = DateTimeOffset.UtcNow,
            CreatedById = 0
        });

        await db.SaveChangesAsync();
    }
}
""";
}
