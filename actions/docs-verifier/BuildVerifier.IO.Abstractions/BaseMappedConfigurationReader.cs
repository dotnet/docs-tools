using System.Threading.Tasks;

namespace BuildVerifier.IO.Abstractions
{
    public abstract class BaseMappedConfigurationReader<TConfigurationFile, TMappedResult>
        : BaseConfigurationReader<TConfigurationFile>
        where TConfigurationFile : class
    {
        public abstract ValueTask<TMappedResult> MapConfigurationAsync();
    }
}
