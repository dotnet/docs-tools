# Preview link generator GitHub Action

This action updates a pull request body with preview links extracted from the OpenPublishing.Build report after the OPS status check completes.

## Usage

```yml
on: [pull_request_target]

jobs:
  preview_link_generator_job:
    runs-on: ubuntu-latest
    steps:
    - uses: dotnet/docs-tools/actions/preview-link-generator@main
      with:
        repo_token: ${{ secrets.GITHUB_TOKEN }}
```
