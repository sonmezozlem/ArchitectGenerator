using ArchitectGenerator.Services;

namespace ArchitectGenerator.Core;

public class SolutionScaffolder
{
	private readonly CommandRunner _runner;

	public SolutionScaffolder(CommandRunner runner)
	{
		_runner = runner;
	}

	public async Task CreateAsync(string solutionName, string basePath)
	{
		var solutionPath = Path.Combine(basePath, solutionName);
		Directory.CreateDirectory(solutionPath);

		Console.WriteLine($"📁 Solution path: {solutionPath}");

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

		await _runner.RunAsync("dotnet", "build", solutionPath);

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("🎉 PROJE OLUŞTURULDU!");
		Console.ResetColor();
	}

	private async Task CreateProject(string solutionPath, string parentPath, string projectName)
	{
		var projectPath = Path.Combine(parentPath, projectName);

		await _runner.RunAsync(
			"dotnet",
			$"new classlib -n {projectName} -o \"{projectPath}\"",
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
			$"new webapi -n {projectName} -o \"{projectPath}\"",
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
}