using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class Variable: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string _id;
    private readonly string _value;
    private readonly bool _isValid;

    public Variable(YamlMappingNode node, RunnerItemSubTypes subType, State state)
    {
        _type = subType;
        _id = node["name"].ToString();
        _isValid = true;

        if (subType == RunnerItemSubTypes.Add || subType == RunnerItemSubTypes.Set)
        {
            _value = node["value"].ToString();

            if (_value.StartsWith("jmes:"))
            {
                _value = Utilities.GetJMESResult(_value.Substring("jmes:".Length), state);
                if (_value.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                    _isValid = false;
            }
        }
        else
            // The default invalid JMES value. Removed variables don't need a value.
            _value = "null";
    }


    public async Task Run(State state)
    {
        if (!_isValid)
        {
            state.Logger.LogError($"Encountered an invalid variable: {_id}");
            return;
        }

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

        await Task.CompletedTask;
    }
}
