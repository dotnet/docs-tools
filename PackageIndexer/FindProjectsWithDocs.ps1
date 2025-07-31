Get-ChildItem -Path "C:\users\gewarren\runtime\src\libraries" -Recurse -Filter "*.csproj" -File | 
    Where-Object { $_.FullName -notlike "*\ref\*" -and $_.FullName -notlike "*\tests\*" -and $_.FullName -notlike "*\gen\*" -and $_.FullName -notlike "*\shims\*" -and $_.FullName -notlike "*\tools\*" -and $_.FullName -notlike "*\System.Private*\*" -and $_.FullName -notlike "*\Fuzzing\*" -and $_.FullName -notlike "*\externals.csproj" -and $_.FullName -notlike "*\Microsoft.NETCore.Platforms\*" -and $_.BaseName -notlike "System.Threading.RateLimiting" -and $_.BaseName -notlike "Microsoft.XmlSerializer.Generator" } | 
    ForEach-Object {
        $content = Get-Content -Path $_.FullName -Raw
        if ($content -notmatch "UseCompilerGeneratedDocXmlFile") {
            $_.BaseName
        }
    }
