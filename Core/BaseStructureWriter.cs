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

	public async Task WriteAsync(string solutionPath, string solutionName, DatabaseProvider provider)
	{
		var srcPath = Path.Combine(solutionPath, "src");
		var baseRoot = Path.Combine(srcPath, "Base");
		var webApiRoot = Path.Combine(srcPath, "Presentation", $"{solutionName}.WebApi");

		await WriteBaseDomain(baseRoot);
		await WriteBaseApplication(baseRoot);
		await WriteBaseInfrastructure(baseRoot);
		await WriteBasePersistence(baseRoot, provider);
		await WriteWebApi(webApiRoot, solutionName, provider);
	}

	private async Task WriteBaseDomain(string baseRoot)
	{
		var domainRoot = Path.Combine(baseRoot, "Base.Domain");

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Entities", "BaseEntity.cs"),
			BaseTemplates.BaseEntity());

		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Enums", "UserRole.cs"),
			BaseTemplates.UserRole());

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

	private async Task WriteBaseApplication(string baseRoot)
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

	private async Task WriteBasePersistence(string baseRoot, DatabaseProvider provider)
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
	}

	private async Task WriteWebApi(string webApiRoot, string solutionName, DatabaseProvider provider)
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
	}
}