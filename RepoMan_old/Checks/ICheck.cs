namespace RepoMan.Checks;

internal interface ICheck
{
    Task<bool> Run(State state);
}
