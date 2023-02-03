using System.ComponentModel;
using Buildalyzer;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using NuGet.Configuration;
using NugetDotnet.Core;
using Spectre.Console.Cli;

namespace DotnetBazel.Repository;

public sealed class RepositoryCommandSettings : CommandSettings
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

    [CommandOption("-n|--name")]
    [Description("The generated bazel repository name. Defaults to 'nuget'.")]
    public string RepositoryName { get; init; } = "nuget";

    [CommandOption("-o|--output")]
    [Description("The output file path for the generated bzl file. Defaults to stdout.")]
    public string? OutputPath { get; init; } = null;
}

public class RepositoryCommand : AsyncCommand<RepositoryCommandSettings>
{
    public RepositoryCommand()
    {
        // must be called before any msbuild APIs are used
        // and must be call from a seperate method.
        MSBuildLocator.RegisterDefaults();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RepositoryCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TargetFramework))
        {
            throw new ArgumentException("--framework is required");
        }

        var packageReferences = settings.ProjectFiles
            .SelectMany(projectFile =>
            {
                var project = new Project(projectFile);

                var packageReferences = project.Items
                    .Where(item => item.ItemType == "PackageReference")
                    .Select(item => new
                    {
                        Name = item.EvaluatedInclude,
                        Version = item.GetMetadataValue("Version"),
                    });

                return packageReferences;
            })
            .OrderByDescending(package => package.Version)
            .DistinctBy(package => package.Name)
            .Select((packageReference) =>
                new ValueTuple<string, string>(packageReference.Name, packageReference.Version));


        var nugetConfig = Settings.LoadDefaultSettings(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var dependencyResolver = new DependencyResolver(
            settings: nugetConfig,
            targetFramework: settings.TargetFramework,
            targetRuntime: settings.TargetRuntime);

        var packages = await dependencyResolver.ResolveAsync(packageReferences);

        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            using var writer = new StreamWriter(settings.OutputPath);
            await RepositoryCommandOutput.WriteAsync(packages, settings.RepositoryName, writer);
        }
        else
        {
            await RepositoryCommandOutput.WriteAsync(packages, settings.RepositoryName, Console.Out);
        }

        return 0;
    }
}
