using Newtonsoft.Json.Linq;

namespace GitHub.RepositoryExplorer.Models;

public static class DailyRecordExtensions
{
    public static IssuesSnapshot ToSnapshot(this DailyRecord dailyRecord, SnapshotKey key)
    {
        var date = dailyRecord.Date;
        var count = dailyRecord.Issues.IssueCount(key.Product, key.Technology, key.Priority, key.Classification);
        return new IssuesSnapshot(key.Product, key.Technology, key.Priority, key.Classification, new int[] { count }, date.ToShortDateString());
    }

    public static IssuesSnapshot ToSnapshot(this IEnumerable<DailyRecord> dailyRecords, SnapshotKey key, DateOnly fromDate, DateOnly endDate)
    {
        List<int> counts = new List<int>();

        DateOnly currentDate = fromDate;
        foreach (var dailyRecord in 
            dailyRecords)
        {
            // If the dates skip by different dates than the next selected day, there's no data for that day.
            // add -1 as the value.
            while (dailyRecord.Date > currentDate)
            {
                counts.Add(-1);
                currentDate = currentDate.AddDays(1);
            }
            counts.Add(dailyRecord.Issues.IssueCount(key.Product, key.Technology, key.Priority, key.Classification));
            currentDate = currentDate.AddDays(1);
        }
        while (endDate > currentDate)
        {
            counts.Add(-1);
            currentDate = currentDate.AddDays(1);
        }

        return new IssuesSnapshot(key.Product, key.Technology, key.Priority, key.Classification, counts.ToArray(), fromDate.ToShortDateString());
    }
}