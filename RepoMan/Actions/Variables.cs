using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions
{
    public class Variable: IRunnerItem
    {
        private RunnerItemSubTypes _type;
        private string _id;
        private string _value;

        public Variable(YamlMappingNode node, RunnerItemSubTypes subType, State state)
        {
            _type = subType;

            _id = node["name"].ToString();

            if (subType == RunnerItemSubTypes.Add || subType == RunnerItemSubTypes.Set)
                _value = node["value"].ToString();
        }


        public async Task Run(State state)
        {
            if (_type == RunnerItemSubTypes.Remove)
            {
                state.Logger.LogInformation($"Remove variable: '{_id}'");
                state.Variables.Remove(_id);
            }
            else
            {
                state.Logger.LogInformation($"Set variable: '{_id}' to '{_value}'");
                state.Variables[_id] = _value;
            }
        }
    }
}
