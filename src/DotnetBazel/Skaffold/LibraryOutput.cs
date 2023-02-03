using NugetDotnet.Core;

public static class LibraryOutput
{
    public static async Task WriteAsync(string name, string framework, IEnumerable<string> dependencies, TextWriter writer)
    {
        await writer.WriteLineAsync("load(\"@rules_dotnet//dotnet:defs.bzl\", \"csharp_library\")");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("csharp_library(");
        await writer.WriteLineAsync($"    name = \"{name}\",");
        await writer.WriteLineAsync("    srcs = glob([\"**/*.cs\"], exclude = [\"**/bin/**/*.cs\", \"**/obj/**/*.cs\"]),");
        await writer.WriteLineAsync("    private_deps = [");
        await writer.WriteLineAsync("        \"@nuget//microsoft.netcore.app.ref\",");
        await writer.WriteLineAsync("        \"@nuget//microsoft.aspnetcore.app.ref\",");
        await writer.WriteLineAsync("    ],");
        await writer.WriteLineAsync($"    target_frameworks = [\"{framework}\"],");
        await writer.WriteLineAsync("    deps = [");
        foreach (var dep in dependencies)
        {
            await writer.WriteLineAsync($"        \"{dep}\",");
        }
        await writer.WriteLineAsync("    ],");
        await writer.WriteLineAsync("    visibility = [\"//visibility:public\"],");
        await writer.WriteLineAsync(")");
    }
}

