namespace Api;

internal static class EnvFileLocator
{
    /// <summary>
    /// Finds Backend/.env whether you run from the Api folder, repo root, or bin output.
    /// </summary>
    internal static IEnumerable<string> ResolveEnvFilePaths()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in GetCandidates())
        {
            string full;
            try
            {
                full = Path.GetFullPath(p);
            }
            catch
            {
                continue;
            }

            if (seen.Add(full))
                yield return full;
        }
    }

    private static IEnumerable<string> GetCandidates()
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");

        var baseDir = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(baseDir))
            yield break;

        yield return Path.Combine(baseDir, ".env");
        yield return Path.Combine(baseDir, "..", ".env");
        yield return Path.Combine(baseDir, "..", "..", ".env");
        yield return Path.Combine(baseDir, "..", "..", "..", ".env");
    }
}
