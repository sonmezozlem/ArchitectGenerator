using ArchitectGenerator.Services;
using ArchitectGenerator.Templates;

namespace ArchitectGenerator.Core;

public class ModuleScaffolder
{
	private const string UsingStartMarker = "// <ARCHITECT_GEN_MODULES_USING_START>";
	private const string UsingEndMarker = "// <ARCHITECT_GEN_MODULES_USING_END>";
	private const string DiStartMarker = "// <ARCHITECT_GEN_MODULES_DI_START>";
	private const string DiEndMarker = "// <ARCHITECT_GEN_MODULES_DI_END>";

	private readonly CommandRunner _runner;
	private readonly FileWriter _fileWriter;
	private readonly TestScaffolder? _testScaffolder;

	public ModuleScaffolder(CommandRunner runner, FileWriter fileWriter, TestScaffolder? testScaffolder = null)
	{
		_runner = runner;
		_fileWriter = fileWriter;
		_testScaffolder = testScaffolder;
	}

	public async Task AddAsync(string solutionPath, string solutionName, string moduleName)
	{
		Console.WriteLine($"📦 Modül ekleniyor: {moduleName}");

		var srcPath = Path.Combine(solutionPath, "src");
		var moduleRoot = Path.Combine(srcPath, moduleName);
		Directory.CreateDirectory(moduleRoot);

		await CreateProject(solutionPath, moduleRoot, $"{moduleName}.Domain");
		await CreateProject(solutionPath, moduleRoot, $"{moduleName}.Application");
		await CreateProject(solutionPath, moduleRoot, $"{moduleName}.Infrastructure");
		await CreateProject(solutionPath, moduleRoot, $"{moduleName}.Persistence");

		await AddModuleReferences(solutionPath, moduleName);
		await AddModulePackages(solutionPath, moduleName);
		await WriteModuleFiles(moduleRoot, moduleName);
		await WireIntoWebApi(solutionPath, solutionName, moduleName);

		if (_testScaffolder is not null)
		{
			await _testScaffolder.CreateModuleTestsAsync(solutionPath, moduleName);
		}

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine($"✅ Modül eklendi: {moduleName}");
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

	private async Task AddModuleReferences(string solutionPath, string moduleName)
	{
		string src = Path.Combine(solutionPath, "src");
		string baseDir = Path.Combine(src, "Base");
		string modDir = Path.Combine(src, moduleName);

		string baseDomain = Path.Combine(baseDir, "Base.Domain", "Base.Domain.csproj");
		string baseApp = Path.Combine(baseDir, "Base.Application", "Base.Application.csproj");
		string baseInfra = Path.Combine(baseDir, "Base.Infrastructure", "Base.Infrastructure.csproj");
		string basePersistence = Path.Combine(baseDir, "Base.Persistence", "Base.Persistence.csproj");

		string modDomain = Path.Combine(modDir, $"{moduleName}.Domain", $"{moduleName}.Domain.csproj");
		string modApp = Path.Combine(modDir, $"{moduleName}.Application", $"{moduleName}.Application.csproj");
		string modInfra = Path.Combine(modDir, $"{moduleName}.Infrastructure", $"{moduleName}.Infrastructure.csproj");
		string modPersistence = Path.Combine(modDir, $"{moduleName}.Persistence", $"{moduleName}.Persistence.csproj");

		// Module.Domain → Base.Domain
		await _runner.RunAsync("dotnet", $"add \"{modDomain}\" reference \"{baseDomain}\"", solutionPath);

		// Module.Application → Base.Application + Module.Domain
		await _runner.RunAsync("dotnet", $"add \"{modApp}\" reference \"{baseApp}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{modApp}\" reference \"{modDomain}\"", solutionPath);

		// Module.Infrastructure → Base.Infrastructure + Module.Application
		await _runner.RunAsync("dotnet", $"add \"{modInfra}\" reference \"{baseInfra}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{modInfra}\" reference \"{modApp}\"", solutionPath);

		// Module.Persistence → Base.Persistence + Module.Application
		await _runner.RunAsync("dotnet", $"add \"{modPersistence}\" reference \"{basePersistence}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{modPersistence}\" reference \"{modApp}\"", solutionPath);
	}

	private async Task AddModulePackages(string solutionPath, string moduleName)
	{
		string src = Path.Combine(solutionPath, "src");
		string modApp = Path.Combine(src, moduleName, $"{moduleName}.Application", $"{moduleName}.Application.csproj");

		await _runner.RunAsync("dotnet", $"add \"{modApp}\" package MediatR", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{modApp}\" package FluentValidation.DependencyInjectionExtensions", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{modApp}\" package Microsoft.Extensions.DependencyInjection.Abstractions", solutionPath);
	}

	private async Task WriteModuleFiles(string moduleRoot, string moduleName)
	{
		var appRoot = Path.Combine(moduleRoot, $"{moduleName}.Application");
		var infraRoot = Path.Combine(moduleRoot, $"{moduleName}.Infrastructure");
		var persistenceRoot = Path.Combine(moduleRoot, $"{moduleName}.Persistence");
		var domainRoot = Path.Combine(moduleRoot, $"{moduleName}.Domain");

		// Application
		await _fileWriter.WriteAsync(
			Path.Combine(appRoot, "ConfigureServices.cs"),
			ModuleTemplates.ApplicationConfigureServices(moduleName));
		CreatePlaceholderDir(Path.Combine(appRoot, "Features"));
		CreatePlaceholderDir(Path.Combine(appRoot, "Services"));

		// Domain
		CreatePlaceholderDir(Path.Combine(domainRoot, "Entities"));
		CreatePlaceholderDir(Path.Combine(domainRoot, "Enums"));

		// Infrastructure
		await _fileWriter.WriteAsync(
			Path.Combine(infraRoot, "ConfigureServices.cs"),
			ModuleTemplates.InfrastructureConfigureServices(moduleName));

		// Persistence
		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "ConfigureServices.cs"),
			ModuleTemplates.PersistenceConfigureServices(moduleName));
		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, $"{moduleName}ModelConfigurator.cs"),
			ModuleTemplates.ModelConfigurator(moduleName));
		CreatePlaceholderDir(Path.Combine(persistenceRoot, "Configurations"));

		// classlib şablonu otomatik bir Class1.cs üretir, sil
		RemoveDefaultClass(domainRoot);
		RemoveDefaultClass(appRoot);
		RemoveDefaultClass(infraRoot);
		RemoveDefaultClass(persistenceRoot);
	}

	private async Task WireIntoWebApi(string solutionPath, string solutionName, string moduleName)
	{
		var webApiRoot = Path.Combine(solutionPath, "src", "Presentation", $"{solutionName}.WebApi");
		var webApiCsproj = Path.Combine(webApiRoot, $"{solutionName}.WebApi.csproj");

		string modApp = Path.Combine(solutionPath, "src", moduleName, $"{moduleName}.Application", $"{moduleName}.Application.csproj");
		string modInfra = Path.Combine(solutionPath, "src", moduleName, $"{moduleName}.Infrastructure", $"{moduleName}.Infrastructure.csproj");
		string modPersistence = Path.Combine(solutionPath, "src", moduleName, $"{moduleName}.Persistence", $"{moduleName}.Persistence.csproj");

		// csproj reference'lar
		await _runner.RunAsync("dotnet", $"add \"{webApiCsproj}\" reference \"{modApp}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{webApiCsproj}\" reference \"{modInfra}\"", solutionPath);
		await _runner.RunAsync("dotnet", $"add \"{webApiCsproj}\" reference \"{modPersistence}\"", solutionPath);

		// Program.cs marker enjeksiyonu
		var programPath = Path.Combine(webApiRoot, "Program.cs");
		var content = await File.ReadAllTextAsync(programPath);

		var usingLines = $"using {moduleName}.Application;\r\nusing {moduleName}.Infrastructure;\r\nusing {moduleName}.Persistence;\r\n";
		var diLines = $"builder.Services.Add{moduleName}Application();\r\nbuilder.Services.Add{moduleName}Infrastructure();\r\nbuilder.Services.Add{moduleName}Persistence();\r\n";

		content = InjectBeforeMarker(content, UsingEndMarker, usingLines, moduleName, "using");
		content = InjectBeforeMarker(content, DiEndMarker, diLines, $"Add{moduleName}Application", "DI");

		await File.WriteAllTextAsync(programPath, content);

		// Controllers/{ModuleName}/ klasörü
		var controllersDir = Path.Combine(webApiRoot, "Controllers", moduleName);
		CreatePlaceholderDir(controllersDir);
	}

	private static string InjectBeforeMarker(string content, string endMarker, string linesToInject, string idempotencyToken, string section)
	{
		if (!content.Contains(endMarker))
		{
			throw new InvalidOperationException(
				$"Program.cs içinde '{endMarker}' marker'ı bulunamadı. {section} enjeksiyonu yapılamadı.");
		}

		// Idempotency: aynı modül daha önce eklenmişse tekrar ekleme
		var checkSubstring = endMarker;
		var idx = content.IndexOf(endMarker, StringComparison.Ordinal);
		var regionStart = content.LastIndexOf(section == "using" ? UsingStartMarker : DiStartMarker, idx, StringComparison.Ordinal);
		if (regionStart >= 0)
		{
			var region = content.Substring(regionStart, idx - regionStart);
			if (region.Contains(idempotencyToken, StringComparison.Ordinal))
			{
				Console.WriteLine($"ℹ️ {section} bölgesinde '{idempotencyToken}' zaten var, atlanıyor.");
				return content;
			}
		}

		return content.Replace(endMarker, linesToInject + endMarker);
	}

	private static void CreatePlaceholderDir(string path)
	{
		Directory.CreateDirectory(path);
		var keep = Path.Combine(path, ".gitkeep");
		if (!File.Exists(keep))
		{
			File.WriteAllText(keep, string.Empty);
		}
	}

	private static void RemoveDefaultClass(string projectPath)
	{
		var class1 = Path.Combine(projectPath, "Class1.cs");
		if (File.Exists(class1))
		{
			File.Delete(class1);
		}
	}
}
