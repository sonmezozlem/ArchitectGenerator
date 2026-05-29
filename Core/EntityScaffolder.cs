using ArchitectGenerator.Services;
using ArchitectGenerator.Templates;

namespace ArchitectGenerator.Core;

/// <summary>
/// Var olan bir modüle tek bir entity için tam CRUD (CQRS) dilimi + controller yazar.
/// Modül projeleri ve WebApi referansları zaten <see cref="ModuleScaffolder"/> tarafından kurulmuştur.
/// </summary>
public class EntityScaffolder
{
	private readonly FileWriter _fileWriter;

	public EntityScaffolder(FileWriter fileWriter)
	{
		_fileWriter = fileWriter;
	}

	public async Task AddAsync(
		string solutionPath,
		string solutionName,
		string moduleName,
		string entityName,
		List<EntityField> fields)
	{
		Console.WriteLine($"🧩 Entity ekleniyor: {moduleName}/{entityName}");

		var src = Path.Combine(solutionPath, "src");
		var moduleRoot = Path.Combine(src, moduleName);
		var domainRoot = Path.Combine(moduleRoot, $"{moduleName}.Domain");
		var appRoot = Path.Combine(moduleRoot, $"{moduleName}.Application");
		var persistenceRoot = Path.Combine(moduleRoot, $"{moduleName}.Persistence");
		var webApiRoot = Path.Combine(src, "Presentation", $"{solutionName}.WebApi");

		var featureRoot = Path.Combine(appRoot, "Features", $"{entityName}s");

		// Domain
		await _fileWriter.WriteAsync(
			Path.Combine(domainRoot, "Entities", $"{entityName}.cs"),
			EntityTemplates.Entity(moduleName, entityName, fields));

		// Persistence configuration (module ModelConfigurator otomatik keşfeder)
		await _fileWriter.WriteAsync(
			Path.Combine(persistenceRoot, "Configurations", $"{entityName}Configuration.cs"),
			EntityTemplates.Configuration(moduleName, entityName, fields));

		// Models
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Models", $"{entityName}Response.cs"),
			EntityTemplates.Response(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Models", $"Create{entityName}Request.cs"),
			EntityTemplates.CreateRequest(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Models", $"Update{entityName}Request.cs"),
			EntityTemplates.UpdateRequest(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Models", $"Get{entityName}Request.cs"),
			EntityTemplates.GetRequest(moduleName, entityName, fields));

		// Converters (elle mapping)
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Converters", $"{entityName}Converters.cs"),
			EntityTemplates.Converters(moduleName, entityName, fields));

		// Commands
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Create{entityName}Command.cs"),
			EntityTemplates.CreateCommand(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Create{entityName}CommandHandler.cs"),
			EntityTemplates.CreateCommandHandler(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Update{entityName}Command.cs"),
			EntityTemplates.UpdateCommand(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Update{entityName}CommandHandler.cs"),
			EntityTemplates.UpdateCommandHandler(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Delete{entityName}Command.cs"),
			EntityTemplates.DeleteCommand(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Commands", $"Delete{entityName}CommandHandler.cs"),
			EntityTemplates.DeleteCommandHandler(moduleName, entityName));

		// Validators
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Validators", $"Create{entityName}CommandValidator.cs"),
			EntityTemplates.CreateValidator(moduleName, entityName, fields));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Validators", $"Update{entityName}CommandValidator.cs"),
			EntityTemplates.UpdateValidator(moduleName, entityName, fields));

		// Queries
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Queries", $"Get{entityName}ByIdQuery.cs"),
			EntityTemplates.GetByIdQuery(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Queries", $"Get{entityName}ByIdQueryHandler.cs"),
			EntityTemplates.GetByIdQueryHandler(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Queries", $"Get{entityName}ListQuery.cs"),
			EntityTemplates.GetListQuery(moduleName, entityName));
		await _fileWriter.WriteAsync(Path.Combine(featureRoot, "Queries", $"Get{entityName}ListQueryHandler.cs"),
			EntityTemplates.GetListQueryHandler(moduleName, entityName, fields));

		// Presentation controller
		await _fileWriter.WriteAsync(
			Path.Combine(webApiRoot, "Controllers", moduleName, $"{entityName}Controller.cs"),
			EntityTemplates.Controller(moduleName, entityName));

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine($"✅ Entity eklendi: {moduleName}/{entityName} (CRUD + controller)");
		Console.ResetColor();
	}
}
