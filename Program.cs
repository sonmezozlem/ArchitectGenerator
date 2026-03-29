using ArchitectGenerator.Core;
using ArchitectGenerator.Services;
//program.cs
try
{
	Console.WriteLine("🏗️ Architect Generator");

	Console.Write("Solution adı: ");
	var solutionName = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(solutionName))
	{
		Console.WriteLine("❌ Geçersiz isim");
		Console.ReadKey();
		return;
	}

	Console.Write("Klasör (boş = Desktop): ");
	var path = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(path))
	{
		path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
	}

	var runner = new CommandRunner();
	var fileWriter = new FileWriter();
	var baseStructureWriter = new BaseStructureWriter(fileWriter);
	var scaffolder = new SolutionScaffolder(runner, baseStructureWriter);

	await scaffolder.CreateAsync(solutionName, path);

	Console.WriteLine("Tamamlandı. Çıkmak için bir tuşa bas...");
	Console.ReadKey();
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine(ex.ToString());
	Console.ResetColor();
	Console.ReadKey();
}