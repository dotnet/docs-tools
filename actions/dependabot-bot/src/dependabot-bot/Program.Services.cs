using Pathological.ProjectSystem.Services;
using Microsoft.Extensions.DependencyInjection;

static partial class Program
{
    static IDiscoveryService AddAndGetDiscoveryService()
    {
        var serviceProvider = new ServiceCollection()
            .AddDotNetProjectSystem()
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<IDiscoveryService>();
    }
}