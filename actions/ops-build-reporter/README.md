# OPS Build Reporter GitHub Action

This action updates a pull request body with preview links extracted from the OpenPublishing.Build report and a link to the build report itself. Optionally, it also annotates changed files with any build errors or warnings.

## Usage

```yml
on: [pull_request_target]

jobs:
  ops_build_reporter_job:
    permissions:
      checks: write
      pull-requests: write
    runs-on: ubuntu-latest
    steps:
    - uses: dotnet/docs-tools/actions/ops-build-reporter@main
      with:
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        max_wait_time_minutes: 20
        annotate_file_warnings: true
```

When `annotate_file_warnings` is enabled, the workflow token requires `checks: write` permissions.
