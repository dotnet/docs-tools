namespace DotNetDocs.RepoMan.Checks;

internal interface ICheck
{
    Task<bool> Run(InstanceData state);
}
