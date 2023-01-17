using DotnetDocsTools.GitHubCommunications;
using GitHub.RepositoryExplorer.Functions;
using GitHub.RepositoryExplorer.Functions.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace GitHub.RepositoryExplorer.Functions;

public class Startup : FunctionsStartup
{

    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        FunctionsHostBuilderContext context = builder.GetContext();

        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        FunctionsHostBuilderContext context = builder.GetContext();

        builder.Services.AddOptions<RepositoriesConfig>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("RepositoriesConfig").Bind(settings);
            });

        builder.Services.AddCosmosRepository();


        builder.Services.AddSingleton((_) => IGitHubClient.CreateGitHubClient(context.Configuration["GitHubKey"]));
    }
}
