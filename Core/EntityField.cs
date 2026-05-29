using System.Text.RegularExpressions;

namespace ArchitectGenerator.Core;

/// <summary>
/// Bir entity alanı: Ad + C# tipi (örn. "Name:string", "Price:decimal", "Description:string?").
/// </summary>
public sealed class EntityField
{
	public required string Name { get; init; }
	public required string Type { get; init; }

	public bool IsNullable => Type.EndsWith('?');
	public bool IsString => Type is "string" or "string?";
	public bool RequiredString => IsString && !IsNullable;

	/// <summary>Tip, '?' olmadan (örn. "string", "decimal", "DateTime").</summary>
	public string BaseType => IsNullable ? Type[..^1] : Type;

	/// <summary>Tarih tipi mi (aralık filtresi üretmek için).</summary>
	public bool IsDate => BaseType is "DateTime" or "DateTimeOffset";

	/// <summary>Domain/DTO için property satırı.</summary>
	public string PropertyLine()
	{
		// Otomatik property'de son ek YOK; yalnızca zorunlu string'lerde initializer eklenir.
		var suffix = RequiredString ? " = null!;" : "";
		return $"public {Type} {Name} {{ get; set; }}{suffix}";
	}

	private static readonly Dictionary<string, string> Canon = new(StringComparer.OrdinalIgnoreCase)
	{
		["string"] = "string", ["int"] = "int", ["long"] = "long", ["short"] = "short",
		["byte"] = "byte", ["decimal"] = "decimal", ["double"] = "double", ["float"] = "float",
		["bool"] = "bool", ["datetime"] = "DateTime", ["datetimeoffset"] = "DateTimeOffset",
		["guid"] = "Guid"
	};

	private static readonly Regex NameRegex = new("^[A-Z][a-zA-Z0-9]*$");

	/// <summary>
	/// "Name:string, Price:decimal, Description:string?" biçimini ayrıştırır.
	/// Hatalı girişte <see cref="FormatException"/> fırlatır.
	/// </summary>
	public static List<EntityField> Parse(string input)
	{
		var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length == 0)
			throw new FormatException("En az bir alan girin.");

		var fields = new List<EntityField>();

		foreach (var part in parts)
		{
			var kv = part.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (kv.Length != 2)
				throw new FormatException($"Geçersiz alan: '{part}'. Biçim: Ad:tip (örn. Name:string).");

			var name = kv[0];
			var rawType = kv[1];
			var nullable = rawType.EndsWith('?');
			var baseType = nullable ? rawType[..^1] : rawType;

			if (!NameRegex.IsMatch(name))
				throw new FormatException($"Alan adı PascalCase olmalı: '{name}'.");

			if (!Canon.TryGetValue(baseType, out var canonical))
				throw new FormatException(
					$"Desteklenmeyen tip: '{rawType}'. İzinli: string, int, long, short, byte, decimal, double, float, bool, DateTime, DateTimeOffset, Guid (sonuna ? eklenebilir).");

			fields.Add(new EntityField { Name = name, Type = canonical + (nullable ? "?" : "") });
		}

		if (fields.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
			throw new FormatException("Alan adları benzersiz olmalı.");

		return fields;
	}
}
