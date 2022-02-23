FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY . ./

RUN dotnet publish ./src/ActionRunner/ActionRunner.csproj -c Release -o out --no-self-contained

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY --from=build /out .

ENTRYPOINT [ "dotnet", "/ActionRunner.dll" ]
