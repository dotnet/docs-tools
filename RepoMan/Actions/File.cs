using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

// TODO: This is an action, but it seems to operate like a check??
internal sealed class File : IRunnerItem
{
    private const string ModeAdd = "add";
    private const string ModeChanged = "changed";
    private const string ModeDelete = "remove";

    private readonly string _mode;
    private readonly bool _isValid;
    private readonly IEnumerable<FileCheck> _items;

    public File(YamlSequenceNode node, string mode, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Check-files with mode {mode}", mode);

        _mode = mode;

        if (mode != ModeAdd && mode != ModeChanged && mode != ModeDelete)
            throw new Exception($"BUILD: File action mode is invalid: {mode}");

        List<FileCheck> items = new List<FileCheck>(node.Children.Count);

        foreach (YamlNode item in node.Children)
        {
            data.Logger.LogDebug("BUILD: Adding check {path}", item["path"]);
            items.Add(new FileCheck(item["path"].ToString(), Runner.Build(item["run"].AsSequenceNode(), data)));
        }

        _items = items;

        _isValid = true;
    }

    public async Task Run(InstanceData data)
    {
        if (!_isValid)
        {
            data.Logger.LogError("File action is invalid, can't run");
            return;
        }

        data.Logger.LogInformation("RUN [FILES]:Running files action and checking for PR file matches");

        // TODO: New feature, detect add/updated/delete file changes.
        // Currently we don't care what happened.
        foreach (FileCheck item in _items)
        {
            bool match = false;
            foreach (Octokit.PullRequestFile file in data.PullRequestFiles)
            {
                if (Utilities.MatchRegex(item.RegexCheck, file.FileName ?? "", data) || Utilities.MatchRegex(item.RegexCheck, file.PreviousFileName ?? "", data))
                {
                    data.Logger.LogInformation("Found a match for {regex}", item.RegexCheck.Replace("\n", "\\n"));
                    match = true;
                    break;
                }
            }

            if (match)
                await item.Actions.Run(data);
        }
        return;
    }

    private sealed record class FileCheck(string RegexCheck, Runner Actions);
}
