using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class SetSvcSubSvcLabels: IRunnerItem
{
    public SetSvcSubSvcLabels()
    {
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation($"RUN [SERVICELABELS]:Adding service and subservice labels");

        if (data.Variables.ContainsKey("ms.service"))
            data.Operations.LabelsAdd.Add($"{data.Variables["ms.service"]}/svc");
        else
            data.Logger.LogInformation("svc metadata wasn't found: {service}", $"{data.Variables["ms.service"]}/svc");

        if (data.Variables.ContainsKey("ms.subservice"))
            data.Operations.LabelsAdd.Add($"{data.Variables["ms.subservice"]}/subsvc");
        else
            data.Logger.LogInformation("subsvc metadata wasn't found: {subservice}", $"{data.Variables["ms.subservice"]}/subsvc");


        await Task.CompletedTask;
    }
}
