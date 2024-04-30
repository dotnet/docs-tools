var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

if (builder.HostEnvironment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

builder.Services.AddOptions();
builder.Services.Configure<IssuesApiOptions>(
    builder.Configuration.GetSection(nameof(IssuesApiOptions)));

builder.Services.AddSingleton<AppInMemoryStateService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(
    HttpClientNames.IssuesApi,
    (services, client) =>
    {
        var options =
            services.GetRequiredService<IOptions<IssuesApiOptions>>().Value;

        client.BaseAddress = new Uri(options.ServerUrl);
    });

builder.Services.AddSingleton<IssuesClient>();
builder.Services.AddSingleton<IssueSnapshotsClient>();
builder.Services.AddSingleton<IssuesByPriorityClient>();
builder.Services.AddSingleton<IssuesByClassificationClient>();
builder.Services.AddSingleton<RepositoryLabelsClient>();
builder.Services.AddLocalStorageServices();

await builder.Build().RunAsync();
