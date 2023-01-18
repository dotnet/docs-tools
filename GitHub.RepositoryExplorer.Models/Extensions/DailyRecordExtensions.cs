namespace GitHub.RepositoryExplorer.Models;

public static class DailyRecordExtensions
{
    public static IssuesSnapshot ToSnapshot(this DailyRecord dailyRecord, SnapshotKey key)
    {
        var date = dailyRecord.Date;
        var count = dailyRecord.Issues.IssueCount(key.Product, key.Technology, key.Priority, key.Classification);
        return new IssuesSnapshot(key.Product, key.Technology, key.Priority, key.Classification, new int[] { count }, date.ToShortDateString());
    }

    public static IssuesSnapshot ToSnapshot(this IEnumerable<DailyRecord> dailyRecords, SnapshotKey key)
    {
        List<int> counts = new List<int>();

        foreach (var dailyRecord in 
            dailyRecords)
        {
            counts.Add(dailyRecord.Issues.IssueCount(key.Product, key.Technology, key.Priority, key.Classification));
        }
        var date = dailyRecords.First().Date;
        return new IssuesSnapshot(key.Product, key.Technology, key.Priority, key.Classification, counts.ToArray(), date.ToShortDateString());
    }
}