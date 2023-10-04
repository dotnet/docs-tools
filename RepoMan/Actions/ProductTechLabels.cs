using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class SetProdTechLabels: IRunnerItem
{
    public SetProdTechLabels()
    {
    }

    public async Task Run(State state)
    {
        state.Logger.LogInformation($"Adding product and tech labels");

        if (state.Variables.ContainsKey("ms.prod"))
            state.Operations.LabelsAdd.Add($"{state.Variables["ms.prod"]}/prod");

        if (state.Variables.ContainsKey("ms.technology"))
            state.Operations.LabelsAdd.Add($"{state.Variables["ms.technology"]}/tech");

        await Task.CompletedTask;
    }
}
