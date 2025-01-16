## Prerequisites

The following components are required to run the tool locally:

- Visual Studio 2022 (17.4) with the **.NET Core cross-platform development** workload
- [.NET SDK 7.0](https://dotnet.microsoft.com/download/dotnet/7.0) or later

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
1. Store the token in an environment variable named "OSPO_KEY".  If you are using the GitHub Action to generate the PR automatically, add the key as a secret in your repository:
  - Go to **Settings** on your repo.
  - Select **Secrets**
  - Add "OSPO_KEY" as a **Repository Secret**.

## Build and run

There are 2 supported approaches to running the tool locally, as outlined in the following sections. Choose the approach that best suits your needs.

### Run from the generated DLL

1. Build the *WhatsNew.Cli* project.
1. Navigate to the *WhatsNew.Cli* project's *bin\Debug\net5.0* directory.
1. Run the appropriate `dotnet whatsnew.dll` command from the aforementioned directory. For example:

    ```bash
    dotnet whatsnew.dll -h
    ```
