FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./dependabot-bot.csproj -c Release -o out --no-self-contained

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/runtime:7.0
COPY --from=build-env /out .
ENTRYPOINT ["dotnet", "dependabot-bot.dll"]