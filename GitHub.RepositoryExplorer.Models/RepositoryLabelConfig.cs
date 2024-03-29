﻿using System.Text.Json;

namespace GitHub.RepositoryExplorer.Models;

// Each config stores:
// array of ms.prod
// child array of ms.technology
// array of priorities
// array of type (e.g. "doc-bug", "doc-enhancement", "doc-idea"

public static class Extensions
{
    public static string UnassignedFilter(this IEnumerable<ILabelDefinition> values) =>
        string.Join(" ", values.Select(c => $"-label:{c.Label}"));


    public static string Filter(this IEnumerable<ILabelDefinition> values, ILabelDefinition labelDefinition) =>
        labelDefinition.Label switch
        {
            "*" => values.UnassignedFilter(),
            null => "",
            _ => @$"label:""{labelDefinition.Label}"""
        };
}
public interface ILabelDefinition
{
    string Label { get; set; }
    string DisplayLabel { get; set; }
}

// This will be read from some config file, that somehow specifies the GitHub org and GitHub repo (maybe the config is stored there in time)
public class IssueClassificationModel
{
    static readonly char[] invalidChars = { ',', '/', '\\', ':' };

    public Product[] Products { get; set; } = Array.Empty<Product>();
    public Classification[] Classification { get; set; } = Array.Empty<Classification>();
    public Priority[] Priorities { get; set; } = Array.Empty<Priority>();

    public static async Task<IssueClassificationModel> CreateFromConfig(string configFolder, string organization, string repo)
    {
        // TODO: Get config file from other storage.
        if (organization is null) throw new ArgumentNullException(nameof(organization));
        if (repo is null) throw new ArgumentNullException(nameof(repo));
        if (organization.IndexOfAny(invalidChars) != -1) throw new ArgumentException("organization contains invalid characters", nameof(organization));
        if (repo.IndexOfAny(invalidChars) != -1) throw new ArgumentException("repo contains invalid characters", nameof(repo));
        var filePath = $"{organization}-{repo}.json";
        if (!string.IsNullOrEmpty(configFolder))
        {
            filePath = Path.Combine(configFolder, filePath);
        }
        using FileStream openStream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<IssueClassificationModel>(openStream) ??
            throw new InvalidOperationException("Could not read config");
    }
}

// In all cases, these objects are serialized at startup. 
// They are non-null once read. Hence the nullable suppression.
public class Product : ILabelDefinition
{
    public string Label { get; set; } = default!;
    public string DisplayLabel { get; set; } = default!;
    public Technology[] Technologies { get; set; } = default!;

    public string UnassignedTechnology() => string.Join(" ", Technologies.Select(t => $"-label:{t.Label}"));

}

public class Technology : ILabelDefinition
{
    public string Label { get; set; } = default!;
    public string DisplayLabel { get; set; } = default!;
}

public class Classification : ILabelDefinition
{
    public string Label { get; set; } = default!;
    public string DisplayLabel { get; set; } = default!;
}

public class Priority : ILabelDefinition
{
    public string Label { get; set; } = default!;
    public string DisplayLabel { get; set; } = default!;
}
