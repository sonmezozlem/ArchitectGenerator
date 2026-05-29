using ArchitectGenerator.Core;
using ArchitectGenerator.Services;
//program.cs
try
{
	Console.WriteLine("🏗️ Architect Generator");

	var solutionName = ConsolePrompter.PromptSolutionName();
	if (solutionName is null)
	{
		Console.WriteLine("❌ Geçersiz isim");
		if (!Console.IsInputRedirected) Console.ReadKey();
		return;
	}

	var path = ConsolePrompter.PromptPath();

	var provider = ConsolePrompter.PromptDatabaseProvider();
	var includeTests = ConsolePrompter.PromptYesNo("Test projeleri de oluşturulsun mu? (e/h) [e]: ", defaultYes: true);

	var roles = ConsolePrompter.PromptRoles();
	var publicRegister = ConsolePrompter.PromptYesNo(
		"Public (herkese açık) kayıt endpoint'i olsun mu? Açılırsa kullanıcı en düşük rolü alır (e/h) [h]: ",
		defaultYes: false);

	var baseOptions = new BaseOptions
	{
		Roles = roles,
		PublicRegister = publicRegister
	};

	var runner = new CommandRunner();
	var fileWriter = new FileWriter();
	var baseStructureWriter = new BaseStructureWriter(fileWriter);
	var testScaffolder = includeTests ? new TestScaffolder(runner, fileWriter) : null;
	var scaffolder = new SolutionScaffolder(runner, baseStructureWriter, testScaffolder);

	await scaffolder.CreateAsync(solutionName, path, provider, baseOptions);

	var solutionPath = Path.Combine(path, solutionName);
	var moduleScaffolder = new ModuleScaffolder(runner, fileWriter, testScaffolder);
	var entityScaffolder = new EntityScaffolder(fileWriter);

	while (ConsolePrompter.PromptYesNo("Modül eklemek ister misin? (e/h): ", defaultYes: false))
	{
		var moduleName = ConsolePrompter.PromptModuleName();
		if (moduleName is null) break;

		await moduleScaffolder.AddAsync(solutionPath, solutionName, moduleName);

		// Modüle entity (CRUD dilimi) ekleme
		while (ConsolePrompter.PromptYesNo($"'{moduleName}' modülüne entity (CRUD) eklemek ister misin? (e/h): ", defaultYes: false))
		{
			var entityName = ConsolePrompter.PromptEntityName();
			if (entityName is null) break;

			var fields = ConsolePrompter.PromptEntityFields();
			if (fields is null) break;

			await entityScaffolder.AddAsync(solutionPath, solutionName, moduleName, entityName, fields);
		}
	}

	Console.WriteLine("Son build çalıştırılıyor...");
	await runner.RunAsync("dotnet", "build -v minimal", solutionPath);

	Console.WriteLine("Tamamlandı. Çıkmak için bir tuşa bas...");
	if (!Console.IsInputRedirected) Console.ReadKey();
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine(ex.ToString());
	Console.ResetColor();
	if (!Console.IsInputRedirected) Console.ReadKey();
}
