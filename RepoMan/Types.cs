using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoMan;

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
    Predefined
}

public enum RunnerItemSubTypes
{
    Add,
    Remove,
    Set
}
