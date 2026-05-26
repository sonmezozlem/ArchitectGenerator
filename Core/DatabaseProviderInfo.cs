namespace ArchitectGenerator.Core;

public static class DatabaseProviderInfo
{
	public static string DisplayName(DatabaseProvider provider) => provider switch
	{
		DatabaseProvider.SqlServer => "SQL Server",
		DatabaseProvider.PostgreSQL => "PostgreSQL",
		DatabaseProvider.MySQL => "MySQL",
		DatabaseProvider.SQLite => "SQLite",
		_ => throw new ArgumentOutOfRangeException(nameof(provider))
	};

	public static string PackageName(DatabaseProvider provider) => provider switch
	{
		DatabaseProvider.SqlServer => "Microsoft.EntityFrameworkCore.SqlServer",
		DatabaseProvider.PostgreSQL => "Npgsql.EntityFrameworkCore.PostgreSQL",
		DatabaseProvider.MySQL => "Pomelo.EntityFrameworkCore.MySql",
		DatabaseProvider.SQLite => "Microsoft.EntityFrameworkCore.Sqlite",
		_ => throw new ArgumentOutOfRangeException(nameof(provider))
	};

	public static string UseDbCall(DatabaseProvider provider) => provider switch
	{
		DatabaseProvider.SqlServer => "options.UseSqlServer(connectionString)",
		DatabaseProvider.PostgreSQL => "options.UseNpgsql(connectionString)",
		DatabaseProvider.MySQL => "options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))",
		DatabaseProvider.SQLite => "options.UseSqlite(connectionString)",
		_ => throw new ArgumentOutOfRangeException(nameof(provider))
	};

	public static string ConnectionString(DatabaseProvider provider, string solutionName) => provider switch
	{
		DatabaseProvider.SqlServer => $"Server=.;Database={solutionName}Db;Trusted_Connection=True;TrustServerCertificate=True",
		DatabaseProvider.PostgreSQL => $"Host=localhost;Port=5432;Database={solutionName}Db;Username=postgres;Password=postgres",
		DatabaseProvider.MySQL => $"Server=localhost;Port=3306;Database={solutionName}Db;User=root;Password=root",
		DatabaseProvider.SQLite => $"Data Source={solutionName}Db.db",
		_ => throw new ArgumentOutOfRangeException(nameof(provider))
	};
}
