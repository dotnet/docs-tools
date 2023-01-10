namespace IssueCloser;

class IssueSet
{

    internal static readonly int[] Ages =
    {
        // P0
         -1, 
        -1,  
        -1, 
        -1,  
        -1, 
        -1,  
        -1, 
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        // P1, Pri1
        -1, // TD 24,
        18,
        -1, // TD 18,
        24,
        // P1, Pri2
        18,
        12,
        12,
        18,
        // P1, Pri3
        12,
        12,
        12, // TD 9,
        12,
        // P1, Missing
        18,
        12,
        12,
        18,
        // P2, Pri1
        18,
        12,
        12,
        18,
        // P2, Pri2
        12,
         9,
         9,
        12,
        // P2, Pri3
         9,
         9,
         9, // TD 6,
         9,
        // P2, Missing
        12,
         9,
         9,
        12,
        // P3, Pri1
        9, // TD 12,
         9,
         9,
        12,
        // P3, Pri2
         9,
         6,
         6,
         9,
        // P3, Pri3
         6,
         6,
         3,
         6,
        // P3, Missing
         9,
         6,
         6,
         9,
        // Missing, Pri1
        18,
        12,
        12,
        18,
        // Missing, Pri2
        12,
         9,
         9,
        12,
        // Missing, Pri3
         9,
         9,
         6,
         9,
        // Missing, Missing
        12,
         9,
         9,
        12
    };

    public int AgeToClose { get; init; }

    public int TotalIssues { get; set; }
    public int ClosedIssues { get; set; }

    public bool ShouldCloseIssue(CloseCriteria issue, int ageInMonths)
    {
        TotalIssues++;
        if ((AgeToClose == -1) || (ageInMonths < AgeToClose))
            return false;
        ClosedIssues++;
        return true;
    }

    public override string ToString() =>
        $"Total issues: {TotalIssues}, Closing {ClosedIssues} older than {AgeToClose} months";
}

