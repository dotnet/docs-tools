# Snippets5000

Snippets5000 is a command-line tool for validating and building code samples in a local repository checkout. It reads the files changed in a GitHub pull request, discovers the corresponding projects or solutions, checks their structure, and builds each unique target. A standalone C# file can also be a build target with .NET 10.

## Prerequisites

- The .NET 10 SDK to run the tool from source.
- A local checkout containing the sample changes you want to validate. The tool doesn't clone the repository or check out the pull request.
- Any SDKs, workloads, and build tools required by the samples.
- For pull-request mode, a GitHub token in the `GitHubKey` environment variable with read access to the repository and its pull-request files.

Only run the tool against code and configuration you trust: builds and custom commands execute locally.

## Command-line options

Run the following examples from the root of this repository. Use an absolute path for `--sourcepath`; the tool changes its working directory during discovery and builds.

### Validate a pull request

The default command queries GitHub for changed files, then validates and builds the affected targets in your local checkout.

```shell
dotnet run --project snippets5000/Snippets5000 -- --sourcepath /absolute/path/to/docs --owner dotnet --repo docs --pullrequest 12345
```

| Option | Description |
| --- | --- |
| `--sourcepath` | Required. Local root directory of the repository being validated. |
| `--owner` | Required. GitHub repository owner or organization. |
| `--repo` | Required. GitHub repository name, without the owner. |
| `--pullrequest` | Required. Pull-request number to query for changed files. |

Set `GitHubKey` in the process environment or supply it through your CI secret store. Don't put tokens in configuration files.

### Simulate a pull request locally

The `dryrun` command uses local JSON data instead of GitHub. Despite its name, it **executes builds** for discovered targets. It doesn't require `GitHubKey`.

```shell
dotnet run --project snippets5000/Snippets5000 -- dryrun --sourcepath /absolute/path/to/docs-tools/snippets5000/PullRequestSimulations --test-data-file /absolute/path/to/docs-tools/snippets5000/PullRequestSimulations/data.json --test-id "Edit - Single file no project - Compile"
```

| Option | Description |
| --- | --- |
| `--sourcepath` | Required. Root directory containing the simulated repository files. |
| `--test-data-file` | Required. Path to a JSON array of simulated pull requests. |
| `--test-id` | Required. The `Name` of a simulation, matched case-insensitively. |

A minimal simulation file looks like this:

```json
[
  {
    "Name": "Edit a sample",
    "Items": [
      {
        "ItemType": "Edit",
        "Path": "samples/example/Program.cs"
      }
    ]
  }
]
```

`Path` is relative to `--sourcepath`. `ItemType` can be `Edit`, `Create`, or `Delete`, but discovery uses the local file's existence, not `ItemType`, to determine whether it was deleted. `ExpectedResults` and `CountOfEmptyResults` are test-suite metadata; the `dryrun` command doesn't assert them.

### List all build targets

```shell
dotnet run --project snippets5000/Snippets5000 -- build-all --sourcepath /absolute/path/to/docs
```

`--sourcepath` is required. **Currently, `build-all` only lists targets; it doesn't compile or validate them.** It finds `.sln` and `.slnx` solutions, plus `.csproj`, `.fsproj`, `.vbproj`, and `.vcxproj` projects outside solution directories. It doesn't use the discovery environment variables below.

Use `--help`, `dryrun --help`, or `build-all --help` to display command-line help.

## Environment variables

These settings are read when the process starts. Names are case-sensitive on platforms such as Linux.

| Variable | Purpose | Default |
| --- | --- | --- |
| `GitHubKey` | GitHub token. Required only in pull-request mode. | None |
| `ExtensionsProjects` | Project and solution extensions used by pull-request and simulation discovery. | `.sln;.slnx;.csproj;.fsproj;.vbproj;.vcxproj;.proj` |
| `ExtensionsCodeTriggers` | Code-file extensions that trigger discovery. | `.cs;.vb;.fs;.cpp;.h;.xaml;.razor;.cshtml;.vbhtml;.sln;.slnx;.csproj;.fsproj;.vbproj;.vcxproj;.proj` |
| `FileTriggers` | Exact file names that trigger discovery. | `global.json;snippets.5000.json` |

