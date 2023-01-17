# Functions App for GitHub.RepositoryExplorer

Function app built using the Azure Function SDK v4 and .NET 6. This app contains timer triggered functions that will retrieve and store daily issue counts from the configured GitHub repositories.

List of repositories configured in `appsettings.json`.

Create a `local.settings.json` file with the following contents:

{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "GitHubKey": "YOUR GITHUB PERSONAL ACCESS TOKEN HERE",
    "RepositoryOptions__CosmosConnectionString": "YOUR COSMOS DB CONNECTION STRING HERE",
    "RepositoryOptions__DatabaseId": "IssueStatistics",
    "RepositoryOptions__ContainerId": "DailyStatistics"
  }
}

## Running Function Manually

When doing local development, to run a TimerTrigger function manually, send a POST request to the folllowing URL:

`http://localhost:7071/admin/functions/{FunctionName}`

with a JSON body:

```
{ "input": "test" }
```

Example:
`POST http://localhost:7071/admin/functions/CaptureDailyIssueCountsFunction`
