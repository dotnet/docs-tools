$support = $args[0]         # STS, LTS, or Preview
$projectsJson = $args[1]    # JSON string of projects to upgrade

Write-Host "Upgrading projects to $support versions"

# Convert JSON string to array of strings
$projects = ConvertFrom-Json $projectsJson

Write-Host "Found $($projects.Length) projects to upgrade"

# Install .NET Upgrade Assistant global tool
dotnet tool install --global upgrade-assistant

# Iterate all upgrade projects
foreach ($projectDir in $projects) {
    Write-Host "Attempting to upgrade project: $projectDir"

    # Create a new branch
    $branchName = $projectDir -replace '[^A-Za-z0-9._-]', '-'
    $branch = "upgrade/$branchName"
    git checkout -b $branch

    upgrade-assistant upgrade "$projectDir" `
        --non-interactive `
        --operation Inplace `
        -t $support

    # Attempt the upgrade using upgrade-assistant
    if ($LASTEXITCODE -eq 0) {
        # Commit the changes
        git add .
        git commit -m ".NET Version Sweeper: Upgraded $projectDir"

        # Push the branch to the repository
        git push origin $branch

        # Create a pull request
        gh pr create `
            --base main `
            --head $branch `
            --title "[$support] Upgrade $projectDir" `
            --body "Automated pull request to upgrade the target framework moniker `
            (TFM) of $projectDir to .NET $support."
    }
    else {
        Write-Host "Unable to upgrade project: $projectDir"

        # Delete the branch if the upgrade fails
        git checkout main
        git branch -D $branch
    }
}