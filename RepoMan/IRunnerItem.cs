namespace RepoMan;

internal interface IRunnerItem
{
    Task Run(State state);
}
