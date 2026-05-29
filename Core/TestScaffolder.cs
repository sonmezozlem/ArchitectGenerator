using ArchitectGenerator.Services;
using ArchitectGenerator.Templates;

namespace ArchitectGenerator.Core;

public class TestScaffolder
{
	private readonly CommandRunner _runner;
	private readonly FileWriter _fileWriter;

	public TestScaffolder(CommandRunner runner, FileWriter fileWriter)
	{
		_runner = runner;
		_fileWriter = fileWriter;
	}

	public async Task CreateBaseTestsAsync(string solutionPath, string adminRole)
	{
		Console.WriteLine("🧪 Base test projeleri oluşturuluyor...");
		var testsBaseDir = Path.Combine(solutionPath, "tests", "Base");

		await CreateTestProject(solutionPath, testsBaseDir, "Base.Domain.Tests", srcProjectPath: BasePath(solutionPath, "Base.Domain"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsBaseDir, "Base.Domain.Tests", "BaseEntityTests.cs"),
			TestTemplates.BaseDomainTests());

		await CreateTestProject(solutionPath, testsBaseDir, "Base.Application.Tests", srcProjectPath: BasePath(solutionPath, "Base.Application"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsBaseDir, "Base.Application.Tests", "ValidationBehaviorTests.cs"),
			TestTemplates.BaseApplicationTests());

		await CreateTestProject(solutionPath, testsBaseDir, "Base.Infrastructure.Tests", srcProjectPath: BasePath(solutionPath, "Base.Infrastructure"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsBaseDir, "Base.Infrastructure.Tests", "PasswordHasherTests.cs"),
			TestTemplates.BaseInfrastructureTests());

		await CreateTestProject(solutionPath, testsBaseDir, "Base.Persistence.Tests", srcProjectPath: BasePath(solutionPath, "Base.Persistence"));
		await AddPackage(solutionPath, testsBaseDir, "Base.Persistence.Tests", "Microsoft.EntityFrameworkCore.InMemory");
		await _fileWriter.WriteAsync(
			Path.Combine(testsBaseDir, "Base.Persistence.Tests", "GenericRepositoryTests.cs"),
			TestTemplates.BasePersistenceTests(adminRole));
	}

	public async Task CreateModuleTestsAsync(string solutionPath, string moduleName)
	{
		Console.WriteLine($"🧪 {moduleName} test projeleri oluşturuluyor...");
		var testsModuleDir = Path.Combine(solutionPath, "tests", moduleName);

		await CreateTestProject(solutionPath, testsModuleDir, $"{moduleName}.Domain.Tests", srcProjectPath: ModulePath(solutionPath, moduleName, "Domain"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsModuleDir, $"{moduleName}.Domain.Tests", "PlaceholderTests.cs"),
			TestTemplates.ModuleDomainTests(moduleName));

		await CreateTestProject(solutionPath, testsModuleDir, $"{moduleName}.Application.Tests", srcProjectPath: ModulePath(solutionPath, moduleName, "Application"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsModuleDir, $"{moduleName}.Application.Tests", "PlaceholderTests.cs"),
			TestTemplates.ModuleApplicationTests(moduleName));

		await CreateTestProject(solutionPath, testsModuleDir, $"{moduleName}.Infrastructure.Tests", srcProjectPath: ModulePath(solutionPath, moduleName, "Infrastructure"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsModuleDir, $"{moduleName}.Infrastructure.Tests", "PlaceholderTests.cs"),
			TestTemplates.ModuleInfrastructureTests(moduleName));

		await CreateTestProject(solutionPath, testsModuleDir, $"{moduleName}.Persistence.Tests", srcProjectPath: ModulePath(solutionPath, moduleName, "Persistence"));
		await _fileWriter.WriteAsync(
			Path.Combine(testsModuleDir, $"{moduleName}.Persistence.Tests", "PlaceholderTests.cs"),
			TestTemplates.ModulePersistenceTests(moduleName));
	}

	private async Task CreateTestProject(string solutionPath, string parentDir, string projectName, string srcProjectPath)
	{
		var projectDir = Path.Combine(parentDir, projectName);
		var csprojPath = Path.Combine(projectDir, $"{projectName}.csproj");

		await _runner.RunAsync(
			"dotnet",
			$"new xunit -n {projectName} -o \"{projectDir}\" --framework net10.0",
			solutionPath);

		await _runner.RunAsync("dotnet", $"sln add \"{csprojPath}\"", solutionPath);

		// xUnit template'i Class1.cs gibi default dosya bırakabiliyor
		var defaultTest = Path.Combine(projectDir, "UnitTest1.cs");
		if (File.Exists(defaultTest)) File.Delete(defaultTest);

		await _runner.RunAsync("dotnet", $"add \"{csprojPath}\" package FluentAssertions", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{csprojPath}\" package Moq", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{csprojPath}\" reference \"{srcProjectPath}\"", solutionPath);
	}

	private async Task AddPackage(string solutionPath, string parentDir, string projectName, string packageName)
	{
		var csprojPath = Path.Combine(parentDir, projectName, $"{projectName}.csproj");
		await _runner.RunAsync("dotnet", $"add \"{csprojPath}\" package {packageName}", solutionPath);
	}

	private static string BasePath(string solutionPath, string projectName) =>
		Path.Combine(solutionPath, "src", "Base", projectName, $"{projectName}.csproj");

	private static string ModulePath(string solutionPath, string moduleName, string layer) =>
		Path.Combine(solutionPath, "src", moduleName, $"{moduleName}.{layer}", $"{moduleName}.{layer}.csproj");
}
