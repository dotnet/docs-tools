namespace Quest2GitHub.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImportServices(
        this IServiceCollection services, IConfiguration importOptionsSection)
    {
        services.Configure<ImportOptions>(importOptionsSection);

        return services;
    }
}
