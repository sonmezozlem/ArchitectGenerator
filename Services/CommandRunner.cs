using System.Diagnostics;

namespace ArchitectGenerator.Services;

public class CommandRunner
{
	public async Task RunAsync(string fileName, string arguments, string workingDirectory)
	{
		Console.WriteLine($"⚙️ {fileName} {arguments}");
		Console.WriteLine($"📂 Working Directory: {workingDirectory}");

		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				WorkingDirectory = workingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			}
		};

		process.Start();

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		if (!string.IsNullOrWhiteSpace(output))
		{
			Console.WriteLine("----- STDOUT -----");
			Console.WriteLine(output);
		}

		if (!string.IsNullOrWhiteSpace(error))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("----- STDERR -----");
			Console.WriteLine(error);
			Console.ResetColor();
		}

		if (process.ExitCode != 0)
		{
			throw new Exception(
				$"Komut başarısız oldu.\n" +
				$"FileName: {fileName}\n" +
				$"Arguments: {arguments}\n" +
				$"WorkingDirectory: {workingDirectory}\n" +
				$"ExitCode: {process.ExitCode}\n" +
				$"STDOUT:\n{output}\n" +
				$"STDERR:\n{error}");
		}
	}
}