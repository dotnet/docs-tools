FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/dependabot-bot/dependabot-bot.csproj", "src/dependabot-bot/"]
RUN dotnet restore "src/dependabot-bot/dependabot-bot.csproj"
COPY . .
WORKDIR "/src/src/dependabot-bot"
RUN dotnet build "dependabot-bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dependabot-bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dependabot-bot.dll"]