using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions
{
    public class Comment : IRunnerItem
    {
        private string _comment;
        private string _value;

        public Comment(YamlNode node, State state)
        {
            _comment = node.ToString();
        }

        public async Task Run(State state)
        {
            state.Logger.LogInformation($"Adding comment");
            state.Logger.LogDebug(_comment);
            await GithubCommand.AddComment(_comment, state);
        }
    }
}
