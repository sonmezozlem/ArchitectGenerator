using ArchitectGenerator.Core;
using ArchitectGenerator.Services;

try
{
	Console.WriteLine("🏗️ Architect Generator");

	Console.Write("Solution adı: ");
	var solutionName = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(solutionName))
	{
		Console.WriteLine("❌ Geçersiz isim");
		Console.WriteLine("Çıkmak için bir tuşa bas...");
		Console.ReadKey();
		return;
	}

	Console.Write("Klasör (boş = Desktop): ");
	var path = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(path))
	{
		path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
	}

	var scaffolder = new SolutionScaffolder(new CommandRunner());

	await scaffolder.CreateAsync(solutionName, path);

	Console.WriteLine("İşlem tamamlandı. Çıkmak için bir tuşa bas...");
	Console.ReadKey();
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine("HATA:");
	Console.WriteLine(ex.ToString());
	Console.ResetColor();

	Console.WriteLine("Çıkmak için bir tuşa bas...");
	Console.ReadKey();
}