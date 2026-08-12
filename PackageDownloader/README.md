# PackageDownloader

`PackageDownloader` updates local reference assemblies used to feed the .NET API 
reference documentation generator. It downloads the latest .NET Core and Windows Desktop 
reference assemblies from the packages available on the NuGet V3 feed 
at `https://packagefeedproxy.microsoft.io/nuget/v3/index.json`.

## Run

From the repository root:

```powershell
dotnet run --project PackageDownloader\PackageDownloader.csproj
```

Override the output directory and .NET version with named arguments:

```powershell
dotnet run --project PackageDownloader\PackageDownloader.csproj -- --dotnet-dir C:\Users\me\binaries\dotnet --version 11.0
```

Options:

- `--dotnet-dir <path>`: root output directory. Defaults to `C:\Users\gewarren\binaries\dotnet`.
- `--version <major.minor>`: .NET version used to select the package `ref/net<version>` directory and name the output directories. Defaults to `11.0`.

The program deletes and recreates its target output directories, so don't run it
while files in those directories are being edited or used by another process.

The tool also downloads shim exclusions from the `netfxreference.props` file in dotnet/runtime to decide which assemblies to skip.