List settings are semicolon-separated and **replace**, rather than extend, the defaults. Include the leading dot in extensions, and don't add spaces around separators. File names and extensions are matched case-insensitively. Unset, empty, or whitespace-only values use the defaults.

For example, to add `Directory.Build.props` as a trigger in Bash:

```bash
export FileTriggers='global.json;snippets.5000.json;Directory.Build.props'
```

The setting is named `ExtensionsProjects`, not `ExtensionsProjs`. `VS_DEVCMD` isn't read by the current implementation.

## Per-target configuration: `snippets.5000.json`

Place an optional `snippets.5000.json` file **in the same directory as the discovered build target** (the solution, project, or standalone C# file). Configuration isn't inherited from parent directories or merged across projects. When a solution is the target, its configuration applies to the whole solution build.

```json
{
  "host": "dotnet",
  "expectederrors": []
}
```

JSON comments are supported. Use the lowercase property names shown here.

| Property | Default | Description |
| --- | --- | --- |
| `host` | `"dotnet"` | Build host: `"dotnet"`, `"visualstudio"`, or `"custom"`. Values are case-sensitive. |
| `command` | Not set | Command script to execute when `host` is `"custom"`. Required for that host; ignored by the other hosts. |
| `expectederrors` | `[]` | Compiler errors that can be ignored when a build fails. |

### Build hosts

`dotnet` runs `dotnet build "<target>"`.

`visualstudio` is intended for Windows. It calls `VsDevCmd.bat`, runs `nuget.exe restore "<target>"`, and then runs `msbuild.exe "<target>" -restore:True`. The setup-script path is currently hard-coded to:

```text
C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\Tools\VsDevCmd.bat
```

There is no environment-variable or JSON override for that path. Use a custom host if your installation differs. If no configuration file exists, the tool automatically selects `visualstudio` for a project whose first line starts with `<Project`, contains `ToolsVersion`, and doesn't contain `Sdk=`.

`custom` writes `command` to a script and executes it from the build target's directory. It supports these literal, case-sensitive placeholders:

| Placeholder | Replacement |
| --- | --- |
| `{repoRoot}` | The supplied `--sourcepath`. |
| `{projectPath}` | Absolute path to the build target. |
| `{projectDirectory}` | Absolute directory containing the build target. |

For example, this configuration passes additional arguments to `dotnet build`:

```json
{
  "host": "custom",
  "command": "#!/bin/bash\ndotnet build \"{projectPath}\" --configuration Release"
}
```

On Linux, use a shell script with an appropriate shebang, as shown above. On Windows, use batch-file syntax and omit the shebang. Quote placeholders when paths might contain spaces. Custom commands must return a nonzero exit code when they fail.

### Expected compiler errors

For samples that intentionally fail to compile, declare expected diagnostics:

```json
{
  "expectederrors": [
    {
      "file": "Program.cs",
      "line": 5,
      "column": 1,
      "error": "CS0234"
    }
  ]
}
```

The tool parses MSBuild-style error output and accepts a failed build only when every detected error is matched. `error` is the diagnostic code, `line` is the source line number, and `file` is a suffix of the reported file path, matched case-insensitively. Use separators that match the build output.

Current limitations:

- `column` is deserialized but isn't used for matching.
- File matching always uses the **first** `expectederrors` entry's `file`, even when matching later entries. Don't rely on one configuration to suppress errors across different files.
- Expected errors don't suppress structural failures, such as a missing project or a solution that doesn't reference its projects.
- Only recognizable build-error output can be matched; this isn't a general-purpose exit-code allowlist. Expected diagnostics aren't required to occur when a build succeeds.

## Results and generated files

Pull-request and simulation modes return `0` when all targets are accepted and `1` when unresolved structural or build failures remain. Argument, configuration, or process errors can also terminate the command unsuccessfully.

The tool emits GitHub Actions error annotations and writes failure details to `output.json` in the directory from which it was started. It writes that file only when unresolved failures remain; it doesn't remove an existing file after a successful run.

Each build writes a runner script beside its target: `snippets5000_runner.sh` on Linux or `snippets5000_runner.bat` otherwise. These scripts aren't automatically deleted.
