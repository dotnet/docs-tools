using System.Threading.Tasks;

namespace BuildVerifier.IO.Abstractions
{
    public abstract class BaseMappedConfigurationReader<TConfigurationFile, TMappedResult>
        : BaseConfigurationReader<TConfigurationFile>
        where TConfigurationFile : class
    {
        /// <summary>
        /// Maps the <typeparamref name="TConfigurationFile"/> into a consumer-defined <typeparamref name="TMappedResult"/>.
        /// </summary>
        public abstract ValueTask<TMappedResult> MapConfigurationAsync();
    }
}
