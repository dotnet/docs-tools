namespace DotNetDocs.RepoMan.Actions;

internal interface IRunnerItem
{
    Task Run(InstanceData data);
}
