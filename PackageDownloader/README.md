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

The program deletes and recreates its target output directories, so don't run it
while files in those directories are being edited or used by another process.

Note: the output location/version are currently configured in `Program.cs` (see `dotnetDir`, `versionDir`, and `windowsDesktopDir`). The tool also downloads shim exclusions from the `netfxreference.props` file in dotnet/runtime to decide which assemblies to skip.
