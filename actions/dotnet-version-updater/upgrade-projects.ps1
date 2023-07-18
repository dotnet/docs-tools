$support = $args[0]         # STS, LTS, or Preview
$projectsJson = $args[1]    # JSON string of projects to upgrade

Write-Host "Upgrading projects to $support versions"
Write-Host ""

# Convert JSON string to array of strings
$projects = ConvertFrom-Json $projectsJson

Write-Host "Parsed $($projects.Length) projects to upgrade"
Write-Host ""

# Install .NET Upgrade Assistant global tool
dotnet tool install --global upgrade-assistant

# Get the git repository directory
$gitRepoDir = (git rev-parse --show-toplevel)

# Create an array of projects that to track failure attempts
$failedProjects = @()

# Iterate all upgrade projects
foreach ($projectDir in $projects) {
    Write-Host "Attempting to upgrade project: $projectDir"
    Write-Host ""

    # Remove the git repository directory from the project directory
    $relativePath = $projectDir.Replace($gitRepoDir, "")

    # Remove invalid characters from the branch name, then remove leading hyphen
    $branchName = $relativePath -replace '[^A-Za-z0-9._-]', '-'
    $branchName = $branchName -replace '^[-]+', ''

    # Format the branch name
    $branch = "auto-upgrade/$branchName"

    # Create a new branch
    git checkout -b $branch

    # Normalize the project directory
    $projectDir = [IO.Path]::GetFullPath($projectDir)

    # Capture all output from the upgrade-assistant command
    $output = upgrade-assistant upgrade "$projectDir" `
        --non-interactive `
        --operation Inplace `
        -t $support `
        2>&1

    # Check if the exit code is 0 (success), or if there is output from a failure
    if ($LASTEXITCODE -eq 0 -and -not ($output -match "Exception")) {

        Write-Host $output
        Write-Host ""

        # Check for changes, report the exit code
        git diff --exit-code

        # If there aren't any, delete the 
        # branch and continue to the next project
        if ($LASTEXITCODE -ne 0) {
            $failedProjects += $projectDir

            Write-Host "No changes detected for project: $projectDir"
            Write-Host ""

            # Delete the branch if there aren't any changes
            git checkout main
            git branch -D $branch
            Write-Host ""

            continue
        }

        # Commit the changes
        git add .
        git commit -m ".NET Version Sweeper: Upgraded $projectDir"

        # Push the branch to the repository
        git push origin $branch

        # Format the pull request message
        $pullRequestMessage = "⚡ This is an automated pull request (powered by the 
            [.NET Versioon Sweeper](https://github.com/dotnet/versionsweeper) and
            the [.NET Upgrade Assistant](https://github.com/dotnet/upgrade-assistant)]) 
            to _upgrade_ the **target framework moniker (TFM)** for the '$projectDir' 
            project to the $support version. For more information, see 
            [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy)."

        # Create a pull request
        gh pr create `
            --base main `
            --head $branch `
            --title "[$support] Upgrade $projectDir" `
            --body $pullRequestMessage
    }
    else {
        $failedProjects += $projectDir

        Write-Host "Unable to upgrade project: $projectDir"
        Write-Host ""

        # Write the output as a warning
        Write-Warning -Message ([String]::Join("`n", $output))
        Write-Host ""

        # Delete the branch if the upgrade fails
        git checkout main
        git branch -D $branch
        Write-Host ""
    }
}

# If there are any failed projects, log them
if ($failedProjects.Length -gt 0) {
    Write-Host "Failed to upgrade $($failedProjects.Length) projects:"
    Write-Host ""

    foreach ($failedProject in $failedProjects) {
        Write-Host $failedProject
    }
}
