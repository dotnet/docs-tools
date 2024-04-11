namespace RepoMan;

public interface IRunnerItem
{
    Task Run(State state);
}
