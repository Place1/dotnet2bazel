using DotnetBazel.Repository;
using DotnetBazel.Skaffold;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(options =>
{
    options.AddCommand<RepositoryCommand>("repository");
    options.AddCommand<SkaffoldCommand>("skaffold");
});

return await app.RunAsync(args);
