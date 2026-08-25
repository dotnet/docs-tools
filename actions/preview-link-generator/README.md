# Preview link generator GitHub Action

This action updates a pull request body with preview links extracted from the OpenPublishing.Build report after the OPS status check completes.

## Usage

```yml
on: [pull_request_target]

jobs:
  preview_link_generator_job:
    permissions:
      checks: write
      pull-requests: write
    runs-on: ubuntu-latest
    steps:
    - uses: dotnet/docs-tools/actions/preview-link-generator@main
      with:
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        max_wait_time_minutes: 20
        annotate_file_warnings: true
```

      When `annotate_file_warnings` is enabled, the action creates check-run annotations for build errors and warnings that occur on added lines in the pull request. Diagnostics on unchanged lines are ignored. The workflow token requires `checks: write` for this option.
