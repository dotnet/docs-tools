FROM mcr.microsoft.com/dotnet/sdk:9.0 as build-env
# Copy everything and publish the release (publish implicitly restores and builds)
WORKDIR /app
COPY . ./
RUN dotnet publish "WhatsNew.Cli/WhatsNew.Cli.csproj" -c Release -o out --no-self-contained

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:9.0
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "/WhatsNew.dll" ]