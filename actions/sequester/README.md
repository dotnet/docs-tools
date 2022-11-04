# Import GitHub issues to Quest

For teams that work (almost) exclusively in public, GitHub is the natural location to do work item planning and tracking. To save significant time tracking work, we'll automatically copy updates to GitHub issues into Quest for consistency. We'll build a GitHub Action to transfer information from GitHub issues to Quest. The action will run in response to trigger events and update the Quest task board.

This first iteration includes the minimum necessary features to transfer data from GitHub to Quest. From this first iteration, we'll learn what additional features would be useful.

## Installation and use

Once installed, this workflow does the following:

- When a configured "trigger" label is added to an issue, the issue is copied to [Quest](https://dev.azure.com/msft-skilling/Content) as a new user story. In addition, the description in the GitHub issue is amended with a link to the work item. Finally, the configured "trigger" label is removed and a new "linked" label is added.
- When a linked work item is updated, by changing the assignee or closing ths issue, the associated work item is updated.

To install the GitHub actions:

1. ***Add the trigger labels***
  - You'll need to add two labels: one that informs the action to import an issue from GitHub to Quest. The second informs you that an issue has been imported.
1. ***Add a `quest-config.json` file***
  - In the root folder of your repo, create a config file that contains the keys shown [later in this document](#configure-consuming-github-action-workflow). In most cases, you'll modify the Azure DevOps area path, and trigger labels.
1. ***Add the `quest.yml` action workflow file***
  - For an example, see the [`dotnet/docs` installation](https://github.com/dotnet/docs/blob/main/.github/workflows/quest.yml). You'll likely need to change the checks on the labels.
1. ***Add secrets for Azure Dev Ops and Microsoft Open Source Programs Office***
  - You'll need to add two secret tokens to access the OSPO REST APIs and Quest Azure DevOps APIs.
  - *OSPO_KEY*: Generate a PAT [here](https://ossmsft.visualstudio.com/_usersSettings/tokens). UserProfile: Read is the only access needed.
  - *QUEST_KEY*: Generate a PAT [here](https://dev.azure.com/msft-skilling/_usersSettings/tokens). WorkItems: Read/Write and Project & Team: Read/Write access are needed.
1. Start applying labels.
  - Add the trigger label to any issue, and it will be imported into Quest.

## Suggestions for future releases

- [ ] Populate the "GitHub Repo" field in Azure DevOps to make reporting by repository easier.
- [ ] Add Epics (configurable) as a parent of user stories on import.
- [ ] Update the label block in Quest when an issue is closed. That way, any "OKR" labels get added when the work item is completed. This would be a simplified version of updating all labels when labels are added or removed.
- [ ] Integrate with Repoman. That tool already performs a number of actions on different events in the repo. The code underlying these events could be ported there.

## Triggers

The GitHub action will run on four events:

- *Issue added to a project*: Adding an issue to a (GitHub) project indicates that the team has agreed to add it to the work backlog. The action will copy the issue to Quest, and add a comment on the GitHub issue that links to the work item.
- *Assigned to changed*: When an issue is assigned, or it's assignment is changed, the action runs to update the person to whom the issue is assigned.
- *Completed*: When an issue is closed, close the corresponding Quest work item.

There are no plans to update the GitHub issue when changes are made in the Quest User Story. Any employee working in Quest has the expectation that their comments or updates are internal and secure. Automatically propagating that text to a public location creates the risk of publicly disclosing internal information.

## Configure consuming GitHub Action workflow

The consuming repository would ideally define the config file. As an example, it would look like this (with the following file name _quest-config.json_):

```json
{
  "AzureDevOps": {
    "Org": "msft-skilling",
	"Project": "Content",
    "AreaPath": "Production\\Digital and App Innovation\\DotNet and more\\dotnet"
  },
  "ImportTriggerLabel": ":world_map: reQUEST",
  "ImportedLabel": ":pushpin: seQUESTered"
}
```

This config file would override the defaults, enabling consuming repositories to do as they please. In addition to this JSON config, there would need to be three environment variables set:

- `ImportOptions__ApiKeys__GitHubToken`
- `ImportOptions__ApiKeys__OSPOKey`
- `ImportOptions__ApiKeys__QuestKey`

These env vars would be assigned to from the consuming repository secrets, as follows (likely in a workflow file named _quest-import.yml_):

```yaml
env:
  ImportOptions__ApiKeys__GitHubToken: ${{ secrets.GITHUB_TOKEN }}
  ImportOptions__ApiKeys__OSPOKey: ${{ secrets.OSPO_API_KEY }}
  ImportOptions__ApiKeys__QuestKey: ${{ secrets.QUEST_API_KEY }}
```

> **Note:** To configure GitHub Action secrets, see [GitHub Docs: Encrypted secrets](https://docs.github.com/actions/security-guides/encrypted-secrets).

## Field mapping

The GitHub issue fields don't match the Quest fields. Here's how we'll map them, or populate the required Quest fields:

| GitHub field  | Quest field                                                          |
|---------------|----------------------------------------------------------------------|
| Title         | Title                                                                |
| Description   | Description                                                          |
| Assigned to   | Assigned to *if FTE*                                                 |
| Comments      | Initial comments will be appended to the the user story description. |
| Labels        | Labels are added to the description when a user story is created.    |

Labels are prefixed with `#` and space characters are replaced with `-` to provide easy search for Quest items with a given label.

There are other Quest fields that don't have GitHub equivalents. They are populated as follows:

- State: The state is set to "new" (by default) when a user story is created.
- Area: Area is configured for each repo. The first pass will set all user stories to the same Area. In future updates, we may use the \*/prod and \*/technology labels to refine areas.
- Iteration: Iteration is left blank. In future updates, we may map GitHub sprint projects to Quest iterations. That event would assign the associated user story to an iteration.
- When a GitHub issues is imported into a Quest item, the bot will add a comment on both to link to the associated item:
  - On the GitHub issue:  `Associated WorkItem - nnnnnn` where `nnnnnn` is the Quest work item tag.
  - On the Quest item:  `GitHubOrg/repo#nnnnn`, where `GitHubOrg` is the GitHub organization (e.g. `MicrosoftDocs`), `repo` is the public repository, and `nnnnn` is the issue number.

## Actions performed on each trigger

This section briefly describes what will happen when each trigger event occurs for an issue. Because a maintainer may perform actions in different orders, there are a few conditions to check on each action.

### Issue added to a project

When an issue is added to a project, AND the issue is open, AND the GitHub issue isn't linked to a Quest User story, the following occurs:

- The issue is copied to a new Quest Work item.
  - All comments are copied into the description.
  - All labels are added as a bullet list in the description.
  - A comment is added in the Quest User Story to link to the original GitHub issue.
  - If the issue has already been assigned AND the assignee is an FTE, the Quest User Story is assigned to the same person.
- A comment is added to the GitHub issue to link to the Quest Work item.

If the issue is added to a project, and the GitHub issue is linked to a Quest user story, do nothing.

> **Note**: we could replace the existing description, or add a new comment with the updated description.

### Assignment

If the GitHub issue isn't linked to a Quest User Story, do nothing. If the GitHub issue is linked to a Quest User Story, the following occurs:

- Update the assigned field in the Quest user story, removing any existing assignees.

> **Note**: The differences between GitHub assignees and Azure Dev Ops assignee introduces the potential for tearing:
>
> - GitHub supports up to 10 assignees on an issue. The first assignee is transferred to Quest.
> - Some FTEs are not assigned as users in Quest. If a GH issue is assigned to a Microsoft employee who isn't a Quest user, that issue is transferred as "unassigned".

### Completed

If the GitHub issue isn't linked to a Quest User Story, do nothing. If the GitHub issue is linked to a Quest User story, the following occurs:

- The Quest user story state is changed to complete.

## Other design considerations

This first proof-of-concept has three goals:

1. Minimize duplicated work by reporting in two systems: public GitHub issues, and internal Quest Work items.
1. Learn what other events could trigger automated updates.
1. Learn what other information is useful for Quest users.

This initial automation ensures that content developers or M1's aren't spending time duplicating information, but can work in their environment. In doing this, we'll learn if any other information or triggers are useful. For example, teams working in public use GitHub projects to plan iterations. We may find that when an issue is added to a GitHub project, it should be scheduled for an iteration in Quest.
