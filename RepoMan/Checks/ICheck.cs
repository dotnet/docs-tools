namespace RepoMan.Checks;

public interface ICheck
{
    Task<bool> Run(State state);
}
