namespace ArchitectGenerator.Services;

public class FileWriter
{
	public async Task WriteAsync(string path, string content)
	{
		var directory = Path.GetDirectoryName(path);

		if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		await File.WriteAllTextAsync(path, content);
		Console.WriteLine($"📝 {path}");
	}
}