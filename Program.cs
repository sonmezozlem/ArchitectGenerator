using ArchitectGenerator.Core;
using ArchitectGenerator.Services;
using System.Text.RegularExpressions;
//program.cs
try
{
	Console.WriteLine("🏗️ Architect Generator");

	Console.Write("Solution adı: ");
	var solutionName = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(solutionName))
	{
		Console.WriteLine("❌ Geçersiz isim");
		if (!Console.IsInputRedirected) Console.ReadKey();
		return;
	}

	Console.Write("Klasör (boş = Desktop): ");
	var path = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(path))
	{
		path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
	}

	var provider = PromptDatabaseProvider();
	var includeTests = PromptYesNo("Test projeleri de oluşturulsun mu? (e/h) [e]: ", defaultYes: true);

	var runner = new CommandRunner();
	var fileWriter = new FileWriter();
	var baseStructureWriter = new BaseStructureWriter(fileWriter);
	var testScaffolder = includeTests ? new TestScaffolder(runner, fileWriter) : null;
	var scaffolder = new SolutionScaffolder(runner, baseStructureWriter, testScaffolder);

	await scaffolder.CreateAsync(solutionName, path, provider);

	var solutionPath = Path.Combine(path, solutionName);
	var moduleScaffolder = new ModuleScaffolder(runner, fileWriter, testScaffolder);

	while (PromptYesNo("Modül eklemek ister misin? (e/h): ", defaultYes: false))
	{
		var moduleName = PromptModuleName();
		if (moduleName is null) break;

		await moduleScaffolder.AddAsync(solutionPath, solutionName, moduleName);
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

static DatabaseProvider PromptDatabaseProvider()
{
	Console.WriteLine();
	Console.WriteLine("Veritabanı seçin:");
	Console.WriteLine("  1) SQL Server (varsayılan)");
	Console.WriteLine("  2) PostgreSQL");
	Console.WriteLine("  3) MySQL");
	Console.WriteLine("  4) SQLite");

	while (true)
	{
		Console.Write("Seçim (1-4) [1]: ");
		var input = Console.ReadLine();

		if (string.IsNullOrWhiteSpace(input))
			return DatabaseProvider.SqlServer;

		if (int.TryParse(input, out var n) && Enum.IsDefined(typeof(DatabaseProvider), n))
			return (DatabaseProvider)n;

		Console.WriteLine("❌ Geçersiz seçim, tekrar deneyin.");
	}
}

static bool PromptYesNo(string prompt, bool defaultYes)
{
	while (true)
	{
		Console.Write(prompt);
		var input = Console.ReadLine()?.Trim().ToLowerInvariant();
		if (string.IsNullOrEmpty(input)) return defaultYes;
		if (input is "e" or "evet" or "y" or "yes") return true;
		if (input is "h" or "hayır" or "hayir" or "n" or "no") return false;
		Console.WriteLine("❌ Lütfen e veya h girin.");
	}
}

static string? PromptModuleName()
{
	var regex = new Regex("^[A-Z][a-zA-Z0-9]*$");
	while (true)
	{
		Console.Write("Modül adı (PascalCase, örn. Expense): ");
		var name = Console.ReadLine()?.Trim();
		if (string.IsNullOrEmpty(name)) return null;
		if (regex.IsMatch(name)) return name;
		Console.WriteLine("❌ Modül adı PascalCase olmalı (büyük harfle başla, sadece harf/rakam).");
	}
}
