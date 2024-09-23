using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Variable: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string _id;
    private readonly string _value;
    private readonly bool _isValid;

    public Variable(YamlMappingNode node, RunnerItemSubTypes subType, InstanceData data)
    {
        _type = subType;
        _id = node["name"].ToString();
        _isValid = true;

        if (subType == RunnerItemSubTypes.Add || subType == RunnerItemSubTypes.Set)
            _value = node["value"].ToString();
        else
            // The default invalid JMES value. Removed variables don't need a value.
            _value = "null";
    }

    public async Task Run(InstanceData data)
    {
        if (_type == RunnerItemSubTypes.Remove)
        {
            data.Logger.LogInformation("RUN [VARIABLES]: Remove variable: '{id}'", _id);
            data.Variables.Remove(_id);
        }
        else
        {
            data.Logger.LogInformation("RUN [VARIABLES]: Set variable: '{id}' to '{value}'", _id, _value);

            string tempValue = _value;

            if (tempValue.StartsWith("jmes:"))
                tempValue = Utilities.GetJMESResult(_value.Substring("jmes:".Length), data).Trim('"');

            data.Variables[_id] = tempValue;
        }

        await Task.CompletedTask;
    }
}
