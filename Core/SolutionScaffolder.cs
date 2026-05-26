using ArchitectGenerator.Services;

namespace ArchitectGenerator.Core;

public class SolutionScaffolder
{
	private readonly CommandRunner _runner;
	private readonly BaseStructureWriter _baseStructureWriter;
	private readonly TestScaffolder? _testScaffolder;

	public SolutionScaffolder(CommandRunner runner, BaseStructureWriter baseStructureWriter, TestScaffolder? testScaffolder = null)
	{
		_runner = runner;
		_baseStructureWriter = baseStructureWriter;
		_testScaffolder = testScaffolder;
	}

	public async Task CreateAsync(string solutionName, string basePath, DatabaseProvider provider)
	{
		var solutionPath = Path.Combine(basePath, solutionName);
		Directory.CreateDirectory(solutionPath);

		Console.WriteLine($"📁 Solution path: {solutionPath}");
		Console.WriteLine($"🗄️ Veritabanı: {DatabaseProviderInfo.DisplayName(provider)}");

		await _runner.RunAsync("dotnet", $"new sln -n {solutionName}", solutionPath);

		var srcPath = Path.Combine(solutionPath, "src");
		Directory.CreateDirectory(srcPath);

		var basePathDir = Path.Combine(srcPath, "Base");
		Directory.CreateDirectory(basePathDir);

		await CreateProject(solutionPath, basePathDir, "Base.Domain");
		await CreateProject(solutionPath, basePathDir, "Base.Application");
		await CreateProject(solutionPath, basePathDir, "Base.Infrastructure");
		await CreateProject(solutionPath, basePathDir, "Base.Persistence");

		var presentationPath = Path.Combine(srcPath, "Presentation");
		Directory.CreateDirectory(presentationPath);

		await CreateWebApi(solutionPath, presentationPath, $"{solutionName}.WebApi");

		await AddReferences(solutionPath, solutionName);
		await AddPackages(solutionPath, solutionName, provider);
		await AddFrameworkReferences(solutionPath);

		await _baseStructureWriter.WriteAsync(solutionPath, solutionName, provider);

		if (_testScaffolder is not null)
		{
			await _testScaffolder.CreateBaseTestsAsync(solutionPath);
		}

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("🎉 BASE KATMAN OLUŞTURULDU!");
		Console.ResetColor();
	}

	private async Task CreateProject(string solutionPath, string parentPath, string projectName)
	{
		var projectPath = Path.Combine(parentPath, projectName);

		await _runner.RunAsync(
			"dotnet",
			$"new classlib -n {projectName} -o \"{projectPath}\" --framework net10.0",
			solutionPath);

		var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");

		await _runner.RunAsync(
			"dotnet",
			$"sln add \"{csprojPath}\"",
			solutionPath);
	}

	private async Task CreateWebApi(string solutionPath, string parentPath, string projectName)
	{
		var projectPath = Path.Combine(parentPath, projectName);

		await _runner.RunAsync(
			"dotnet",
			$"new webapi -n {projectName} -o \"{projectPath}\" --framework net10.0",
			solutionPath);

		var csprojPath = Path.Combine(projectPath, $"{projectName}.csproj");

		await _runner.RunAsync(
			"dotnet",
			$"sln add \"{csprojPath}\"",
			solutionPath);
	}

	private async Task AddReferences(string solutionPath, string solutionName)
	{
		string src = Path.Combine(solutionPath, "src");

		string baseDomain = Path.Combine(src, "Base", "Base.Domain", "Base.Domain.csproj");
		string baseApp = Path.Combine(src, "Base", "Base.Application", "Base.Application.csproj");
		string baseInfra = Path.Combine(src, "Base", "Base.Infrastructure", "Base.Infrastructure.csproj");
		string basePersistence = Path.Combine(src, "Base", "Base.Persistence", "Base.Persistence.csproj");
		string webApi = Path.Combine(src, "Presentation", $"{solutionName}.WebApi", $"{solutionName}.WebApi.csproj");

		await _runner.RunAsync("dotnet", $"add \"{baseApp}\" reference \"{baseDomain}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" reference \"{baseApp}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{basePersistence}\" reference \"{baseApp}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{webApi}\" reference \"{baseInfra}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{webApi}\" reference \"{basePersistence}\"", solutionPath);
	}

	private async Task AddPackages(string solutionPath, string solutionName, DatabaseProvider provider)
	{
		string src = Path.Combine(solutionPath, "src");

		string baseApp = Path.Combine(src, "Base", "Base.Application", "Base.Application.csproj");
		string baseInfra = Path.Combine(src, "Base", "Base.Infrastructure", "Base.Infrastructure.csproj");
		string basePersistence = Path.Combine(src, "Base", "Base.Persistence", "Base.Persistence.csproj");
		string webApi = Path.Combine(src, "Presentation", $"{solutionName}.WebApi", $"{solutionName}.WebApi.csproj");

		// Base.Application
		await _runner.RunAsync("dotnet", $"add \"{baseApp}\" package MediatR", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseApp}\" package FluentValidation.DependencyInjectionExtensions", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseApp}\" package Microsoft.Extensions.DependencyInjection.Abstractions", solutionPath);

		// Base.Infrastructure
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" package BCrypt.Net-Next", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" package StackExchange.Redis", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" package Microsoft.AspNetCore.Authentication.JwtBearer", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" package Microsoft.IdentityModel.Tokens", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{baseInfra}\" package System.IdentityModel.Tokens.Jwt", solutionPath);

		// Base.Persistence
		await _runner.RunAsync("dotnet", $"add \"{basePersistence}\" package {DatabaseProviderInfo.PackageName(provider)}", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{basePersistence}\" package Microsoft.EntityFrameworkCore.Relational", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{basePersistence}\" package Microsoft.Extensions.DependencyInjection.Abstractions", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{basePersistence}\" package Microsoft.Extensions.Configuration.Abstractions", solutionPath);

		// WebApi
		await _runner.RunAsync("dotnet", $"add \"{webApi}\" package Swashbuckle.AspNetCore", solutionPath);
	}

	private async Task AddFrameworkReferences(string solutionPath)
	{
		string src = Path.Combine(solutionPath, "src");
		string infraCsproj = Path.Combine(src, "Base", "Base.Infrastructure", "Base.Infrastructure.csproj");

		var content = await File.ReadAllTextAsync(infraCsproj);

		// Varsa Http paket referansını temizle
		content = content.Replace(
			"<PackageReference Include=\"Microsoft.AspNetCore.Http\" />", "");
		content = content.Replace(
			"<PackageReference Include=\"Microsoft.AspNetCore.Http.Abstractions\" />", "");

		// FrameworkReference ekle
		content = content.Replace(
			"</Project>",
			"""

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
""");

		await File.WriteAllTextAsync(infraCsproj, content);
		Console.WriteLine("✅ FrameworkReference eklendi: Base.Infrastructure");
	}
}