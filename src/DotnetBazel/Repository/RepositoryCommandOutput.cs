using NugetDotnet.Core;

public static class RepositoryCommandOutput
{
    public static async Task WriteAsync(IEnumerable<INugetRepositoryEntry> entries, string name, TextWriter writer)
    {
        await writer.WriteLineAsync("load(\"@rules_dotnet//dotnet:defs.bzl\", \"nuget_repo\")");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"def {name}():");
        await writer.WriteLineAsync("    nuget_repo(");
        await writer.WriteLineAsync($"        name = \"{name}\",");
        await writer.WriteLineAsync("        packages = [");
        foreach (var entry in entries)
        {
            await writer.WriteAsync($"            (\"{entry.Id}\", \"{entry.Version}\", \"{entry.Hash}\", [");

            var dependencies = entry.DependencyGroups.SingleOrDefault()?.Packages;
            if (dependencies != null)
            {
                await writer.WriteAsync(string.Join(", ", dependencies.Select(dep => $"\"{dep.Id}\"")));
            }

            await writer.WriteLineAsync("], []),");
        }
        await writer.WriteLineAsync("        ],");
        await writer.WriteLineAsync("    )");
    }
}

