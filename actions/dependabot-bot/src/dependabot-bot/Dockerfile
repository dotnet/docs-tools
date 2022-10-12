FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish "dependabot-bot.csproj" -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS final
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "dependabot-bot.dll" ]