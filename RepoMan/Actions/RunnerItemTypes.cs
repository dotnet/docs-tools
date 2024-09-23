namespace DotNetDocs.RepoMan.Actions;

public enum RunnerItemTypes
{
    Check,
    Label,
    Milestone,
    Project,
    Comment,
    Files,
    Issue,
    PullRquest,
    Variable,
    Assignee,
    Reviewer,
    Predefined,
    Close,
    Reopen,
    SvcSubSvcLabels,
    LinkRelatedIssues
}

public enum RunnerItemSubTypes
{
    Add,
    Remove,
    Set
}
