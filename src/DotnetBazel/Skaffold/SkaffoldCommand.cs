using System.ComponentModel;
using Buildalyzer;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using NuGet.Configuration;
using NugetDotnet.Core;
using Spectre.Console.Cli;

namespace DotnetBazel.Skaffold;

public sealed class SkaffoldCommandSettings : CommandSettings
{
    [CommandArgument(0, "<csproj>")]
    [Description("The path to a csproj file.")]
    public string[] ProjectFiles { get; init; } = null!;

    [CommandOption("-f|--framework")]
    [Description("The target framework.")]
    public string TargetFramework { get; init; } = null!;

    [CommandOption("-r|--runtime")]
    [Description("The target runtime.")]
    [DefaultValue(null)]
    public string? TargetRuntime { get; init; } = null;

    [CommandOption("-w|--write")]
    [Description("Write the generated BUILD.bazel file.")]
    public bool Write { get; init; } = false;


    [CommandOption("--workspace")]
    [Description("Path to the bazel workspace directory.")]
    public string WorkspaceRoot { get; init; } = null!;
}

public class SkaffoldCommand : AsyncCommand<SkaffoldCommandSettings>
{
    public SkaffoldCommand()
    {
        // must be called before any msbuild APIs are used
        // and must be call from a seperate method.
        MSBuildLocator.RegisterDefaults();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SkaffoldCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.WorkspaceRoot))
        {
            throw new ArgumentException("missing --workspace flag (path to the bazel workspace directory)");
        }

        await Task.WhenAll(settings.ProjectFiles
            .Select(async (projectFile) =>
            {
                var project = new Project(projectFile);

                var packageReferences = project.Items
                    .Where(item => item.ItemType == "PackageReference")
                    .Select(item => new
                    {
                        Name = item.EvaluatedInclude,
                        Version = item.GetMetadataValue("Version"),
                    })
                    .Select(p => $"@nuget//{p.Name.ToLower()}");

                var projectReferences = project.Items
                    .Where(item => item.ItemType == "ProjectReference")
                    .Select(item => new
                    {
                        Name = Path.GetFileNameWithoutExtension(item.EvaluatedInclude),
                        Directory = Path.GetDirectoryName(Path.GetRelativePath(Path.GetFullPath(settings.WorkspaceRoot), Path.GetFullPath(Path.Join(Path.GetDirectoryName(projectFile), item.EvaluatedInclude)))),
                    })
                    .Select(p => $"//{p.Directory}:{p.Name}");

                var name = Path.GetFileNameWithoutExtension(projectFile);
                var dependencies = projectReferences
                    .Concat(packageReferences)
                    .Order();

                if (projectFile.Contains("Test") || projectFile.Contains("Integration"))
                {
                    return;
                }

                var output = Console.Out;
                if (settings.Write)
                {
                    output = new StreamWriter(Path.Join(Path.GetDirectoryName(projectFile), "BUILD.bazel"));
                }

                if (File.Exists(Path.Join(Path.GetDirectoryName(projectFile), "Program.cs")))
                {
                    await BinaryOutput.WriteAsync(name, settings.TargetFramework, dependencies, output);
                }
                else
                {
                    await LibraryOutput.WriteAsync(name, settings.TargetFramework, dependencies, output);
                }

                await output.DisposeAsync();
            }));

        return 0;
    }
}
