using ArchitectGenerator.Services;
using ArchitectGenerator.Templates;

namespace ArchitectGenerator.Core;

public class BaseStructureWriter
{
	private readonly FileWriter _fileWriter;

	public BaseStructureWriter(FileWriter fileWriter)
	{
		_fileWriter = fileWriter;
	}

	public async Task WriteAsync(string solutionPath, string solutionName, DatabaseProvider provider, BaseOptions options)
	{
		var srcPath = Path.Combine(solutionPath, "src");
		var baseRoot = Path.Combine(srcPath, "Base");
		var webApiRoot = Path.Combine(srcPath, "Presentation", $"{solutionName}.WebApi");

		await WriteBaseDomain(baseRoot, options);
		await WriteBaseApplication(baseRoot, options);
		await WriteBaseInfrastructure(baseRoot);
		await WriteBasePersistence(baseRoot, provider, options);
		await WriteWebApi(webApiRoot, solutionName, provider, options);
	}

	private async Task WriteBaseDomain(string baseRoot, BaseOptions options)
	{
		var domainRoot = Path.Combine(baseRoot, "Base.Domain");

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Entities", "BaseEntity.cs"),
			BaseTemplates.BaseEntity());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Enums", "UserRole.cs"),
			BaseTemplates.UserRole(options.Roles));

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Identity", "User.cs"),
			BaseTemplates.User());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Identity", "RefreshToken.cs"),
			BaseTemplates.RefreshToken());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Interfaces", "IRepository.cs"),
			BaseTemplates.IRepository());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Interfaces", "IUnitOfWork.cs"),
			BaseTemplates.IUnitOfWork());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Interfaces", "ITransaction.cs"),
			BaseTemplates.ITransaction());
	}

	private async Task WriteBaseApplication(string baseRoot, BaseOptions options)
	{
		var appRoot = Path.Combine(baseRoot, "Base.Application");

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Behaviors", "ValidationBehavior.cs"),
			BaseTemplates.ValidationBehavior());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Interfaces", "IUserContextService.cs"),
			BaseTemplates.IUserContextService());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Interfaces", "IJwtService.cs"),
			BaseTemplates.IJwtService());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Interfaces", "IPasswordHasher.cs"),
			BaseTemplates.IPasswordHasher());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Interfaces", "IRefreshTokenGenerator.cs"),
			BaseTemplates.IRefreshTokenGenerator());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Interfaces", "IRedisService.cs"),
			BaseTemplates.IRedisService());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Settings", "JwtSettings.cs"),
			BaseTemplates.JwtSettings());

		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "ConfigureServices.cs"),
			BaseTemplates.BaseApplicationConfigureServices());

		// Common/Exceptions
		var exceptionsRoot = Path.Combine(appRoot, "Common", "Exceptions");
		await _fileWriter.WriteAsync(Path.Combine(exceptionsRoot, "InvalidCredentialsException.cs"), BaseTemplates.InvalidCredentialsException());
		await _fileWriter.WriteAsync(Path.Combine(exceptionsRoot, "ConflictException.cs"), BaseTemplates.ConflictException());
		await _fileWriter.WriteAsync(Path.Combine(exceptionsRoot, "ForbiddenException.cs"), BaseTemplates.ForbiddenException());

		// Common/Models (sayfalama)
		var modelsRoot = Path.Combine(appRoot, "Common", "Models");
		await _fileWriter.WriteAsync(Path.Combine(modelsRoot, "PagedRequest.cs"), BaseTemplates.PagedRequest());
		await _fileWriter.WriteAsync(Path.Combine(modelsRoot, "PagedResult.cs"), BaseTemplates.PagedResult());

		// Common/Extensions (koşullu predicate için And/Or/Not)
		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "Common", "Extensions", "ExpressionExtensions.cs"),
			BaseTemplates.ExpressionExtensions());

		// Features/Auth/Logins
		var loginsRoot = Path.Combine(appRoot, "Features", "Auth", "Logins");
		await _fileWriter.WriteAsync(Path.Combine(loginsRoot, "Models", "LoginRequest.cs"), BaseTemplates.LoginRequest());
		await _fileWriter.WriteAsync(Path.Combine(loginsRoot, "Models", "LoginResponse.cs"), BaseTemplates.LoginResponse());
		await _fileWriter.WriteAsync(Path.Combine(loginsRoot, "Commands", "LoginCommand.cs"), BaseTemplates.LoginCommand());
		await _fileWriter.WriteAsync(Path.Combine(loginsRoot, "Commands", "LoginCommandHandler.cs"), BaseTemplates.LoginCommandHandler());
		await _fileWriter.WriteAsync(Path.Combine(loginsRoot, "Validators", "LoginCommandValidator.cs"), BaseTemplates.LoginCommandValidator());

		// Features/Auth/Logouts
		var logoutsRoot = Path.Combine(appRoot, "Features", "Auth", "Logouts");
		await _fileWriter.WriteAsync(Path.Combine(logoutsRoot, "Commands", "LogoutCommand.cs"), BaseTemplates.LogoutCommand());
		await _fileWriter.WriteAsync(Path.Combine(logoutsRoot, "Commands", "LogoutCommandHandler.cs"), BaseTemplates.LogoutCommandHandler());

		// Features/Auth/RefreshTokens
		var refreshRoot = Path.Combine(appRoot, "Features", "Auth", "RefreshTokens");
		await _fileWriter.WriteAsync(Path.Combine(refreshRoot, "Models", "RefreshTokenRequest.cs"), BaseTemplates.RefreshTokenRequest());
		await _fileWriter.WriteAsync(Path.Combine(refreshRoot, "Models", "RefreshTokenResponse.cs"), BaseTemplates.RefreshTokenResponse());
		await _fileWriter.WriteAsync(Path.Combine(refreshRoot, "Commands", "RefreshTokenCommand.cs"), BaseTemplates.RefreshTokenCommand());
		await _fileWriter.WriteAsync(Path.Combine(refreshRoot, "Commands", "RefreshTokenCommandHandler.cs"), BaseTemplates.RefreshTokenCommandHandler());
		await _fileWriter.WriteAsync(Path.Combine(refreshRoot, "Validators", "RefreshTokenCommandValidator.cs"), BaseTemplates.RefreshTokenCommandValidator());

		// Features/Auth/Users (admin-only kullanıcı oluşturma)
		var usersRoot = Path.Combine(appRoot, "Features", "Auth", "Users");
		await _fileWriter.WriteAsync(Path.Combine(usersRoot, "Models", "CreateUserRequest.cs"), BaseTemplates.CreateUserRequest());
		await _fileWriter.WriteAsync(Path.Combine(usersRoot, "Models", "CreateUserResponse.cs"), BaseTemplates.CreateUserResponse());
		await _fileWriter.WriteAsync(Path.Combine(usersRoot, "Commands", "CreateUserCommand.cs"), BaseTemplates.CreateUserCommand());
		await _fileWriter.WriteAsync(Path.Combine(usersRoot, "Commands", "CreateUserCommandHandler.cs"), BaseTemplates.CreateUserCommandHandler());
		await _fileWriter.WriteAsync(Path.Combine(usersRoot, "Validators", "CreateUserCommandValidator.cs"), BaseTemplates.CreateUserCommandValidator());

		// Features/Auth/Registers (yalnızca public self-register açıksa)
		if (options.PublicRegister)
		{
			var registersRoot = Path.Combine(appRoot, "Features", "Auth", "Registers");
			await _fileWriter.WriteAsync(Path.Combine(registersRoot, "Models", "RegisterRequest.cs"), BaseTemplates.RegisterRequest());
			await _fileWriter.WriteAsync(Path.Combine(registersRoot, "Models", "RegisterResponse.cs"), BaseTemplates.RegisterResponse());
			await _fileWriter.WriteAsync(Path.Combine(registersRoot, "Commands", "RegisterCommand.cs"), BaseTemplates.RegisterCommand());
			await _fileWriter.WriteAsync(Path.Combine(registersRoot, "Commands", "RegisterCommandHandler.cs"), BaseTemplates.RegisterCommandHandler(options.SelfRegisterRole));
			await _fileWriter.WriteAsync(Path.Combine(registersRoot, "Validators", "RegisterCommandValidator.cs"), BaseTemplates.RegisterCommandValidator());
		}
	}

	private async Task WriteBaseInfrastructure(string baseRoot)
	{
		var infraRoot = Path.Combine(baseRoot, "Base.Infrastructure");

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Services", "Auth", "PasswordHasher.cs"),
			BaseTemplates.PasswordHasher());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Services", "UserContextService.cs"),
			BaseTemplates.UserContextService());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Middlewares", "GlobalExceptionMiddleware.cs"),
			BaseTemplates.GlobalExceptionMiddleware());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Middlewares", "ExceptionMiddlewareExtensions.cs"),
			BaseTemplates.ExceptionMiddlewareExtensions());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "ConfigureServices.cs"),
			BaseTemplates.BaseInfrastructureConfigureServices());

		await _fileWriter.WriteAsync(
	Path.Combine(infraRoot, "Services", "Auth", "JwtService.cs"),
	BaseTemplates.JwtService());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Services", "Auth", "RefreshTokenGenerator.cs"),
			BaseTemplates.RefreshTokenGenerator());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Services", "Redis", "RedisService.cs"),
			BaseTemplates.RedisService());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Middlewares", "SecurityHeadersMiddleware.cs"),
			BaseTemplates.SecurityHeadersMiddleware());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Middlewares", "RequestLoggingMiddleware.cs"),
			BaseTemplates.RequestLoggingMiddleware());

		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "Middlewares", "JwtBlacklistMiddleware.cs"),
			BaseTemplates.JwtBlacklistMiddleware());
	}

	private async Task WriteBasePersistence(string baseRoot, DatabaseProvider provider, BaseOptions options)
	{
		var persistenceRoot = Path.Combine(baseRoot, "Base.Persistence");

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Interfaces", "IModelConfigurator.cs"),
			BaseTemplates.IModelConfigurator());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "DbContext", "AppDbContext.cs"),
			BaseTemplates.AppDbContext());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Configurations", "Base", "BaseEntityConfiguration.cs"),
			BaseTemplates.BaseEntityConfiguration());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Configurations", "Identity", "UserConfiguration.cs"),
			BaseTemplates.UserConfiguration());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Configurations", "Identity", "RefreshTokenConfiguration.cs"),
			BaseTemplates.RefreshTokenConfiguration());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Repositories", "GenericRepository.cs"),
			BaseTemplates.GenericRepository());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "UnitOfWork", "EfTransaction.cs"),
			BaseTemplates.EfTransaction());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "UnitOfWork", "UnitOfWork.cs"),
			BaseTemplates.UnitOfWork());

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "ConfigureServices.cs"),
			BaseTemplates.BasePersistenceConfigureServices(provider));

		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Seeding", "IdentityDataSeeder.cs"),
			BaseTemplates.IdentityDataSeeder(options.AdminRole));
	}

	private async Task WriteWebApi(string webApiRoot, string solutionName, DatabaseProvider provider, BaseOptions options)
	{
		await _fileWriter.WriteAsync(
			Path.Combine(webApiRoot, "Program.cs"),
			BaseTemplates.ProgramCs());

		await _fileWriter.WriteAsync(
			Path.Combine(webApiRoot, "appsettings.json"),
			BaseTemplates.AppSettings(solutionName, provider));

		await _fileWriter.WriteAsync(
			Path.Combine(webApiRoot, "Controllers", "Base", "ApiControllerBase.cs"),
			BaseTemplates.ApiControllerBase());

		await _fileWriter.WriteAsync(
			Path.Combine(webApiRoot, "Controllers", "Base", "AuthController.cs"),
			BaseTemplates.AuthController(options.AdminRole, options.PublicRegister));
	}
}