using DotNetDocs.Tools.GitHubCommunications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.DocsTools.GraphQLQueries
{
    public interface IGitHubMutation<TMutation, TVariables> where TMutation : IGitHubMutation<TMutation, TVariables>
    {
        public abstract static GraphQLPacket GetMutationPacket(TVariables variables);
    }
}
