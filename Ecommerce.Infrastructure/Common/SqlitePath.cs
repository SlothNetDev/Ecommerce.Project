namespace Ecommerce.Infrastructure.Common;

public static class SqlitePath
{
    public static string GetSolutionRoot()
    {
        // Works for:
        // - Web API runtime
        // - EF Core design-time
        // - CLI (dotnet ef)
        var baseDir = AppContext.BaseDirectory;

        // Go up until we leave bin/{Debug|Release}/netX
        var dir = new DirectoryInfo(baseDir);

        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Ecommerce.Api")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new InvalidOperationException("Could not locate solution root.");

        return dir.FullName;
    }

    public static string GetDatabasePath(string fileName = "Ecommerce.db")
    {
        var root = GetSolutionRoot();
        var dataDir = Path.Combine(root, "Data");

        Directory.CreateDirectory(dataDir);

        return Path.Combine(dataDir, fileName);
    }

    public static string GetConnectionString(string fileName = "Ecommerce.db")
    {
        return $"Data Source={GetDatabasePath(fileName)}";
    }
}