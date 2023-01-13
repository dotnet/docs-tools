## Prerequisites

The following components are required to run the tool locally:

- Visual Studio 2019 with the **.NET Core cross-platform development** workload
- [.NET SDK 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) or later

## Set up a GitHub personal access token

A GitHub personal access token (PAT) is required to access the GitHub API. Use the following steps to satisfy the PAT requirement:

1. Generate a PAT per the instructions at [Creating a token](https://help.github.com/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line#creating-a-token). When selecting scopes for the PAT, check the following boxes:
    - For private repos:
      - **repo**
      - **admin:org** > **read:org**
    - For public repos:
      - **repo** > **public_repo**
      - **admin:org** > **read:org**
1. For private repos in the *MicrosoftDocs* GitHub organization, after generating the PAT, select **Enable SSO** and choose **Authorize** for both **microsoft** and **MicrosoftDocs**.
1. Store the PAT in an environment variable named `GitHubKey`.

## Set up an OSPO personal access token

> This step is required for versions 2.0 and above.

The OSPO personal access token is required to access the OSPO API. This API determines community contributors and Microsoft employee and vendor contributors.

1. Request a token at [Visual Studio Online](https://ossmsft.visualstudio.com/_usersSettings/tokens). You can disable all scopes *except* read:user profile.
1. Store the token in an environment variable named "OspoKey".  If you are using the GitHub Action to generate the PR automatically, add the key as a secret in your repository:
  - Go to **Settings** on your repo.
  - Select **Secrets**
  - Add "OpsoKey" as a **Repository Secret**.

## Build and run

There are 2 supported approaches to running the tool locally, as outlined in the following sections. Choose the approach that best suits your needs.

### Run from the generated DLL

1. Build the *WhatsNew.Cli* project.
1. Navigate to the *WhatsNew.Cli* project's *bin\Debug\net5.0* directory.
1. Run the appropriate `dotnet whatsnew.dll` command from the aforementioned directory. For example:

    ```bash
    dotnet whatsnew.dll -h
    ```

### Run as a .NET global tool

1. Build the *WhatsNew.Cli* project. Doing so packages the project as a [.NET global tool](https://docs.microsoft.com/dotnet/core/tools/global-tools), as represented by the *.nupkg* file in the project's *nuget* directory.
1. Run the following command from the project's root directory to install the global tool:

    ```bash
    dotnet tool install -g --add-source nuget dotnet-whatsnew
    ```

1. Run the following command to confirm installation of the global tool:

    ```bash
    dotnet whatsnew -h
    ```

## Build and deploy (CI/CD)

### Build

Build automation is handled by Azure Pipelines via the [dotnet-docs-tools build pipeline](https://dev.azure.com/mseng/TechnicalContent/_build?definitionId=10534). A YAML representation of this build pipeline is stored in the *azure-pipelines.yml* file.

The build pipeline contains a `trigger` section which determines the branch names/patterns and paths that trigger a build. For example:

```yml
trigger:
  branches:
   include:
     - main
     - scottaddie/*
  paths:
    include:
      - whatsnew/*
    exclude:
      - whatsnew/src/WhatsNew.Cli/CONTRIBUTING.md
      - whatsnew/src/WhatsNew.Cli/README.md
```

With the preceding configuration:

- A commit to *main* or a branch prefixed with *scottaddie/* triggers the build.
- Any file in the *whatsnew* directory triggers a build, with the exception of the Markdown files listed in `exclude`.

### Release

Publishing of the NuGet package (containing the .NET global tool) is handled by the last task in the build pipeline. The package version number is determined by the *WhatsNew.Cli* project's `<Version>` element's value. If the publish is successful, the new package version appears in the Azure Artifacts [DotnetDocsTools feed](https://dev.azure.com/mseng/TechnicalContent/_packaging?_a=feed&feed=DotnetDocsTools%40Local).

Complete the following steps to publish the package:

1. Increment the version number in the project file's `<Version>` element and commit the change. Wait for the build to complete, and ensure that the change has been merged to *main*.
1. Select the **Run pipeline** button at https://dev.azure.com/mseng/TechnicalContent/_build?definitionId=10534.
1. Select *master* as the branch in the **Branch/tag** drop down list.
1. Select *true* for the question **Publish NuGet package**.
1. Select the **Run** button.
