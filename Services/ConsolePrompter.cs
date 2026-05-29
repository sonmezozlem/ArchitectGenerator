using System.Text.RegularExpressions;
using ArchitectGenerator.Core;

namespace ArchitectGenerator.Services;

/// <summary>
/// Tüm kullanıcı girdisi (konsol prompt'ları) tek yerde. Orkestrasyondan ayrıdır.
/// </summary>
public static class ConsolePrompter
{
	private static readonly Regex PascalCase = new("^[A-Z][a-zA-Z0-9]*$");

	public static string? PromptSolutionName()
	{
		Console.Write("Solution adı: ");
		var name = Console.ReadLine();
		return string.IsNullOrWhiteSpace(name) ? null : name;
	}

	public static string PromptPath()
	{
		Console.Write("Klasör (boş = Desktop): ");
		var path = Console.ReadLine();
		return string.IsNullOrWhiteSpace(path)
			? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
			: path;
	}

	public static DatabaseProvider PromptDatabaseProvider()
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

	public static bool PromptYesNo(string prompt, bool defaultYes)
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

	public static string? PromptModuleName()
	{
		while (true)
		{
			Console.Write("Modül adı (PascalCase, örn. Expense): ");
			var name = Console.ReadLine()?.Trim();
			if (string.IsNullOrEmpty(name)) return null;
			if (PascalCase.IsMatch(name)) return name;
			Console.WriteLine("❌ Modül adı PascalCase olmalı (büyük harfle başla, sadece harf/rakam).");
		}
	}

	public static string? PromptEntityName()
	{
		while (true)
		{
			Console.Write("Entity adı (PascalCase, tekil — örn. Product): ");
			var name = Console.ReadLine()?.Trim();
			if (string.IsNullOrEmpty(name)) return null;
			if (PascalCase.IsMatch(name)) return name;
			Console.WriteLine("❌ Entity adı PascalCase olmalı (büyük harfle başla, sadece harf/rakam).");
		}
	}

	public static List<EntityField>? PromptEntityFields()
	{
		Console.WriteLine("Alanlar: 'Ad:tip' çiftleri, virgülle. Nullable için tipe ? ekle.");
		Console.WriteLine("  Örn: Name:string, Price:decimal, Stock:int, Description:string?");
		while (true)
		{
			Console.Write("Alanlar (boş = iptal): ");
			var input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input)) return null;

			try
			{
				return EntityField.Parse(input);
			}
			catch (FormatException ex)
			{
				Console.WriteLine($"❌ {ex.Message}");
			}
		}
	}

	public static List<string> PromptRoles()
	{
		Console.WriteLine();
		Console.WriteLine("Roller: virgülle ayır, İLK rol en yetkili (Admin) kabul edilir, SON rol en düşük yetkili.");
		Console.WriteLine("  Örn: Admin,Restaurant   |   Admin,HR,Employee   |   Admin,Customer");

		while (true)
		{
			Console.Write("Roller [Admin,User]: ");
			var input = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(input))
				return new List<string> { "Admin", "User" };

			var roles = input
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();

			if (roles.Count == 0)
			{
				Console.WriteLine("❌ En az bir rol girin.");
				continue;
			}

			if (roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != roles.Count)
			{
				Console.WriteLine("❌ Roller benzersiz olmalı.");
				continue;
			}

			if (roles.All(r => PascalCase.IsMatch(r)))
				return roles;

			Console.WriteLine("❌ Her rol PascalCase olmalı (büyük harfle başla, sadece harf/rakam).");
		}
	}
}
