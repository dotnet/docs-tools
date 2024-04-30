using Microsoft.Extensions.Logging;

namespace RepoMan.Actions;

public sealed class SetSvcSubSvcLabels: IRunnerItem
{
    public SetSvcSubSvcLabels()
    {
    }

    public async Task Run(State state)
    {
        state.Logger.LogInformation($"Adding service and subservice labels");

        if (state.Variables.ContainsKey("ms.service"))
            state.Operations.LabelsAdd.Add($"{state.Variables["ms.service"]}/svc");

        if (state.Variables.ContainsKey("ms.subservice"))
            state.Operations.LabelsAdd.Add($"{state.Variables["ms.subservice"]}/subsvc");

        await Task.CompletedTask;
    }
}
